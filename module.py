# module.py
import ctypes
import os
import sqlite3
from datetime import datetime

# ==========================================
# 1. INITIALISATION CTYPES (Mise à jour en c_double)
# ==========================================
dll_path = os.path.join(os.path.dirname(__file__), "calculs.dll")
lib = ctypes.CDLL(dll_path)

lib.calc_vat.argtypes = [ctypes.c_double, ctypes.c_double]
lib.calc_vat.restype = ctypes.c_double

lib.calc_ttc.argtypes = [ctypes.c_double, ctypes.c_double]
lib.calc_ttc.restype = ctypes.c_double

lib.sum_total.argtypes = [ctypes.POINTER(ctypes.c_double), ctypes.c_int]
lib.sum_total.restype = ctypes.c_double

lib.render_change.argtypes = [ctypes.c_int, ctypes.POINTER(ctypes.c_int), ctypes.c_int]
lib.render_change.restype = ctypes.c_int

lib.calc_ttc_batch.argtypes = [ctypes.POINTER(ctypes.c_double), ctypes.POINTER(ctypes.c_double), ctypes.POINTER(ctypes.c_double), ctypes.c_int]
lib.calc_ttc_batch.restype = None

# ==========================================
# 2. WRAPPERS CTYPES
# ==========================================
def calc_vat(price_ht: float, vat_rate: float) -> float:
    return round(lib.calc_vat(ctypes.c_double(price_ht), ctypes.c_double(vat_rate)), 2)

def calc_ttc(price_ht: float, vat_rate: float) -> float:
    return round(lib.calc_ttc(ctypes.c_double(price_ht), ctypes.c_double(vat_rate)), 2)

def render_change(amount_cents: int) -> dict:
    denominations = [50000, 20000, 10000, 5000, 2000, 1000, 500, 200, 100, 50, 20, 10, 5, 2, 1]
    n_denom = len(denominations)
    coins_out = (ctypes.c_int * n_denom)()
    lib.render_change(ctypes.c_int(amount_cents), coins_out, ctypes.c_int(n_denom))
    
    result = {}
    for i in range(n_denom):
        # Formatage propre demandé: ex "0.50"
        key = f"{denominations[i] / 100:.2f}"
        result[key] = coins_out[i]
    return result

# ==========================================
# 3. BASE DE DONNÉES ET LOGIQUE METIER
# ==========================================
DB_FILE = os.path.join(os.path.dirname(__file__), 'data', 'db.sqlite')

def get_db():
    os.makedirs(os.path.dirname(DB_FILE), exist_ok=True)
    conn = sqlite3.connect(DB_FILE)
    conn.row_factory = sqlite3.Row
    return conn

def init_db():
    with get_db() as conn:
        conn.executescript("""
        CREATE TABLE IF NOT EXISTS products (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            name TEXT NOT NULL,
            price_ht REAL NOT NULL CHECK (price_ht >= 0),
            vat_rate REAL NOT NULL CHECK (vat_rate IN (0.06, 0.12, 0.21)),
            stock INTEGER NOT NULL DEFAULT 0 CHECK (stock >= 0),
            is_active INTEGER NOT NULL DEFAULT 1 -- AJOUT: Soft delete
        );
        CREATE TABLE IF NOT EXISTS transactions (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            date TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
            total_ht REAL NOT NULL,
            total_vat REAL NOT NULL,
            total_ttc REAL NOT NULL,
            amount_given REAL NOT NULL
        );
        CREATE TABLE IF NOT EXISTS transaction_items (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            transaction_id INTEGER NOT NULL REFERENCES transactions(id),
            product_id INTEGER NOT NULL REFERENCES products(id),
            qty INTEGER NOT NULL CHECK (qty > 0),
            unit_price_ht REAL NOT NULL, -- AJOUT: Historisation
            vat_rate REAL NOT NULL,      -- AJOUT: Historisation
            unit_price_ttc REAL NOT NULL
        );
        """)
        count = conn.execute("SELECT COUNT(*) FROM products").fetchone()[0]
        if count == 0:
            conn.executemany("""
                INSERT INTO products (name, price_ht, vat_rate, stock) VALUES (?, ?, ?, ?)
            """, [
                ("Pain Artisanal", 2.50, 0.06, 150),
                ("Lait Bio 1L", 1.80, 0.06, 100),
                ("Café Arabica 500g", 6.50, 0.06, 80),
                ("Coca-Cola 33cl", 1.50, 0.21, 200),
                ("Sandwich Club", 4.50, 0.12, 50),
                ("Chips Sel 150g", 2.20, 0.21, 120),
                ("Pommes 1kg", 3.00, 0.06, 90),
                ("Bouteille d'eau 1.5L", 0.90, 0.06, 300)
            ])

# --- CRUD Produits ---
def list_products(lowstock=False):
    with get_db() as conn:
        query = "SELECT * FROM products WHERE is_active = 1"
        if lowstock:
            query += " AND stock < 5"
        return [dict(row) for row in conn.execute(query).fetchall()]

def add_product(data: dict):
    with get_db() as conn:
        cursor = conn.cursor()
        cursor.execute("INSERT INTO products (name, price_ht, vat_rate, stock) VALUES (?, ?, ?, ?)",
                       (data['name'], data['price_ht'], data['vat_rate'], data.get('stock', 0)))
        return cursor.lastrowid

def delete_product(product_id: int):
    with get_db() as conn:
        stock = conn.execute("SELECT stock FROM products WHERE id = ?", (product_id,)).fetchone()
        if stock and stock['stock'] > 0:
            raise ValueError("Impossible de supprimer un produit en stock.")
        # SOFT DELETE: On désactive au lieu de détruire la ligne
        conn.execute("UPDATE products SET is_active = 0 WHERE id = ?", (product_id,))
        
def update_product(product_id: int, data: dict):
    with get_db() as conn:
        conn.execute("""
            UPDATE products 
            SET name = ?, price_ht = ?, vat_rate = ?, stock = ? 
            WHERE id = ?
        """, (data['name'], data['price_ht'], data['vat_rate'], data.get('stock', 0), product_id))

# --- Transactions ---
def create_transaction(items: list, amount_given: float) -> dict:
    with get_db() as conn:
        cursor = conn.cursor()
        total_ht, total_vat, total_ttc = 0.0, 0.0, 0.0
        
        for item in items:
            prod = cursor.execute("SELECT * FROM products WHERE id = ? AND is_active = 1", (item['product_id'],)).fetchone()
            if not prod or prod['stock'] < item['qty']:
                raise ValueError(f"Stock insuffisant ou produit invalide pour le produit ID {item['product_id']}")
            
            total_ht += prod['price_ht'] * item['qty']
            total_vat += calc_vat(prod['price_ht'], prod['vat_rate']) * item['qty']
            total_ttc += calc_ttc(prod['price_ht'], prod['vat_rate']) * item['qty']

        total_ht = round(total_ht, 2)
        total_vat = round(total_vat, 2)
        total_ttc = round(total_ttc, 2)
        
        if amount_given < total_ttc:
            raise ValueError("Montant donné insuffisant.")
            
        cursor.execute("INSERT INTO transactions (total_ht, total_vat, total_ttc, amount_given) VALUES (?, ?, ?, ?)",
                       (total_ht, total_vat, total_ttc, amount_given))
        trans_id = cursor.lastrowid
        
        for item in items:
            prod = cursor.execute("SELECT * FROM products WHERE id = ?", (item['product_id'],)).fetchone()
            unit_ttc = calc_ttc(prod['price_ht'], prod['vat_rate'])
            # Insertion des valeurs historisées
            cursor.execute("INSERT INTO transaction_items (transaction_id, product_id, qty, unit_price_ht, vat_rate, unit_price_ttc) VALUES (?, ?, ?, ?, ?, ?)",
                           (trans_id, item['product_id'], item['qty'], prod['price_ht'], prod['vat_rate'], unit_ttc))
            cursor.execute("UPDATE products SET stock = stock - ? WHERE id = ?", (item['qty'], item['product_id']))
            
        change_cents = int(round((amount_given - total_ttc) * 100))
        change_dict = render_change(change_cents)
        
        return {
            "transaction_id": trans_id,
            "total_ttc": total_ttc,
            "change_returned": change_dict
        }

# ==========================================
# 4. STATISTIQUES DASHBOARD (Historisées)
# ==========================================
def daily_summary(date_str=None):
    if not date_str:
        date_str = datetime.now().strftime('%Y-%m-%d')
    with get_db() as conn:
        row = conn.execute("""
            SELECT SUM(total_ttc) as ca_ttc, SUM(total_ht) as ca_ht, 
                   SUM(total_vat) as tva_total, COUNT(id) as n_tickets
            FROM transactions
            WHERE date(date) = date(?)
        """, (date_str,)).fetchone()
        return {
            "ca_ttc": round(row["ca_ttc"] or 0.0, 2),
            "ca_ht": round(row["ca_ht"] or 0.0, 2),
            "tva_total": round(row["tva_total"] or 0.0, 2),
            "n_tickets": row["n_tickets"] or 0
        }

def top_products(n=10):
    with get_db() as conn:
        # Utilise les prix de l'historique de la transaction
        rows = conn.execute("""
            SELECT p.name, SUM(ti.qty * ti.unit_price_ttc) as revenue
            FROM transaction_items ti
            JOIN products p ON ti.product_id = p.id
            GROUP BY p.id
            ORDER BY revenue DESC
            LIMIT ?
        """, (n,)).fetchall()
        return [dict(row) for row in rows]

def get_revenue_by_vat():
    with get_db() as conn:
        # Calcule la TVA basé sur le moment de l'achat !
        rows = conn.execute("""
            SELECT ti.vat_rate, SUM(ti.qty * ti.unit_price_ht * ti.vat_rate) as vat_amount
            FROM transaction_items ti
            GROUP BY ti.vat_rate
        """).fetchall()
        return [dict(row) for row in rows]

def get_daily_history_30_days():
    with get_db() as conn:
        rows = conn.execute("""
            SELECT date(date) as day, SUM(total_ttc) as total
            FROM transactions
            GROUP BY day
            ORDER BY day DESC
            LIMIT 30
        """).fetchall()
        return [{"day": row["day"], "total": round(row["total"], 2)} for row in reversed(rows)]

init_db()