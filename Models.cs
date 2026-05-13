using Microsoft.EntityFrameworkCore;
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
        [Column("manufacturer_id")]
        public int Id { get; set; }

        [Column("name"), Required, MaxLength(150)]
        public string Name { get; set; } = "";

        [Column("country"), Required, MaxLength(100)]
        public string Country { get; set; } = "";

        [Column("address"), Required, MaxLength(255)]
        public string Address { get; set; } = "";

        [Column("phone"), Required, MaxLength(20)]
        public string Phone { get; set; } = "";

        [Column("email"), Required, MaxLength(150), EmailAddress]
        public string Email { get; set; } = "";

        public ICollection<Product> Products { get; set; } = new List<Product>();
    }

    [Table("product")]
    public class Product
    {
        [Key]
        [Column("product_id")]
        public int Id { get; set; }

        [Column("name"), Required, MaxLength(150)]
        public string Name { get; set; } = "";

        [Column("category_id")]
        public int CategoryId { get; set; }

        [Column("manufacturer_id")]
        public int ManufacturerId { get; set; }

        [Column("expiration_date"), ColumnType("date")]
        public DateTime ExpirationDate { get; set; }

        [Column("production_date"), ColumnType("date")]
        public DateTime ProductionDate { get; set; }

        [Column("unit"), MaxLength(50)]
        public string Unit { get; set; } = "шт";

        [Column("description")]
        public string? Description { get; set; }

        [Column("prescription_required")]
        public bool PrescriptionRequired { get; set; }

        [Column("purchase_price"), ColumnType("numeric(10,2)")]
        public decimal PurchasePrice { get; set; }

        [Column("sale_price"), ColumnType("numeric(10,2)")]
        public decimal SalePrice { get; set; }

        public Category? Category { get; set; } 
        public Manufacturer? Manufacturer { get; set; }
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public ICollection<StockBalance> StockBalances { get; set; } = new List<StockBalance>();
    }

    [Table("pharmacy")]
    public class Pharmacy
    {
        [Key]
        [Column("pharmacy_id")]
        public int Id { get; set; }

        [Column("address"), Required, MaxLength(255)]
        public string Address { get; set; } = "";

        [Column("phone"), Required, MaxLength(20)]
        public string Phone { get; set; } = "";

        [Column("working_hours"), Required, MaxLength(100)]
        public string WorkingHours { get; set; } = "";

        public ICollection<Receipt> Receipts { get; set; } = new List<Receipt>();
        public ICollection<StockBalance> StockBalances { get; set; } = new List<StockBalance>();
    }

    [Table("employee")]
    public class Employee
    {
        [Key]
        [Column("employee_id")]
        public int Id { get; set; }

        [Column("full_name"), Required, MaxLength(150)]
        public string FullName { get; set; } = "";

        [Column("idnp"), Required, MaxLength(13), Unique]
        public string Idnp { get; set; } = "";

        [Column("phone"), Required, MaxLength(20)]
        public string Phone { get; set; } = "";

        [Column("address"), Required, MaxLength(255)]
        public string Address { get; set; } = "";

        [Column("role_id")]
        public int RoleId { get; set; }

        public Role? Role { get; set; }
        public SystemUser? SystemUser { get; set; }
        public ICollection<Receipt> Receipts { get; set; } = new List<Receipt>();

        /// <summary>
        /// Вычисляемое свойство для отображения зарплаты из роли.
        /// </summary>
        [NotMapped]
        public decimal Salary => Role?.FixedSalary ?? 0;
    }

    [Table("receipt")]
    public class Receipt
    {
        [Key]
        [Column("receipt_id")]
        public int Id { get; set; }

        [Column("receipt_number")]
        public int ReceiptNumber { get; set; }

        [Column("pharmacy_id")]
        public int PharmacyId { get; set; }

        [Column("employee_id")]
        public int EmployeeId { get; set; }

        [Column("total_amount"), ColumnType("numeric(10,2)")]
        public decimal TotalAmount { get; set; }

        [Column("date"), ColumnType("date")]
        public DateTime Date { get; set; } = DateTime.UtcNow;

        // ✅ ИСПРАВЛЕНО: время может быть null в БД (дефолт CURRENT_TIME)
        [Column("time"), ColumnType("time")]
        public TimeSpan? Time { get; set; }

        // Вычисляемое свойство для отображения
        [NotMapped]
        public string DisplayId => $"{Date:dd.MM.yyyy}-{ReceiptNumber:D3}";

        public Pharmacy? Pharmacy { get; set; }
        public Employee? Employee { get; set; }
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }

    // ═══════════════════════════════════════════════════════════
    // ORDER ITEM — ИСПРАВЛЕНО: составной первичный ключ
    // ═══════════════════════════════════════════════════════════
    [Table("order_item")]
    [PrimaryKey(nameof(ReceiptId), nameof(ProductId))] // ✅ ДОБАВЛЕНО
    public class OrderItem
    {
        [Column("receipt_id")]
        public int ReceiptId { get; set; }

        [Column("product_id")]
        public int ProductId { get; set; }

        [Column("quantity")]
        public int Quantity { get; set; }

        [Column("unit_price"), ColumnType("numeric(10,2)")]
        public decimal UnitPrice { get; set; }

        [Column("total_price"), ColumnType("numeric(10,2)")]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public decimal TotalPrice { get; set; }

        [Column("discount"), ColumnType("numeric(5,2)")]
        public decimal Discount { get; set; }

        public Receipt? Receipt { get; set; }
        public Product? Product { get; set; }
    }

    // ═══════════════════════════════════════════════════════════
    // STOCK BALANCE — ИСПРАВЛЕНО: составной первичный ключ
    // ═══════════════════════════════════════════════════════════
    [Table("stock_balance")]
    [PrimaryKey(nameof(PharmacyId), nameof(ProductId))] // ✅ ДОБАВЛЕНО
    public class StockBalance
    {
        [Column("pharmacy_id")]
        public int PharmacyId { get; set; }

        [Column("product_id")]
        public int ProductId { get; set; }

        [Column("remaining_qty")]
        public int RemainingQty { get; set; }

        public Pharmacy? Pharmacy { get; set; }
        public Product? Product { get; set; }
    }

    [Table("category")]
    public class Category
    {
        [Key]
        [Column("category_id")]
        public int Id { get; set; }

        [Column("name"), Required, MaxLength(100)]
        public string Name { get; set; } = "";

        [Column("description")]
        public string? Description { get; set; }

        public ICollection<Product> Products { get; set; } = new List<Product>();
    }

    [Table("role")]
    public class Role
    {
        [Key]
        [Column("role_id")]
        public int Id { get; set; }

        [Column("name"), Required, MaxLength(100)]
        public string Name { get; set; } = "";

        [Column("fixed_salary"), ColumnType("numeric(10,2)")]
        public decimal FixedSalary { get; set; }

        [Column("description")]
        public string? Description { get; set; }

        public ICollection<Employee> Employees { get; set; } = new List<Employee>();
    }

    // ═══════════════════════════════════════════════════════════
    // SYSTEM USER — ИСПРАВЛЕНО: имя таблицы system_users
    // ═══════════════════════════════════════════════════════════
    [Table("system_users")] 
    public class SystemUser
    {
        [Key]
        [Column("user_id")]
        public int Id { get; set; }

        [Column("employee_id"), Unique] // ✅ UNIQUE в БД
        public int EmployeeId { get; set; }

        [Column("login"), Required, MaxLength(100)]
        public string Login { get; set; } = "";

        [Column("password_hash"), Required, MaxLength(255)]
        public string PasswordHash { get; set; } = "";

        [Column("system_role"), MaxLength(10)]
        [RegularExpression("^(admin|user)$", ErrorMessage = "Роль должна быть 'admin' или 'user'")]
        public string SystemRole { get; set; } = "user";

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        public Employee? Employee { get; set; }
    }

    // ═══════════════════════════════════════════════════════════
    // ВСПОМОГАТЕЛЬНЫЕ АТРИБУТЫ (если нет в вашем проекте)
    // ═══════════════════════════════════════════════════════════

    // Атрибут для указания типа колонки в PostgreSQL
    [AttributeUsage(AttributeTargets.Property)]
    public class ColumnTypeAttribute : Attribute
    {
        public string TypeName { get; }
        public ColumnTypeAttribute(string typeName) => TypeName = typeName;
    }

    // Атрибут для уникальности (для документации/валидации)
    [AttributeUsage(AttributeTargets.Property)]
    public class UniqueAttribute : ValidationAttribute
    {
        public override bool IsValid(object? value) => true; // Проверка на уровне БД
    }
}