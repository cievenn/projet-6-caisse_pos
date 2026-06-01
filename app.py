# app.py
from flask import Flask, jsonify, request, render_template, redirect
from flask_cors import CORS
import module

app = Flask(__name__)
CORS(app)

@app.route("/")
def home():
    return redirect("/dashboard")

# --- Routes Produits ---
@app.route("/api/products", methods=["GET"])
def get_products():
    lowstock = request.args.get('lowstock', 'false').lower() == 'true'
    data = module.list_products(lowstock)
    return jsonify({"status": "ok", "data": data})

@app.route("/api/products", methods=["POST"])
def add_product():
    try:
        new_id = module.add_product(request.json)
        return jsonify({"status": "ok", "data": {"id": new_id}}), 201
    except Exception as e:
        return jsonify({"status": "error", "message": str(e)}), 400

@app.route("/api/products/<int:id>", methods=["DELETE"])
def delete_product(id):
    try:
        module.delete_product(id)
        return jsonify({"status": "ok", "data": None})
    except Exception as e:
        return jsonify({"status": "error", "message": str(e)}), 400
    
@app.route("/api/products/<int:id>", methods=["PUT"])
def update_product(id):
    try:
        module.update_product(id, request.json)
        return jsonify({"status": "ok", "data": None})
    except Exception as e:
        return jsonify({"status": "error", "message": str(e)}), 400
    
# --- Routes Transactions ---
@app.route("/api/transactions", methods=["POST"])
def create_transaction():
    try:
        body = request.json
        res = module.create_transaction(body['items'], body['amount_given'])
        return jsonify({"status": "ok", "data": res}), 201
    except Exception as e:
        return jsonify({"status": "error", "message": str(e)}), 400

@app.route("/api/transactions/calculate", methods=["POST"])
def calculate_transaction():
    try:
        body = request.json
        res = module.calculate_cart(body['items'])
        return jsonify({"status": "ok", "data": res})
    except Exception as e:
        return jsonify({"status": "error", "message": str(e)}), 400

@app.route("/api/transactions", methods=["GET"])
def get_transactions():
    try:
        date_param = request.args.get('date')
        data = module.list_transactions(date_param)
        return jsonify({"status": "ok", "data": data})
    except Exception as e:
        return jsonify({"status": "error", "message": str(e)}), 500

# --- Routes Statistiques exigées par le Cahier des Charges ---
@app.route("/api/stats/daily", methods=["GET"])
def get_daily_stats():
    date_param = request.args.get('date')
    data = module.daily_summary(date_param)
    return jsonify({"status": "ok", "data": data})

@app.route("/api/stats/top-products", methods=["GET"])
def get_top_products():
    n_param = request.args.get('n', default=10, type=int)
    data = module.top_products(n_param)
    return jsonify({"status": "ok", "data": data})

# Route groupée pour simplifier l'envoi vers Chart.js
@app.route("/api/stats/dashboard", methods=["GET"])
def get_dashboard_stats():
    try:
        data = {
            "history": module.get_daily_history_30_days(),
            "top_products": module.top_products(10),
            "vat_split": module.get_revenue_by_vat(),
            "summary": module.daily_summary()
        }
        return jsonify({"status": "ok", "data": data})
    except Exception as e:
        return jsonify({"status": "error", "message": str(e)}), 500

@app.route("/dashboard")
def dashboard():
    return render_template("dashboard.html")

if __name__ == "__main__":
    app.run(debug=True, port=5000)