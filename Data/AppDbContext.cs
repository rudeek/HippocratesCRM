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
        public readonly string ConnectionString;

        public AppDbContext(string connectionString)
        {
            this.ConnectionString = connectionString;
        }

        public DbSet<Manufacturer> Manufacturers { get; set; } = null!;
        public DbSet<Product> Products { get; set; } = null!;
        public DbSet<Pharmacy> Pharmacies { get; set; } = null!;
        public DbSet<Employee> Employees { get; set; } = null!;
        public DbSet<Receipt> Receipts { get; set; } = null!;
        public DbSet<OrderItem> OrderItems { get; set; } = null!;
        public DbSet<StockBalance> StockBalances { get; set; } = null!;
        public DbSet<Category> Categories { get; set; } = null!;
        public DbSet<Role> Roles { get; set; } = null!;
        public DbSet<SystemUser> SystemUsers { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql(ConnectionString);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OrderItem>().HasKey(o => new { o.ReceiptId, o.ProductId });
            modelBuilder.Entity<StockBalance>().HasKey(s => new {s.PharmacyId, s.ProductId});
            modelBuilder.Entity<Employee>()
                .HasOne(e => e.Role)
                .WithMany(r => r.Employees)
                .HasForeignKey(e => e.RoleId);

            modelBuilder.Entity<Employee>()          // ← добавить
                .HasOne(e => e.Pharmacy)
                .WithMany(p => p.Employees)
                .HasForeignKey(e => e.PharmacyId);

            modelBuilder.Entity<Employee>()
                .HasOne(e => e.SystemUser)
                .WithOne(u => u.Employee)
                .HasForeignKey<SystemUser>(u => u.EmployeeId);
        }
    }
}
