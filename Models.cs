using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyHippocrates.Models
{
    [Table("manufacturer")]
    public class Manufacturer
    {
        [Key]
        [Column("manufacturer_id")] public int Id { get; set; }
        [Column("name")] public string Name { get; set; } = "";
        [Column("country")] public string Country { get; set; } = "";
        [Column("address")] public string Address { get; set; } = "";
        [Column("phone")] public string Phone { get; set; } = "";
        [Column("email")] public string Email { get; set; } = "";

        public ICollection<Product> Products { get; set; } = new List<Product>();
    }

    [Table("product")]
    public class Product
    {
        [Key]
        [Column("product_id")] public int Id { get; set; }
        [Column("name")] public string Name { get; set; } = "";
        [Column("manufacturer_id")] public int ManufacturerId { get; set; }
        [Column("expiration_date")] public DateTime ExpirationDate { get; set; }
        [Column("production_date")] public DateTime ProductionDate { get; set; }
        [Column("unit")] public string Unit { get; set; } = "шт";
        [Column("description")] public string? Description { get; set; }
        [Column("prescription_required")] public bool PrescriptionRequired { get; set; }
        [Column("purchase_price")] public decimal PurchasePrice { get; set; }
        [Column("sale_price")] public decimal SalePrice { get; set; }

        public Manufacturer? Manufacturer { get; set; }
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public ICollection<StockBalance> StockBalances { get; set; } = new List<StockBalance>();
    }

    [Table("pharmacy")]
    public class Pharmacy
    {
        [Key]
        [Column("pharmacy_id")] public int Id { get; set; }
        [Column("address")] public string Address { get; set; } = "";
        [Column("phone")] public string Phone { get; set; } = "";
        [Column("working_hours")] public string WorkingHours { get; set; } = "";

        public ICollection<Receipt> Receipts { get; set; } = new List<Receipt>();
        public ICollection<StockBalance> StockBalances { get; set; } = new List<StockBalance>();
    }

    [Table("employee")]
    public class Employee
    {
        [Key]
        [Column("employee_id")] public int Id { get; set; }
        [Column("full_name")] public string FullName { get; set; } = "";
        [Column("idnp")] public string Idnp { get; set; } = "";
        [Column("phone")] public string Phone { get; set; } = "";
        [Column("address")] public string Address { get; set; } = "";
        [Column("salary")] public decimal Salary { get; set; }
        [Column("position")] public string Position { get; set; } = "";

        public ICollection<Receipt> Receipts { get; set; } = new List<Receipt>();
    }

    [Table("receipt")]
    public class Receipt
    {
        [Key]
        [Column("receipt_id")] public int Id { get; set; }
        [Column("receipt_number")] public int ReceiptNumber { get; set; }
        [Column("pharmacy_id")] public int PharmacyId { get; set; }
        [Column("employee_id")] public int EmployeeId { get; set; }
        [Column("total_amount")] public decimal TotalAmount { get; set; }
        [Column("date")] public DateTime Date { get; set; } = DateTime.Today;
        [Column("time")] public TimeSpan Time { get; set; } = DateTime.Now.TimeOfDay;

        public Pharmacy? Pharmacy { get; set; }
        public Employee? Employee { get; set; }
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }

    [Table("order_item")]
    public class OrderItem
    {
        [Column("receipt_id")] public int ReceiptId { get; set; }
        [Column("product_id")] public int ProductId { get; set; }
        [Column("quantity")] public int Quantity { get; set; }
        [Column("unit_price")] public decimal UnitPrice { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        [Column("total_price")] public decimal TotalPrice { get; set; }

        [Column("discount")] public decimal Discount { get; set; }

        public Receipt? Receipt { get; set; }
        public Product? Product { get; set; }

    }

    [Table("stock_balance")]
    public class StockBalance
    {
        [Column("pharmacy_id")] public int PharmacyId { get; set; }
        [Column("product_id")] public int ProductId { get; set; }
        [Column("remaining_qty")] public int RemainingQty { get; set; }

        public Pharmacy? Pharmacy { get; set; }
        public Product? Product { get; set; }
    }
}