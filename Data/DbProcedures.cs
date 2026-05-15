using System;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using MyHippocrates.Models;
using Npgsql;
using NpgsqlTypes;

namespace MyHippocrates.Data
{
    public static class DbProcedures
    {

        private static string Md5Hash(string input)
        {
            using var md5 = MD5.Create();
            var bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
            return string.Concat(bytes.Select(b => b.ToString("x2")));
        }

        private static NpgsqlParameter CreateDateParam(string name, DateTime value) =>
            new(name, NpgsqlDbType.Date) { Value = value.Date };

        private static NpgsqlParameter CreateNullableParam(string name, object? value) =>
            new(name, value ?? DBNull.Value);

        // ══ MANUFACTURER ══════════════════════════════════════════

        public static int AddManufacturer(AppDbContext ctx, Manufacturer m)
        {
            using var cn = new NpgsqlConnection(ctx.ConnectionString);
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
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        public static void UpdateManufacturer(AppDbContext ctx, Manufacturer m)
        {
            using var cn = new NpgsqlConnection(ctx.ConnectionString);
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
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        public static void DeleteManufacturer(AppDbContext ctx, int id)
        {
            using var cn = new NpgsqlConnection(ctx.ConnectionString);
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
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        // ══ PRODUCT ════════════════════════════════════════════════
        // ⚠️ ИСПРАВЛЕНО: добавлен p_category_id, которого не хватало!

        public static int AddProduct(AppDbContext ctx, Product p)
        {
            using var cn = new NpgsqlConnection(ctx.ConnectionString);
            cn.Open();
            using var tx = cn.BeginTransaction();
            try
            {
                using var cmd = cn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = @"
                    CALL sp_add_product(
                        @p_name, @p_category_id, @p_manufacturer_id,
                        @p_production_date, @p_expiration_date,
                        @p_unit, @p_description, @p_prescription_required,
                        @p_purchase_price, @p_sale_price, @p_file_path, @p_id)";

                cmd.Parameters.AddWithValue("p_name", p.Name);
                cmd.Parameters.AddWithValue("p_category_id", p.CategoryId);
                cmd.Parameters.AddWithValue("p_manufacturer_id", p.ManufacturerId);
                cmd.Parameters.Add(CreateDateParam("p_production_date", p.ProductionDate));
                cmd.Parameters.Add(CreateDateParam("p_expiration_date", p.ExpirationDate));
                cmd.Parameters.AddWithValue("p_unit", p.Unit ?? "шт");
                cmd.Parameters.Add(CreateNullableParam("p_description", p.Description));
                cmd.Parameters.AddWithValue("p_prescription_required", p.PrescriptionRequired);
                cmd.Parameters.AddWithValue("p_purchase_price", p.PurchasePrice);
                cmd.Parameters.AddWithValue("p_sale_price", p.SalePrice);
                cmd.Parameters.Add(CreateNullableParam("p_file_path", p.FilePath));

                var pOut = new NpgsqlParameter("p_id", NpgsqlDbType.Integer)
                { Direction = ParameterDirection.InputOutput, Value = 0 };
                cmd.Parameters.Add(pOut);

                cmd.ExecuteNonQuery();
                tx.Commit();
                return (int)pOut.Value!;
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        public static void UpdateProduct(AppDbContext ctx, Product p)
        {
            using var cn = new NpgsqlConnection(ctx.ConnectionString);
            cn.Open();
            using var tx = cn.BeginTransaction();
            try
            {
                using var cmd = cn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = @"
                    CALL sp_update_product(
                        @p_id, @p_name, @p_category_id, @p_manufacturer_id,
                        @p_production_date, @p_expiration_date,
                        @p_unit, @p_description, @p_prescription_required,
                        @p_purchase_price, @p_sale_price, @p_file_path)";

                cmd.Parameters.AddWithValue("p_id", p.Id);
                cmd.Parameters.AddWithValue("p_name", p.Name);
                cmd.Parameters.AddWithValue("p_category_id", p.CategoryId);
                cmd.Parameters.AddWithValue("p_manufacturer_id", p.ManufacturerId);
                cmd.Parameters.Add(CreateDateParam("p_production_date", p.ProductionDate));
                cmd.Parameters.Add(CreateDateParam("p_expiration_date", p.ExpirationDate));
                cmd.Parameters.AddWithValue("p_unit", p.Unit ?? "шт");
                cmd.Parameters.Add(CreateNullableParam("p_description", p.Description));
                cmd.Parameters.AddWithValue("p_prescription_required", p.PrescriptionRequired);
                cmd.Parameters.AddWithValue("p_purchase_price", p.PurchasePrice);
                cmd.Parameters.AddWithValue("p_sale_price", p.SalePrice);
                cmd.Parameters.Add(CreateNullableParam("p_file_path", p.FilePath));

                cmd.ExecuteNonQuery();
                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        public static void DeleteProduct(AppDbContext ctx, int id)
        {
            using var cn = new NpgsqlConnection(ctx.ConnectionString);
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
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        // ══ PHARMACY ═══════════════════════════════════════════════

        public static int AddPharmacy(AppDbContext ctx, Pharmacy p)
        {
            using var cn = new NpgsqlConnection(ctx.ConnectionString);
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
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        public static void UpdatePharmacy(AppDbContext ctx, Pharmacy p)
        {
            using var cn = new NpgsqlConnection(ctx.ConnectionString);
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
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        public static void DeletePharmacy(AppDbContext ctx, int id)
        {
            using var cn = new NpgsqlConnection(ctx.ConnectionString);
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
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        // ══ EMPLOYEE ═══════════════════════════════════════════════

        public static int AddEmployee(AppDbContext ctx, Employee e)
        {
            using var cn = new NpgsqlConnection(ctx.ConnectionString);
            cn.Open();
            using var tx = cn.BeginTransaction();
            try
            {
                using var cmd = cn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = "CALL sp_add_employee(@p_full_name, @p_idnp, @p_phone, @p_address, @p_role_id, @p_id)";

                cmd.Parameters.AddWithValue("p_full_name", e.FullName);
                cmd.Parameters.AddWithValue("p_idnp", e.Idnp);
                cmd.Parameters.AddWithValue("p_phone", e.Phone);
                cmd.Parameters.Add(CreateNullableParam("p_address", e.Address));
                cmd.Parameters.AddWithValue("p_role_id", e.RoleId);

                var pOut = new NpgsqlParameter("p_id", NpgsqlDbType.Integer)
                { Direction = ParameterDirection.InputOutput, Value = 0 };
                cmd.Parameters.Add(pOut);

                cmd.ExecuteNonQuery();
                tx.Commit();
                return (int)pOut.Value!;
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        public static void UpdateEmployee(AppDbContext ctx, Employee e)
        {
            using var cn = new NpgsqlConnection(ctx.ConnectionString);
            cn.Open();
            using var tx = cn.BeginTransaction();
            try
            {
                using var cmd = cn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = "CALL sp_update_employee(@p_id, @p_full_name, @p_idnp, @p_phone, @p_address, @p_role_id)";

                cmd.Parameters.AddWithValue("p_id", e.Id);
                cmd.Parameters.AddWithValue("p_full_name", e.FullName);
                cmd.Parameters.AddWithValue("p_idnp", e.Idnp);
                cmd.Parameters.AddWithValue("p_phone", e.Phone);
                cmd.Parameters.Add(CreateNullableParam("p_address", e.Address));
                cmd.Parameters.AddWithValue("p_role_id", e.RoleId);

                cmd.ExecuteNonQuery();
                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        public static void DeleteEmployee(AppDbContext ctx, int id)
        {
            using var cn = new NpgsqlConnection(ctx.ConnectionString);
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
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        // ══ RECEIPT ════════════════════════════════════════════════

        public static int AddReceipt(AppDbContext ctx, Receipt r)
        {
            using var cn = new NpgsqlConnection(ctx.ConnectionString);
            cn.Open();
            using var tx = cn.BeginTransaction();
            try
            {
                using var cmd = cn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = "CALL sp_add_receipt(@p_pharmacy_id, @p_employee_id, @p_date, @p_id)";

                cmd.Parameters.AddWithValue("p_pharmacy_id", r.PharmacyId);
                cmd.Parameters.AddWithValue("p_employee_id", r.EmployeeId);
                cmd.Parameters.Add(CreateDateParam("p_date", r.Date));

                var pOut = new NpgsqlParameter("p_id", NpgsqlDbType.Integer)
                { Direction = ParameterDirection.InputOutput, Value = 0 };
                cmd.Parameters.Add(pOut);

                cmd.ExecuteNonQuery();
                tx.Commit();
                return (int)pOut.Value!;
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        public static void UpdateReceipt(AppDbContext ctx, Receipt r)
        {
            using var cn = new NpgsqlConnection(ctx.ConnectionString);
            cn.Open();
            using var tx = cn.BeginTransaction();
            try
            {
                using var cmd = cn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = "CALL sp_update_receipt(@p_id, @p_pharmacy_id, @p_employee_id, @p_date)";

                cmd.Parameters.AddWithValue("p_id", r.Id);
                cmd.Parameters.AddWithValue("p_pharmacy_id", r.PharmacyId);
                cmd.Parameters.AddWithValue("p_employee_id", r.EmployeeId);
                cmd.Parameters.Add(CreateDateParam("p_date", r.Date));

                cmd.ExecuteNonQuery();
                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        public static void SetReceiptTotal(AppDbContext ctx, int receiptId, decimal totalAmount)
        {
            using var cn = new NpgsqlConnection(ctx.ConnectionString);
            cn.Open();
            using var tx = cn.BeginTransaction();
            try
            {
                using var cmd = cn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = @"
                    UPDATE receipt
                    SET total_amount = @p_total_amount
                    WHERE receipt_id = @p_receipt_id";

                cmd.Parameters.AddWithValue("p_receipt_id", receiptId);
                cmd.Parameters.AddWithValue("p_total_amount", totalAmount);

                cmd.ExecuteNonQuery();
                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        public static void ApplyReceiptDiscount(AppDbContext ctx, int receiptId, decimal discount)
        {
            using var cn = new NpgsqlConnection(ctx.ConnectionString);
            cn.Open();
            using var tx = cn.BeginTransaction();
            try
            {
                using (var updateItems = cn.CreateCommand())
                {
                    updateItems.Transaction = tx;
                    updateItems.CommandText = @"
                        UPDATE order_item
                        SET discount = @p_discount
                        WHERE receipt_id = @p_receipt_id";
                    updateItems.Parameters.AddWithValue("p_receipt_id", receiptId);
                    updateItems.Parameters.AddWithValue("p_discount", discount);
                    updateItems.ExecuteNonQuery();
                }

                using (var updateReceipt = cn.CreateCommand())
                {
                    updateReceipt.Transaction = tx;
                    updateReceipt.CommandText = @"
                        UPDATE receipt
                        SET total_amount = (
                            SELECT COALESCE(SUM(total_price), 0)
                            FROM order_item
                            WHERE receipt_id = @p_receipt_id
                        )
                        WHERE receipt_id = @p_receipt_id";
                    updateReceipt.Parameters.AddWithValue("p_receipt_id", receiptId);
                    updateReceipt.ExecuteNonQuery();
                }

                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        public static void DeleteReceipt(AppDbContext ctx, int id)
        {
            using var cn = new NpgsqlConnection(ctx.ConnectionString);
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
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        public static void CancelReceipt(AppDbContext ctx, int receiptId)
        {
            using var cn = new NpgsqlConnection(ctx.ConnectionString);
            cn.Open();
            using var tx = cn.BeginTransaction();
            try
            {
                using (var restore = cn.CreateCommand())
                {
                    restore.Transaction = tx;
                    restore.CommandText = @"
                        UPDATE stock_balance sb
                        SET remaining_qty = sb.remaining_qty + oi.quantity
                        FROM order_item oi
                        JOIN receipt r ON r.receipt_id = oi.receipt_id
                        WHERE oi.receipt_id = @p_receipt_id
                          AND sb.pharmacy_id = r.pharmacy_id
                          AND sb.product_id = oi.product_id";
                    restore.Parameters.AddWithValue("p_receipt_id", receiptId);
                    restore.ExecuteNonQuery();
                }

                using (var delete = cn.CreateCommand())
                {
                    delete.Transaction = tx;
                    delete.CommandText = "DELETE FROM receipt WHERE receipt_id = @p_receipt_id";
                    delete.Parameters.AddWithValue("p_receipt_id", receiptId);
                    delete.ExecuteNonQuery();
                }

                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        // ══ ORDER ITEM ═════════════════════════════════════════════

        public static void AddOrderItem(AppDbContext ctx, OrderItem o)
        {
            using var cn = new NpgsqlConnection(ctx.ConnectionString);
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
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        public static void UpdateOrderItem(AppDbContext ctx,
            int oldReceiptId, int oldProductId, OrderItem o)
        {
            using var cn = new NpgsqlConnection(ctx.ConnectionString);
            cn.Open();
            using var tx = cn.BeginTransaction();
            try
            {
                using var cmd = cn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = @"
                    CALL sp_update_order_item(
                        @p_receipt_id, @p_product_id,
                        @p_new_receipt_id, @p_new_product_id,
                        @p_quantity, @p_discount)";

                cmd.Parameters.AddWithValue("p_receipt_id", oldReceiptId);
                cmd.Parameters.AddWithValue("p_product_id", oldProductId);
                cmd.Parameters.AddWithValue("p_new_receipt_id", o.ReceiptId);
                cmd.Parameters.AddWithValue("p_new_product_id", o.ProductId);
                cmd.Parameters.AddWithValue("p_quantity", o.Quantity);
                cmd.Parameters.AddWithValue("p_discount", o.Discount);

                cmd.ExecuteNonQuery();
                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        public static void DeleteOrderItem(AppDbContext ctx, int receiptId, int productId)
        {
            using var cn = new NpgsqlConnection(ctx.ConnectionString);
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
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        // ══ STOCK BALANCE ══════════════════════════════════════════

        public static void AddStockBalance(AppDbContext ctx, StockBalance s)
        {
            using var cn = new NpgsqlConnection(ctx.ConnectionString);
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
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        public static void UpdateStockBalance(AppDbContext ctx,
            int oldPharmacyId, int oldProductId, StockBalance s)
        {
            using var cn = new NpgsqlConnection(ctx.ConnectionString);
            cn.Open();
            using var tx = cn.BeginTransaction();
            try
            {
                using var cmd = cn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = @"
                    CALL sp_update_stock_balance(
                        @p_pharmacy_id, @p_product_id,
                        @p_new_pharmacy_id, @p_new_product_id,
                        @p_remaining_qty)";

                cmd.Parameters.AddWithValue("p_pharmacy_id", oldPharmacyId);
                cmd.Parameters.AddWithValue("p_product_id", oldProductId);
                cmd.Parameters.AddWithValue("p_new_pharmacy_id", s.PharmacyId);
                cmd.Parameters.AddWithValue("p_new_product_id", s.ProductId);
                cmd.Parameters.AddWithValue("p_remaining_qty", s.RemainingQty);

                cmd.ExecuteNonQuery();
                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        public static void DeleteStockBalance(AppDbContext ctx, int pharmacyId, int productId)
        {
            using var cn = new NpgsqlConnection(ctx.ConnectionString);
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
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        // ══ CATEGORY ══════════════════════════════════════════════

        public static int AddCategory(AppDbContext ctx, Category c)
        {
            using var cn = new NpgsqlConnection(ctx.ConnectionString);
            cn.Open();
            using var tx = cn.BeginTransaction();
            try
            {
                using var cmd = cn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = "CALL sp_add_category(@p_name, @p_description, @p_id)";

                cmd.Parameters.AddWithValue("p_name", c.Name);
                cmd.Parameters.Add(CreateNullableParam("p_description", c.Description));

                var pOut = new NpgsqlParameter("p_id", NpgsqlDbType.Integer)
                { Direction = ParameterDirection.InputOutput, Value = 0 };
                cmd.Parameters.Add(pOut);

                cmd.ExecuteNonQuery();
                tx.Commit();
                return (int)pOut.Value!;
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        public static void UpdateCategory(AppDbContext ctx, Category c)
        {
            using var cn = new NpgsqlConnection(ctx.ConnectionString);
            cn.Open();
            using var tx = cn.BeginTransaction();
            try
            {
                using var cmd = cn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = "CALL sp_update_category(@p_id, @p_name, @p_description)";

                cmd.Parameters.AddWithValue("p_id", c.Id);
                cmd.Parameters.AddWithValue("p_name", c.Name);
                cmd.Parameters.Add(CreateNullableParam("p_description", c.Description));

                cmd.ExecuteNonQuery();
                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        public static void DeleteCategory(AppDbContext ctx, int id)
        {
            using var cn = new NpgsqlConnection(ctx.ConnectionString);
            cn.Open();
            using var tx = cn.BeginTransaction();
            try
            {
                using var cmd = cn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = "CALL sp_delete_category(@p_id)";
                cmd.Parameters.AddWithValue("p_id", id);
                cmd.ExecuteNonQuery();
                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        // ══ ROLE ══════════════════════════════════════════════════

        public static int AddRole(AppDbContext ctx, Role r)
        {
            using var cn = new NpgsqlConnection(ctx.ConnectionString);
            cn.Open();
            using var tx = cn.BeginTransaction();
            try
            {
                using var cmd = cn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = "CALL sp_add_role(@p_name, @p_fixed_salary, @p_description, @p_id)";

                cmd.Parameters.AddWithValue("p_name", r.Name);
                cmd.Parameters.AddWithValue("p_fixed_salary", r.FixedSalary);
                cmd.Parameters.Add(CreateNullableParam("p_description", r.Description));

                var pOut = new NpgsqlParameter("p_id", NpgsqlDbType.Integer)
                { Direction = ParameterDirection.InputOutput, Value = 0 };
                cmd.Parameters.Add(pOut);

                cmd.ExecuteNonQuery();
                tx.Commit();
                return (int)pOut.Value!;
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        public static void UpdateRole(AppDbContext ctx, Role r)
        {
            using var cn = new NpgsqlConnection(ctx.ConnectionString);
            cn.Open();
            using var tx = cn.BeginTransaction();
            try
            {
                using var cmd = cn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = "CALL sp_update_role(@p_id, @p_name, @p_fixed_salary, @p_description)";

                cmd.Parameters.AddWithValue("p_id", r.Id);
                cmd.Parameters.AddWithValue("p_name", r.Name);
                cmd.Parameters.AddWithValue("p_fixed_salary", r.FixedSalary);
                cmd.Parameters.Add(CreateNullableParam("p_description", r.Description));

                cmd.ExecuteNonQuery();
                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        public static void DeleteRole(AppDbContext ctx, int id)
        {
            using var cn = new NpgsqlConnection(ctx.ConnectionString);
            cn.Open();
            using var tx = cn.BeginTransaction();
            try
            {
                using var cmd = cn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = "CALL sp_delete_role(@p_id)";
                cmd.Parameters.AddWithValue("p_id", id);
                cmd.ExecuteNonQuery();
                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        // ══ SYSTEM USERS ══════════════════════════════════════════
        // ✅ ИСПРАВЛЕНО: system_user → system_users, используем хранимые процедуры

        public static int AddSystemUser(AppDbContext ctx, SystemUser u, string plainPassword)
        {
            using var cn = new NpgsqlConnection(ctx.ConnectionString);
            cn.Open();
            using var tx = cn.BeginTransaction();
            try
            {
                using var cmd = cn.CreateCommand();
                cmd.Transaction = tx;
                // ✅ Используем хранимую процедуру sp_add_system_users
                cmd.CommandText = @"
                    CALL sp_add_system_users(
                        @p_employee_id, @p_login, @p_password, @p_is_active, @p_id)";

                cmd.Parameters.AddWithValue("p_employee_id", u.EmployeeId);
                cmd.Parameters.AddWithValue("p_login", u.Login);
                cmd.Parameters.AddWithValue("p_password", plainPassword); // хешируется внутри процедуры
                cmd.Parameters.AddWithValue("p_is_active", u.IsActive);

                var pOut = new NpgsqlParameter("p_id", NpgsqlDbType.Integer)
                { Direction = ParameterDirection.InputOutput, Value = 0 };
                cmd.Parameters.Add(pOut);

                cmd.ExecuteNonQuery();
                tx.Commit();
                return (int)pOut.Value!;
            }
            catch (NpgsqlException ex) when (ex.Message.Contains("уже есть системный аккаунт"))
            {
                tx.Rollback();
                throw new InvalidOperationException("У этого сотрудника уже есть аккаунт в системе.", ex);
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        public static void UpdateSystemUser(AppDbContext ctx, SystemUser u, string plainPassword)
        {
            using var cn = new NpgsqlConnection(ctx.ConnectionString);
            cn.Open();
            using var tx = cn.BeginTransaction();
            try
            {
                using var cmd = cn.CreateCommand();
                cmd.Transaction = tx;
                // ✅ Используем хранимую процедуру sp_update_system_users
                // plainPassword = "" → пароль не меняется
                cmd.CommandText = @"
                    CALL sp_update_system_users(
                        @p_id, @p_employee_id, @p_login, @p_password, @p_is_active)";

                cmd.Parameters.AddWithValue("p_id", u.Id);
                cmd.Parameters.AddWithValue("p_employee_id", u.EmployeeId);
                cmd.Parameters.AddWithValue("p_login", u.Login);
                cmd.Parameters.AddWithValue("p_password", plainPassword ?? string.Empty);
                cmd.Parameters.AddWithValue("p_is_active", u.IsActive);

                cmd.ExecuteNonQuery();
                tx.Commit();
            }
            catch (NpgsqlException ex) when (ex.Message.Contains("уже есть системный аккаунт"))
            {
                tx.Rollback();
                throw new InvalidOperationException("У этого сотрудника уже есть аккаунт в системе.", ex);
            }
            catch (NpgsqlException ex) when (ex.Message.Contains("не найден"))
            {
                tx.Rollback();
                throw new InvalidOperationException($"Пользователь с id={u.Id} не найден.", ex);
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        public static void DeleteSystemUser(AppDbContext ctx, int id)
        {
            using var cn = new NpgsqlConnection(ctx.ConnectionString);
            cn.Open();
            using var tx = cn.BeginTransaction();
            try
            {
                using var cmd = cn.CreateCommand();
                cmd.Transaction = tx;
                // ✅ Используем хранимую процедуру sp_delete_system_users
                cmd.CommandText = "CALL sp_delete_system_users(@p_id)";
                cmd.Parameters.AddWithValue("p_id", id);
                cmd.ExecuteNonQuery();
                tx.Commit();
            }
            catch (NpgsqlException ex) when (ex.Message.Contains("не найден"))
            {
                tx.Rollback();
                throw new InvalidOperationException($"Пользователь с id={id} не найден.", ex);
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        // ══ АВТОРИЗАЦИЯ ═══════════════════════════════════════════

        /// <summary>
        /// Проверяет логин/пароль и возвращает данные пользователя.
        /// Пароль сравнивается через md5 (как в БД).
        /// </summary>
        
    }
}
