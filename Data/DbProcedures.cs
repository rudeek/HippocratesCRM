using System;
using System.Data;
using MyHippocrates.Models;
using Npgsql;
using NpgsqlTypes;

namespace MyHippocrates.Data
{
    public static class DbProcedures
    {
        // ══ MANUFACTURER ══════════════════════════════════════════

        public static int AddManufacturer(AppDbContext ctx, Manufacturer m)
        {
            using var cn = new NpgsqlConnection(ctx.СonnectionString);
            cn.Open();
            using var tx = cn.BeginTransaction();
            try
            {
                using var cmd = cn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = "CALL sp_add_manufacturer(@p_name, @p_country, @p_address, @p_phone, @p_email, @p_id)";
                cmd.Parameters.AddWithValue("p_name", m.Name);
                cmd.Parameters.AddWithValue("p_country", m.Country);
                cmd.Parameters.AddWithValue("p_address", m.Address);
                cmd.Parameters.AddWithValue("p_phone", m.Phone);
                cmd.Parameters.AddWithValue("p_email", m.Email);
                var pOut = new NpgsqlParameter("p_id", NpgsqlDbType.Integer)
                { Direction = ParameterDirection.InputOutput, Value = 0 };
                cmd.Parameters.Add(pOut);
                cmd.ExecuteNonQuery();
                tx.Commit();
                return (int)pOut.Value!;
            }
            catch { tx.Rollback(); throw; }
        }

        public static void UpdateManufacturer(AppDbContext ctx, Manufacturer m)
        {
            using var cn = new NpgsqlConnection(ctx.СonnectionString);
            cn.Open();
            using var tx = cn.BeginTransaction();
            try
            {
                using var cmd = cn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = "CALL sp_update_manufacturer(@p_id, @p_name, @p_country, @p_address, @p_phone, @p_email)";
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
            using var cn = new NpgsqlConnection(ctx.СonnectionString);
            cn.Open();
            using var tx = cn.BeginTransaction();
            try
            {
                using var cmd = cn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = "CALL sp_delete_manufacturer(@p_id)";
                cmd.Parameters.AddWithValue("p_id", id);
                cmd.ExecuteNonQuery();
                tx.Commit();
            }
            catch { tx.Rollback(); throw; }
        }

        // ══ PRODUCT ════════════════════════════════════════════════

        public static int AddProduct(AppDbContext ctx, Product p)
        {
            using var cn = new NpgsqlConnection(ctx.СonnectionString);
            cn.Open();
            using var tx = cn.BeginTransaction();
            try
            {
                using var cmd = cn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = "CALL sp_add_product(@p_name, @p_manufacturer_id, @p_production_date, @p_expiration_date, @p_unit, @p_description, @p_prescription_required, @p_purchase_price, @p_sale_price, @p_id)";
                cmd.Parameters.AddWithValue("p_name", p.Name);
                cmd.Parameters.AddWithValue("p_manufacturer_id", p.ManufacturerId);
                cmd.Parameters.Add(new NpgsqlParameter("p_production_date", NpgsqlDbType.Date) { Value = p.ProductionDate.Date });
                cmd.Parameters.Add(new NpgsqlParameter("p_expiration_date", NpgsqlDbType.Date) { Value = p.ExpirationDate.Date });
                cmd.Parameters.AddWithValue("p_unit", p.Unit);
                cmd.Parameters.AddWithValue("p_description", (object?)p.Description ?? DBNull.Value);
                cmd.Parameters.AddWithValue("p_prescription_required", p.PrescriptionRequired);
                cmd.Parameters.AddWithValue("p_purchase_price", p.PurchasePrice);
                cmd.Parameters.AddWithValue("p_sale_price", p.SalePrice);
                var pOut = new NpgsqlParameter("p_id", NpgsqlDbType.Integer)
                { Direction = ParameterDirection.InputOutput, Value = 0 };
                cmd.Parameters.Add(pOut);
                cmd.ExecuteNonQuery();
                tx.Commit();
                return (int)pOut.Value!;
            }
            catch { tx.Rollback(); throw; }
        }

        public static void UpdateProduct(AppDbContext ctx, Product p)
        {
            using var cn = new NpgsqlConnection(ctx.СonnectionString);
            cn.Open();
            using var tx = cn.BeginTransaction();
            try
            {
                using var cmd = cn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = "CALL sp_update_product(@p_id, @p_name, @p_manufacturer_id, @p_production_date, @p_expiration_date, @p_unit, @p_description, @p_prescription_required, @p_purchase_price, @p_sale_price)";
                cmd.Parameters.AddWithValue("p_id", p.Id);
                cmd.Parameters.AddWithValue("p_name", p.Name);
                cmd.Parameters.AddWithValue("p_manufacturer_id", p.ManufacturerId);
                cmd.Parameters.Add(new NpgsqlParameter("p_production_date", NpgsqlDbType.Date) { Value = p.ProductionDate.Date });
                cmd.Parameters.Add(new NpgsqlParameter("p_expiration_date", NpgsqlDbType.Date) { Value = p.ExpirationDate.Date });
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
            using var cn = new NpgsqlConnection(ctx.СonnectionString);
            cn.Open();
            using var tx = cn.BeginTransaction();
            try
            {
                using var cmd = cn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = "CALL sp_delete_product(@p_id)";
                cmd.Parameters.AddWithValue("p_id", id);
                cmd.ExecuteNonQuery();
                tx.Commit();
            }
            catch { tx.Rollback(); throw; }
        }

        // ══ PHARMACY ═══════════════════════════════════════════════

        public static int AddPharmacy(AppDbContext ctx, Pharmacy p)
        {
            using var cn = new NpgsqlConnection(ctx.СonnectionString);
            cn.Open();
            using var tx = cn.BeginTransaction();
            try
            {
                using var cmd = cn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = "CALL sp_add_pharmacy(@p_address, @p_phone, @p_working_hours, @p_id)";
                cmd.Parameters.AddWithValue("p_address", p.Address);
                cmd.Parameters.AddWithValue("p_phone", p.Phone);
                cmd.Parameters.AddWithValue("p_working_hours", p.WorkingHours);
                var pOut = new NpgsqlParameter("p_id", NpgsqlDbType.Integer)
                { Direction = ParameterDirection.InputOutput, Value = 0 };
                cmd.Parameters.Add(pOut);
                cmd.ExecuteNonQuery();
                tx.Commit();
                return (int)pOut.Value!;
            }
            catch { tx.Rollback(); throw; }
        }

        public static void UpdatePharmacy(AppDbContext ctx, Pharmacy p)
        {
            using var cn = new NpgsqlConnection(ctx.СonnectionString);
            cn.Open();
            using var tx = cn.BeginTransaction();
            try
            {
                using var cmd = cn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = "CALL sp_update_pharmacy(@p_id, @p_address, @p_phone, @p_working_hours)";
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
            using var cn = new NpgsqlConnection(ctx.СonnectionString);
            cn.Open();
            using var tx = cn.BeginTransaction();
            try
            {
                using var cmd = cn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = "CALL sp_delete_pharmacy(@p_id)";
                cmd.Parameters.AddWithValue("p_id", id);
                cmd.ExecuteNonQuery();
                tx.Commit();
            }
            catch { tx.Rollback(); throw; }
        }

        // ══ EMPLOYEE ═══════════════════════════════════════════════

        public static int AddEmployee(AppDbContext ctx, Employee e)
        {
            using var cn = new NpgsqlConnection(ctx.СonnectionString);
            cn.Open();
            using var tx = cn.BeginTransaction();
            try
            {
                using var cmd = cn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = "CALL sp_add_employee(@p_full_name, @p_idnp, @p_phone, @p_address, @p_salary, @p_position, @p_id)";
                cmd.Parameters.AddWithValue("p_full_name", e.FullName);
                cmd.Parameters.AddWithValue("p_idnp", e.Idnp);
                cmd.Parameters.AddWithValue("p_phone", e.Phone);
                cmd.Parameters.AddWithValue("p_address", (object?)e.Address ?? DBNull.Value);
                cmd.Parameters.AddWithValue("p_salary", e.Salary);
                cmd.Parameters.AddWithValue("p_position", e.Position);
                var pOut = new NpgsqlParameter("p_id", NpgsqlDbType.Integer)
                { Direction = ParameterDirection.InputOutput, Value = 0 };
                cmd.Parameters.Add(pOut);
                cmd.ExecuteNonQuery();
                tx.Commit();
                return (int)pOut.Value!;
            }
            catch { tx.Rollback(); throw; }
        }

        public static void UpdateEmployee(AppDbContext ctx, Employee e)
        {
            using var cn = new NpgsqlConnection(ctx.СonnectionString);
            cn.Open();
            using var tx = cn.BeginTransaction();
            try
            {
                using var cmd = cn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = "CALL sp_update_employee(@p_id, @p_full_name, @p_idnp, @p_phone, @p_address, @p_salary, @p_position)";
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
            using var cn = new NpgsqlConnection(ctx.СonnectionString);
            cn.Open();
            using var tx = cn.BeginTransaction();
            try
            {
                using var cmd = cn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = "CALL sp_delete_employee(@p_id)";
                cmd.Parameters.AddWithValue("p_id", id);
                cmd.ExecuteNonQuery();
                tx.Commit();
            }
            catch { tx.Rollback(); throw; }
        }

        // ══ RECEIPT ════════════════════════════════════════════════

        public static int AddReceipt(AppDbContext ctx, Receipt r)
        {
            using var cn = new NpgsqlConnection(ctx.СonnectionString);
            cn.Open();
            using var tx = cn.BeginTransaction();
            try
            {
                using var cmd = cn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = "CALL sp_add_receipt(@p_receipt_number, @p_pharmacy_id, @p_employee_id, @p_date, @p_time, @p_id)";
                cmd.Parameters.AddWithValue("p_receipt_number", r.ReceiptNumber);
                cmd.Parameters.AddWithValue("p_pharmacy_id", r.PharmacyId);
                cmd.Parameters.AddWithValue("p_employee_id", r.EmployeeId);
                cmd.Parameters.Add(new NpgsqlParameter("p_date", NpgsqlDbType.Date) { Value = r.Date.Date });
                cmd.Parameters.AddWithValue("p_time", r.Time);
                var pOut = new NpgsqlParameter("p_id", NpgsqlDbType.Integer)
                { Direction = ParameterDirection.InputOutput, Value = 0 };
                cmd.Parameters.Add(pOut);
                cmd.ExecuteNonQuery();
                tx.Commit();
                return (int)pOut.Value!;
            }
            catch { tx.Rollback(); throw; }
        }

        public static void UpdateReceipt(AppDbContext ctx, Receipt r)
        {
            using var cn = new NpgsqlConnection(ctx.СonnectionString);
            cn.Open();
            using var tx = cn.BeginTransaction();
            try
            {
                using var cmd = cn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = "CALL sp_update_receipt(@p_id, @p_receipt_number, @p_pharmacy_id, @p_employee_id, @p_date, @p_time)";
                cmd.Parameters.AddWithValue("p_id", r.Id);
                cmd.Parameters.AddWithValue("p_receipt_number", r.ReceiptNumber);
                cmd.Parameters.AddWithValue("p_pharmacy_id", r.PharmacyId);
                cmd.Parameters.AddWithValue("p_employee_id", r.EmployeeId);
                cmd.Parameters.Add(new NpgsqlParameter("p_date", NpgsqlDbType.Date) { Value = r.Date.Date });
                cmd.Parameters.AddWithValue("p_time", r.Time);
                cmd.ExecuteNonQuery();
                tx.Commit();
            }
            catch { tx.Rollback(); throw; }
        }

        public static void DeleteReceipt(AppDbContext ctx, int id)
        {
            using var cn = new NpgsqlConnection(ctx.СonnectionString);
            cn.Open();
            using var tx = cn.BeginTransaction();
            try
            {
                using var cmd = cn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = "CALL sp_delete_receipt(@p_id)";
                cmd.Parameters.AddWithValue("p_id", id);
                cmd.ExecuteNonQuery();
                tx.Commit();
            }
            catch { tx.Rollback(); throw; }
        }

        // ══ ORDER ITEM ═════════════════════════════════════════════

        public static void AddOrderItem(AppDbContext ctx, OrderItem o)
        {
            using var cn = new NpgsqlConnection(ctx.СonnectionString);
            cn.Open();
            using var tx = cn.BeginTransaction();
            try
            {
                using var cmd = cn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = "CALL sp_add_order_item(@p_receipt_id, @p_product_id, @p_quantity, @p_discount)";
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
            using var cn = new NpgsqlConnection(ctx.СonnectionString);
            cn.Open();
            using var tx = cn.BeginTransaction();
            try
            {
                using var cmd = cn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = "CALL sp_update_order_item(@p_receipt_id, @p_product_id, @p_new_receipt_id, @p_new_product_id, @p_quantity, @p_discount)";
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
            using var cn = new NpgsqlConnection(ctx.СonnectionString);
            cn.Open();
            using var tx = cn.BeginTransaction();
            try
            {
                using var cmd = cn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = "CALL sp_delete_order_item(@p_receipt_id, @p_product_id)";
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
            using var cn = new NpgsqlConnection(ctx.СonnectionString);
            cn.Open();
            using var tx = cn.BeginTransaction();
            try
            {
                using var cmd = cn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = "CALL sp_add_stock_balance(@p_pharmacy_id, @p_product_id, @p_remaining_qty)";
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
            using var cn = new NpgsqlConnection(ctx.СonnectionString);
            cn.Open();
            using var tx = cn.BeginTransaction();
            try
            {
                using var cmd = cn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = "CALL sp_update_stock_balance(@p_pharmacy_id, @p_product_id, @p_new_pharmacy_id, @p_new_product_id, @p_remaining_qty)";
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
            using var cn = new NpgsqlConnection(ctx.СonnectionString);
            cn.Open();
            using var tx = cn.BeginTransaction();
            try
            {
                using var cmd = cn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = "CALL sp_delete_stock_balance(@p_pharmacy_id, @p_product_id)";
                cmd.Parameters.AddWithValue("p_pharmacy_id", pharmacyId);
                cmd.Parameters.AddWithValue("p_product_id", productId);
                cmd.ExecuteNonQuery();
                tx.Commit();
            }
            catch { tx.Rollback(); throw; }
        }
    }
}