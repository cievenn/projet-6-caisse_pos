import requests, random, time, argparse
from datetime import date, timedelta

BASE = "http://localhost:5000"

# ── Générateurs par projet ────────────────────────────────────────────────────

def gen_projet1(n, delay):
    """Station de mesures physiques"""
    types   = ['temperature', 'pressure', 'humidity']
    ranges  = {'temperature':(-10,45), 'pressure':(950,1060), 'humidity':(10,95)}
    units   = {'temperature':'°C', 'pressure':'hPa', 'humidity':'%'}
    for i in range(n):
        t   = random.choice(types)
        lo, hi = ranges[t]
        val = round(random.uniform(lo, hi * (1.15 if random.random()<.08 else 1)), 2)
        r   = requests.post(f"{BASE}/api/measures", json={"type":t,"value":val,"unit":units[t]})
        _log(i+1, n, r.status_code, f"{t}={val}")
        time.sleep(delay)

def gen_projet2(n, delay):
    """Suivi sportif & fitness"""
    types = ['course', 'vélo', 'natation', 'musculation', 'yoga']
    mets  = {'course':8.0, 'vélo':6.0, 'natation':7.0, 'musculation':4.0, 'yoga':2.5}
    for i in range(n):
        t  = random.choice(types)
        dur = random.randint(20, 90)
        r  = requests.post(f"{BASE}/api/sessions", json={
            "type": t, "duration_min": dur,
            "met_value": mets[t] + random.uniform(-0.5, 0.5),
            "date": str(date.today() - timedelta(days=random.randint(0,30)))
        })
        _log(i+1, n, r.status_code, f"{t} {dur}min")
        time.sleep(delay)

def gen_projet3(n, delay):
    """Tournoi ELO — génère des matchs entre joueurs existants"""
    players = requests.get(f"{BASE}/api/players").json().get("data", [])
    if len(players) < 2:
        print("Ajoute d'abord au moins 2 joueurs via la GUI.")
        return
    for i in range(n):
        a, b = random.sample(players, 2)
        res  = random.choice([1.0, 0.5, 0.0])
        r    = requests.post(f"{BASE}/api/matches",
               json={"player_a_id":a["id"],"player_b_id":b["id"],"result":res})
        _log(i+1, n, r.status_code, f"{a['name']} vs {b['name']} → {res}")
        time.sleep(delay)

def gen_projet6(n, delay):
    """Caisse POS — génère des transactions"""
    products = requests.get(f"{BASE}/api/products").json().get("data", [])
    if not products:
        print("Ajoute d'abord des produits via la GUI.")
        return
    for i in range(n):
        items = [{"product_id": p["id"], "qty": random.randint(1,4)}
                 for p in random.sample(products, k=min(random.randint(1,4), len(products)))]
        r = requests.post(f"{BASE}/api/transactions",
            json={"items": items, "amount_given": 500.0})
        _log(i+1, n, r.status_code, f"{len(items)} article(s)")
        time.sleep(delay)

def gen_projet8(n, delay):
    """Notes scolaires"""
    students = requests.get(f"{BASE}/api/students").json().get("data", [])
    courses  = requests.get(f"{BASE}/api/courses").json().get("data", [])
    if not students or not courses:
        print("Ajoute d'abord des élèves et des cours.")
        return
    for i in range(n):
        s = random.choice(students)
        c = random.choice(courses)
        r = requests.post(f"{BASE}/api/grades", json={
            "student_id": s["id"], "course_id": c["id"],
            "value": round(random.gauss(12, 3.5), 1),
            "weight": random.choice([1.0, 1.0, 1.0, 2.0]),
            "date": str(date.today())
        })
        _log(i+1, n, r.status_code, f"{s['name']} — {c['name']}")
        time.sleep(delay)

def gen_projet10(n, delay):
    """Calculatrice scientifique"""
    ops = ["sqrt({})", "power({},{})", "gcd({},{})", "factorial({})", "lcm({},{})"]
    for i in range(n):
        expr = random.choice(ops)
        if expr.count('{}') == 1:
            expr = expr.format(random.randint(1, 144))
        else:
            a, b = random.randint(1,50), random.randint(1,50)
            expr = expr.format(a, b)
        r = requests.post(f"{BASE}/api/calculate", json={"expression": expr})
        _log(i+1, n, r.status_code, expr)
        time.sleep(delay)

# ── Utilitaire ────────────────────────────────────────────────────────────────

def _log(i, n, code, info):
    ok = "✅" if code in (200,201) else "❌"
    print(f"  [{i:>4}/{n}] {ok} HTTP {code} | {info}")

GENS = {1:gen_projet1, 2:gen_projet2, 3:gen_projet3,
        6:gen_projet6, 8:gen_projet8, 10:gen_projet10}

# ── CLI ───────────────────────────────────────────────────────────────────────

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Générateur de données TTINFO")
    parser.add_argument("--projet", type=int, choices=[1,2,3,6,8,10], required=True,
                        help="Numéro du projet (1/2/3/6/8/10)")
    parser.add_argument("--n",      type=int,   default=50,
                        help="Nombre d'entrées à générer (défaut: 50)")
    parser.add_argument("--delay",  type=float, default=0.2,
                        help="Délai entre chaque requête en secondes (défaut: 0.2)")
    parser.add_argument("--url",    default="http://localhost:5000",
                        help="URL de base de l'API Flask")
    args = parser.parse_args()

    BASE = args.url
    print(f"\n🚀 Générateur — Projet {args.projet} — {args.n} entrées @ {args.delay}s/req")
    print(f"   API cible : {BASE}\n")
    GENS[args.projet](args.n, args.delay)
    print(f"\n✅ Terminé.")