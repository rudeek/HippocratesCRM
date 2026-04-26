using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MyHippocrates.Models;

namespace MyHippocrates.Data
{
    public class AppDbContext : DbContext
    {
        public readonly string connectionString;

        public AppDbContext(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public DbSet<Manufacturer> Manufacturers { get; set; } = null!;
        public DbSet<Product> Products { get; set; } = null!;
        public DbSet<Pharmacy> Pharmacies { get; set; } = null!;
        public DbSet<Employee> Employees { get; set; } = null!;
        public DbSet<Receipt> Receipts { get; set; } = null!;
        public DbSet<OrderItem> OrderItems { get; set; } = null!;
        public DbSet<StockBalance> StockBalances { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql(connectionString);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OrderItem>().HasKey(o => new { o.ReceiptId, o.ProductId });
            modelBuilder.Entity<StockBalance>().HasKey(s => new {s.PharmacyId, s.ProductId});
        }
    }
}
