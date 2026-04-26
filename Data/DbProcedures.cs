using System;
using System.Data;
using Microsoft.EntityFrameworkCore;
using MyHippocrates.Models;
using Npgsql;
using NpgsqlTypes;

namespace MyHippocrates.Data
{
    public static class DbProcedures
    {
        // ── helpers ────────────────────────────────────────────────
        private static NpgsqlConnection OpenConnection(AppDbContext ctx)
        {
            ctx.Database.OpenConnection();
            return (NpgsqlConnection)ctx.Database.GetDbConnection();
        }

        private static NpgsqlCommand Cmd(string proc, NpgsqlConnection cn, NpgsqlTransaction tx)
        {
            var cmd = cn.CreateCommand();
            cmd.CommandText = proc;
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Transaction = tx;
            return cmd;
        }

        // ══ MANUFACTURER ══════════════════════════════════════════

        public static int AddManufacturer(AppDbContext ctx, Manufacturer m)
        {
            using var cn = OpenConnection(ctx);
            using var tx = cn.BeginTransaction();
            try
            {
                using var cmd = Cmd("sp_add_manufacturer", cn, tx);
                cmd.Parameters.AddWithValue("p_name", m.Name);
                cmd.Parameters.AddWithValue("p_country", m.Country);
                cmd.Parameters.AddWithValue("p_address", m.Address);
                cmd.Parameters.AddWithValue("p_phone", m.Phone);
                cmd.Parameters.AddWithValue("p_email", m.Email);
                var pOut = new NpgsqlParameter("p_id", NpgsqlDbType.Integer)
                { Direction = ParameterDirection.Output };
                cmd.Parameters.Add(pOut);
                cmd.ExecuteNonQuery();
                tx.Commit();
                return (int)pOut.Value!;
            }
            catch { tx.Rollback(); throw; }
        }

        public static void UpdateManufacturer(AppDbContext ctx, Manufacturer m)
        {
            using var cn = OpenConnection(ctx);
            using var tx = cn.BeginTransaction();
            try
            {
                using var cmd = Cmd("sp_update_manufacturer", cn, tx);
                cmd.Parameters.AddWithValue("p_id", m.Id);
                cmd.Parameters.AddWithValue("p_name", m.Name);
                cmd.Parameters.AddWithValue("p_country", m.Country);
                cmd.Parameters.AddWithValue("p_address", m.Address);
                cmd.Parameters.AddWithValue("p_phone", m.Phone);
                cmd.Parameters.AddWithValue("p_email", m.Email);
                cmd.ExecuteNonQuery();
                tx.Commit();
            }
            catch { tx.Rollback(); throw; }
        }

        public static void DeleteManufacturer(AppDbContext ctx, int id)
        {
            using var cn = OpenConnection(ctx);
            using var tx = cn.BeginTransaction();
            try
            {
                using var cmd = Cmd("sp_delete_manufacturer", cn, tx);
                cmd.Parameters.AddWithValue("p_id", id);
                cmd.ExecuteNonQuery();
                tx.Commit();
            }
            catch { tx.Rollback(); throw; }
        }

        // ══ PRODUCT ════════════════════════════════════════════════

        public static int AddProduct(AppDbContext ctx, Product p)
        {
            using var cn = OpenConnection(ctx);
            using var tx = cn.BeginTransaction();
            try
            {
                using var cmd = Cmd("sp_add_product", cn, tx);
                cmd.Parameters.AddWithValue("p_name", p.Name);
                cmd.Parameters.AddWithValue("p_manufacturer_id", p.ManufacturerId);
                cmd.Parameters.AddWithValue("p_production_date", p.ProductionDate.Date);
                cmd.Parameters.AddWithValue("p_expiration_date", p.ExpirationDate.Date);
                cmd.Parameters.AddWithValue("p_unit", p.Unit);
                cmd.Parameters.AddWithValue("p_description", (object?)p.Description ?? DBNull.Value);
                cmd.Parameters.AddWithValue("p_prescription_required", p.PrescriptionRequired);
                cmd.Parameters.AddWithValue("p_purchase_price", p.PurchasePrice);
                cmd.Parameters.AddWithValue("p_sale_price", p.SalePrice);
                var pOut = new NpgsqlParameter("p_id", NpgsqlDbType.Integer)
                { Direction = ParameterDirection.Output };
                cmd.Parameters.Add(pOut);
                cmd.ExecuteNonQuery();
                tx.Commit();
                return (int)pOut.Value!;
            }
            catch { tx.Rollback(); throw; }
        }

        public static void UpdateProduct(AppDbContext ctx, Product p)
        {
            using var cn = OpenConnection(ctx);
            using var tx = cn.BeginTransaction();
            try
            {
                using var cmd = Cmd("sp_update_product", cn, tx);
                cmd.Parameters.AddWithValue("p_id", p.Id);
                cmd.Parameters.AddWithValue("p_name", p.Name);
                cmd.Parameters.AddWithValue("p_manufacturer_id", p.ManufacturerId);
                cmd.Parameters.AddWithValue("p_production_date", p.ProductionDate.Date);
                cmd.Parameters.AddWithValue("p_expiration_date", p.ExpirationDate.Date);
                cmd.Parameters.AddWithValue("p_unit", p.Unit);
                cmd.Parameters.AddWithValue("p_description", (object?)p.Description ?? DBNull.Value);
                cmd.Parameters.AddWithValue("p_prescription_required", p.PrescriptionRequired);
                cmd.Parameters.AddWithValue("p_purchase_price", p.PurchasePrice);
                cmd.Parameters.AddWithValue("p_sale_price", p.SalePrice);
                cmd.ExecuteNonQuery();
                tx.Commit();
            }
            catch { tx.Rollback(); throw; }
        }

        public static void DeleteProduct(AppDbContext ctx, int id)
        {
            using var cn = OpenConnection(ctx);
            using var tx = cn.BeginTransaction();
            try
            {
                using var cmd = Cmd("sp_delete_product", cn, tx);
                cmd.Parameters.AddWithValue("p_id", id);
                cmd.ExecuteNonQuery();
                tx.Commit();
            }
            catch { tx.Rollback(); throw; }
        }

        // ══ PHARMACY ═══════════════════════════════════════════════

        public static int AddPharmacy(AppDbContext ctx, Pharmacy p)
        {
            using var cn = OpenConnection(ctx);
            using var tx = cn.BeginTransaction();
            try
            {
                using var cmd = Cmd("sp_add_pharmacy", cn, tx);
                cmd.Parameters.AddWithValue("p_address", p.Address);
                cmd.Parameters.AddWithValue("p_phone", p.Phone);
                cmd.Parameters.AddWithValue("p_working_hours", p.WorkingHours);
                var pOut = new NpgsqlParameter("p_id", NpgsqlDbType.Integer)
                { Direction = ParameterDirection.Output };
                cmd.Parameters.Add(pOut);
                cmd.ExecuteNonQuery();
                tx.Commit();
                return (int)pOut.Value!;
            }
            catch { tx.Rollback(); throw; }
        }

        public static void UpdatePharmacy(AppDbContext ctx, Pharmacy p)
        {
            using var cn = OpenConnection(ctx);
            using var tx = cn.BeginTransaction();
            try
            {
                using var cmd = Cmd("sp_update_pharmacy", cn, tx);
                cmd.Parameters.AddWithValue("p_id", p.Id);
                cmd.Parameters.AddWithValue("p_address", p.Address);
                cmd.Parameters.AddWithValue("p_phone", p.Phone);
                cmd.Parameters.AddWithValue("p_working_hours", p.WorkingHours);
                cmd.ExecuteNonQuery();
                tx.Commit();
            }
            catch { tx.Rollback(); throw; }
        }

        public static void DeletePharmacy(AppDbContext ctx, int id)
        {
            using var cn = OpenConnection(ctx);
            using var tx = cn.BeginTransaction();
            try
            {
                using var cmd = Cmd("sp_delete_pharmacy", cn, tx);
                cmd.Parameters.AddWithValue("p_id", id);
                cmd.ExecuteNonQuery();
                tx.Commit();
            }
            catch { tx.Rollback(); throw; }
        }

        // ══ EMPLOYEE ═══════════════════════════════════════════════

        public static int AddEmployee(AppDbContext ctx, Employee e)
        {
            using var cn = OpenConnection(ctx);
            using var tx = cn.BeginTransaction();
            try
            {
                using var cmd = Cmd("sp_add_employee", cn, tx);
                cmd.Parameters.AddWithValue("p_full_name", e.FullName);
                cmd.Parameters.AddWithValue("p_idnp", e.Idnp);
                cmd.Parameters.AddWithValue("p_phone", e.Phone);
                cmd.Parameters.AddWithValue("p_address", (object?)e.Address ?? DBNull.Value);
                cmd.Parameters.AddWithValue("p_salary", e.Salary);
                cmd.Parameters.AddWithValue("p_position", e.Position);
                var pOut = new NpgsqlParameter("p_id", NpgsqlDbType.Integer)
                { Direction = ParameterDirection.Output };
                cmd.Parameters.Add(pOut);
                cmd.ExecuteNonQuery();
                tx.Commit();
                return (int)pOut.Value!;
            }
            catch { tx.Rollback(); throw; }
        }

        public static void UpdateEmployee(AppDbContext ctx, Employee e)
        {
            using var cn = OpenConnection(ctx);
            using var tx = cn.BeginTransaction();
            try
            {
                using var cmd = Cmd("sp_update_employee", cn, tx);
                cmd.Parameters.AddWithValue("p_id", e.Id);
                cmd.Parameters.AddWithValue("p_full_name", e.FullName);
                cmd.Parameters.AddWithValue("p_idnp", e.Idnp);
                cmd.Parameters.AddWithValue("p_phone", e.Phone);
                cmd.Parameters.AddWithValue("p_address", (object?)e.Address ?? DBNull.Value);
                cmd.Parameters.AddWithValue("p_salary", e.Salary);
                cmd.Parameters.AddWithValue("p_position", e.Position);
                cmd.ExecuteNonQuery();
                tx.Commit();
            }
            catch { tx.Rollback(); throw; }
        }

        public static void DeleteEmployee(AppDbContext ctx, int id)
        {
            using var cn = OpenConnection(ctx);
            using var tx = cn.BeginTransaction();
            try
            {
                using var cmd = Cmd("sp_delete_employee", cn, tx);
                cmd.Parameters.AddWithValue("p_id", id);
                cmd.ExecuteNonQuery();
                tx.Commit();
            }
            catch { tx.Rollback(); throw; }
        }

        // ══ RECEIPT ════════════════════════════════════════════════

        public static int AddReceipt(AppDbContext ctx, Receipt r)
        {
            using var cn = OpenConnection(ctx);
            using var tx = cn.BeginTransaction();
            try
            {
                using var cmd = Cmd("sp_add_receipt", cn, tx);
                cmd.Parameters.AddWithValue("p_receipt_number", r.ReceiptNumber);
                cmd.Parameters.AddWithValue("p_pharmacy_id", r.PharmacyId);
                cmd.Parameters.AddWithValue("p_employee_id", r.EmployeeId);
                cmd.Parameters.AddWithValue("p_date", r.Date.Date);
                cmd.Parameters.AddWithValue("p_time", r.Time);
                var pOut = new NpgsqlParameter("p_id", NpgsqlDbType.Integer)
                { Direction = ParameterDirection.Output };
                cmd.Parameters.Add(pOut);
                cmd.ExecuteNonQuery();
                tx.Commit();
                return (int)pOut.Value!;
            }
            catch { tx.Rollback(); throw; }
        }

        public static void UpdateReceipt(AppDbContext ctx, Receipt r)
        {
            using var cn = OpenConnection(ctx);
            using var tx = cn.BeginTransaction();
            try
            {
                using var cmd = Cmd("sp_update_receipt", cn, tx);
                cmd.Parameters.AddWithValue("p_id", r.Id);
                cmd.Parameters.AddWithValue("p_receipt_number", r.ReceiptNumber);
                cmd.Parameters.AddWithValue("p_pharmacy_id", r.PharmacyId);
                cmd.Parameters.AddWithValue("p_employee_id", r.EmployeeId);
                cmd.Parameters.AddWithValue("p_date", r.Date.Date);
                cmd.Parameters.AddWithValue("p_time", r.Time);
                cmd.ExecuteNonQuery();
                tx.Commit();
            }
            catch { tx.Rollback(); throw; }
        }

        public static void DeleteReceipt(AppDbContext ctx, int id)
        {
            using var cn = OpenConnection(ctx);
            using var tx = cn.BeginTransaction();
            try
            {
                using var cmd = Cmd("sp_delete_receipt", cn, tx);
                cmd.Parameters.AddWithValue("p_id", id);
                cmd.ExecuteNonQuery();
                tx.Commit();
            }
            catch { tx.Rollback(); throw; }
        }

        // ══ ORDER ITEM ═════════════════════════════════════════════

        public static void AddOrderItem(AppDbContext ctx, OrderItem o)
        {
            using var cn = OpenConnection(ctx);
            using var tx = cn.BeginTransaction();
            try
            {
                using var cmd = Cmd("sp_add_order_item", cn, tx);
                cmd.Parameters.AddWithValue("p_receipt_id", o.ReceiptId);
                cmd.Parameters.AddWithValue("p_product_id", o.ProductId);
                cmd.Parameters.AddWithValue("p_quantity", o.Quantity);
                cmd.Parameters.AddWithValue("p_discount", o.Discount);
                cmd.ExecuteNonQuery();
                tx.Commit();
            }
            catch { tx.Rollback(); throw; }
        }

        public static void UpdateOrderItem(AppDbContext ctx,
            int oldReceiptId, int oldProductId, OrderItem o)
        {
            using var cn = OpenConnection(ctx);
            using var tx = cn.BeginTransaction();
            try
            {
                using var cmd = Cmd("sp_update_order_item", cn, tx);
                cmd.Parameters.AddWithValue("p_receipt_id", oldReceiptId);
                cmd.Parameters.AddWithValue("p_product_id", oldProductId);
                cmd.Parameters.AddWithValue("p_new_receipt_id", o.ReceiptId);
                cmd.Parameters.AddWithValue("p_new_product_id", o.ProductId);
                cmd.Parameters.AddWithValue("p_quantity", o.Quantity);
                cmd.Parameters.AddWithValue("p_discount", o.Discount);
                cmd.ExecuteNonQuery();
                tx.Commit();
            }
            catch { tx.Rollback(); throw; }
        }

        public static void DeleteOrderItem(AppDbContext ctx, int receiptId, int productId)
        {
            using var cn = OpenConnection(ctx);
            using var tx = cn.BeginTransaction();
            try
            {
                using var cmd = Cmd("sp_delete_order_item", cn, tx);
                cmd.Parameters.AddWithValue("p_receipt_id", receiptId);
                cmd.Parameters.AddWithValue("p_product_id", productId);
                cmd.ExecuteNonQuery();
                tx.Commit();
            }
            catch { tx.Rollback(); throw; }
        }

        // ══ STOCK BALANCE ══════════════════════════════════════════

        public static void AddStockBalance(AppDbContext ctx, StockBalance s)
        {
            using var cn = OpenConnection(ctx);
            using var tx = cn.BeginTransaction();
            try
            {
                using var cmd = Cmd("sp_add_stock_balance", cn, tx);
                cmd.Parameters.AddWithValue("p_pharmacy_id", s.PharmacyId);
                cmd.Parameters.AddWithValue("p_product_id", s.ProductId);
                cmd.Parameters.AddWithValue("p_remaining_qty", s.RemainingQty);
                cmd.ExecuteNonQuery();
                tx.Commit();
            }
            catch { tx.Rollback(); throw; }
        }

        public static void UpdateStockBalance(AppDbContext ctx,
            int oldPharmacyId, int oldProductId, StockBalance s)
        {
            using var cn = OpenConnection(ctx);
            using var tx = cn.BeginTransaction();
            try
            {
                using var cmd = Cmd("sp_update_stock_balance", cn, tx);
                cmd.Parameters.AddWithValue("p_pharmacy_id", oldPharmacyId);
                cmd.Parameters.AddWithValue("p_product_id", oldProductId);
                cmd.Parameters.AddWithValue("p_new_pharmacy_id", s.PharmacyId);
                cmd.Parameters.AddWithValue("p_new_product_id", s.ProductId);
                cmd.Parameters.AddWithValue("p_remaining_qty", s.RemainingQty);
                cmd.ExecuteNonQuery();
                tx.Commit();
            }
            catch { tx.Rollback(); throw; }
        }

        public static void DeleteStockBalance(AppDbContext ctx, int pharmacyId, int productId)
        {
            using var cn = OpenConnection(ctx);
            using var tx = cn.BeginTransaction();
            try
            {
                using var cmd = Cmd("sp_delete_stock_balance", cn, tx);
                cmd.Parameters.AddWithValue("p_pharmacy_id", pharmacyId);
                cmd.Parameters.AddWithValue("p_product_id", productId);
                cmd.ExecuteNonQuery();
                tx.Commit();
            }
            catch { tx.Rollback(); throw; }
        }
    }
}