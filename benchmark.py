# benchmark.py
import ctypes
import time
import module

NB_BATCHES = 10
N = 1_000_000

def bench(label, fn):
    times = []
    for _ in range(NB_BATCHES):
        t0 = time.perf_counter()
        fn()
        t1 = time.perf_counter()
        times.append(t1 - t0)
    total = sum(times)
    moy = total / NB_BATCHES
    return label, total, moy

# Préparation des données
prices = [10.0] * N
rates = [0.21] * N
prices_c = (ctypes.c_double * N)(*prices)
rates_c = (ctypes.c_double * N)(*rates)
out_c = (ctypes.c_double * N)()

# A. Python Pur
def python_pur():
    out = []
    for i in range(N):
        out.append(prices[i] + (prices[i] * rates[i]))

# B. ctypes scalaire
def ctypes_scalaire():
    for i in range(N):
        module.lib.calc_ttc(ctypes.c_double(prices[i]), ctypes.c_double(rates[i]))

# C. ctypes batch
def ctypes_batch():
    module.lib.calc_ttc_batch(prices_c, rates_c, out_c, N)

if __name__ == "__main__":
    print(f"Calcul de {N} éléments sur {NB_BATCHES} batches...\n")
    
    _, t_py_total, t_py_moy = bench("Python pur", python_pur)
    _, t_sc_total, t_sc_moy = bench("ctypes scalaire", ctypes_scalaire)
    _, t_ba_total, t_ba_moy = bench("ctypes batch", ctypes_batch)
    
    print(f"{'# Scénario':<20} | {'Temps total':<10} | {'Moy./batch':<12} | {'Facteur'}")
    print("-" * 65)
    print(f"{'A - Python pur':<20} | {t_py_total:.2f} s   | {t_py_moy*1000:.1f} ms    | x1.0")
    print(f"{'B - ctypes scalaire':<20} | {t_sc_total:.2f} s   | {t_sc_moy*1000:.1f} ms    | x{t_sc_total/t_py_total:.1f}")
    print(f"{'C - ctypes batch':<20} | {t_ba_total:.2f} s   | {t_ba_moy*1000:.1f} ms    | x{t_ba_total/t_py_total:.2f}")
    print("\nExplication orale : L'overhead de conversion des types et le passage Python->C à chaque itération rend le scalaire plus lent. Le batch envoie tout le tableau d'un coup, limitant cet overhead à 1 appel.")