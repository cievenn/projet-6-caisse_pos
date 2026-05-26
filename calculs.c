// calculs.c
#include <stdio.h>
#include <math.h>

// --- Fonctions Scalaires ---

double calc_vat(double price_ht, double vat_rate) {
    return price_ht * vat_rate;
}

double calc_ttc(double price_ht, double vat_rate) {
    return price_ht + calc_vat(price_ht, vat_rate);
}

double sum_total(double* prices, int n) {
    double total = 0.0;
    for (int i = 0; i < n; i++) {
        total += prices[i];
    }
    return total;
}

// Rendu de monnaie (Glouton)
int render_change(int cents, int* coins_out, int n_denom) {
    int denominations[] = {50000, 20000, 10000, 5000, 2000, 1000, 500, 200, 100, 50, 20, 10, 5, 2, 1};
    
    for (int i = 0; i < n_denom; i++) {
        coins_out[i] = cents / denominations[i];
        cents %= denominations[i];
    }
    
    return (cents == 0) ? 0 : -1;
}

// --- Fonctions Batch (Pour le Benchmark) ---
void calc_ttc_batch(double* p, double* r, double* out, int n) {
    for (int i = 0; i < n; i++) {
        out[i] = p[i] + (p[i] * r[i]);
    }
}