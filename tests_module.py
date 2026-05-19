import pytest
import module
import sqlite3
from datetime import datetime

@pytest.fixture(autouse=True)
def setup_db():
    module.init_db()
    with module.get_db() as conn:
        conn.execute("DELETE FROM transaction_items")
        conn.execute("DELETE FROM transactions")
        conn.execute("DELETE FROM products")
        conn.execute("INSERT INTO products (id, name, price_ht, vat_rate, stock) VALUES (1, 'Produit Test', 100.0, 0.21, 10)")
        conn.execute("INSERT INTO products (id, name, price_ht, vat_rate, stock) VALUES (2, 'Produit Faible', 50.0, 0.06, 2)")

def test_calc_vat_21_percent():
    assert module.calc_vat(100.0, 0.21) == 21.00

def test_calc_ttc_basic():
    assert module.calc_ttc(100.0, 0.21) == 121.00

def test_render_change_exact_amount():
    # 47.28 € à rendre = 4728 cents
    res = module.render_change(4728)
    assert res['20'] == 2
    assert res['5'] == 1
    assert res['2'] == 1
    assert res['0.2'] == 1
    assert res['0.05'] == 1
    assert res['0.02'] == 1
    assert res['0.01'] == 1

def test_render_change_greedy_minimum():
    # 500€ doit donner 1x billet 500, pas de petites coupures
    res = module.render_change(50000)
    assert res['500'] == 1
    assert sum(res.values()) == 1 # Une seule pièce/billet au total

def test_create_transaction_decrements_stock():
    module.create_transaction([{"product_id": 1, "qty": 3}], 400.0)
    prods = module.list_products()
    p1 = next(p for p in prods if p['id'] == 1)
    assert p1['stock'] == 7 # 10 - 3 = 7

def test_create_transaction_insufficient_stock_rejected():
    with pytest.raises(ValueError, match="Stock insuffisant"):
        module.create_transaction([{"product_id": 1, "qty": 99}], 10000.0)

def test_low_stock_alert_threshold_5():
    low_stock = module.list_products(lowstock=True)
    assert len(low_stock) == 1
    assert low_stock[0]['id'] == 2 # Le produit avec 2 en stock

def test_daily_summary_totals_match():
    # On fait une vente à 121€ TTC
    module.create_transaction([{"product_id": 1, "qty": 1}], 150.0)
    today = datetime.now().strftime('%Y-%m-%d')
    stats = module.daily_summary(today)
    assert stats['ca_ttc'] == 121.00
    assert stats['n_tickets'] == 1

def test_top_products_sorted_by_revenue():
    module.create_transaction([{"product_id": 1, "qty": 1}], 150.0) # 121€ TTC
    top = module.top_products(10)
    assert len(top) > 0
    assert top[0]['name'] == 'Produit Test'
    assert top[0]['revenue'] == 121.00

def test_delete_product_with_stock_rejected():
    with pytest.raises(ValueError):
        module.delete_product(1) # Le produit 1 a 10 en stock, il ne peut pas être supprimé