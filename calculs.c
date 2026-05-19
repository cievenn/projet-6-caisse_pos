// calculs.c
#include <stdio.h>
#include <math.h>

// --- Fonctions Scalaires ---

// Montant de la TVA
float calc_vat(float price_ht, float vat_rate) {
    return price_ht * vat_rate;
}

// Prix TTC
float calc_ttc(float price_ht, float vat_rate) {
    return price_ht + calc_vat(price_ht, vat_rate);
}

// Somme d'un panier
float sum_total(float* prices, int n) {
    float total = 0.0f;
    for (int i = 0; i < n; i++) {
        total += prices[i];
    }
    return total;
}

// Rendu de monnaie (Glouton)
// Retourne 0 si réussi, -1 si impossible
int render_change(int cents, int* coins_out, int n_denom) {
    // Les dénominations en centimes: 50000, 20000, 10000, 5000, 2000, 1000, 500, 200, 100, 50, 20, 10, 5, 2, 1
    int denominations[] = {50000, 20000, 10000, 5000, 2000, 1000, 500, 200, 100, 50, 20, 10, 5, 2, 1};
    
    for (int i = 0; i < n_denom; i++) {
        coins_out[i] = cents / denominations[i];
        cents %= denominations[i];
    }
    
    return (cents == 0) ? 0 : -1;
}

// --- Fonctions Batch (Pour le Benchmark) ---

void calc_ttc_batch(float* p, float* r, float* out, int n) {
    for (int i = 0; i < n; i++) {
        out[i] = p[i] + (p[i] * r[i]);
    }
}