using Finance.Domain.Budgets;
using Finance.Domain.Categories;
using Finance.Domain.Transactions;
using Finance.Domain.Users;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Finance.Infrastructure
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Transaction> Transactions => Set<Transaction>();
        public DbSet<Budget> Budgets => Set<Budget>();

        protected override void OnModelCreating(ModelBuilder b)
        {
            b.Entity<User>().HasIndex(x => x.Email).IsUnique();
            b.Entity<Category>().HasIndex(x => new { x.UserId, x.Name }).IsUnique();
            b.Entity<Transaction>().HasIndex(x => new { x.UserId, x.OccurredOn });
            b.Entity<Budget>().HasIndex(x => new { x.UserId, x.CategoryId, x.Year, x.Month }).IsUnique();
        }
    }
}
