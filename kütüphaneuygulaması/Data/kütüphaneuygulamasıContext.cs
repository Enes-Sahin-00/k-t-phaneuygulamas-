using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using kütüphaneuygulaması.Models;

namespace kütüphaneuygulaması.Data
{
    public class kütüphaneuygulamasıContext : DbContext
    {
        public kütüphaneuygulamasıContext (DbContextOptions<kütüphaneuygulamasıContext> options)
            : base(options)
        {
        }

        public DbSet<kütüphaneuygulaması.Models.Book> Book { get; set; } = default!;
        public DbSet<kütüphaneuygulaması.Models.usersaccounts> usersaccounts { get; set; } = default!;
        public DbSet<kütüphaneuygulaması.Models.orders> orders { get; set; } = default!;
        public DbSet<kütüphaneuygulaması.Models.report> report { get; set; } = default!;
        public DbSet<kütüphaneuygulaması.Models.Cart> Cart { get; set; } = default!;
        public DbSet<kütüphaneuygulaması.Models.Category> Category { get; set; } = default!;
        public DbSet<kütüphaneuygulaması.Models.Favorite> Favorite { get; set; } = default!;
        public DbSet<kütüphaneuygulaması.Services.RefreshToken> RefreshTokens { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Book - Category relationship
            modelBuilder.Entity<Book>()
                .HasOne(b => b.Category)
                .WithMany(c => c.Books)
                .HasForeignKey(b => b.cataid)
                .OnDelete(DeleteBehavior.Restrict);

            // Cart relationships
            modelBuilder.Entity<Cart>()
                .HasOne(c => c.Book)
                .WithMany(b => b.Carts)
                .HasForeignKey(c => c.BookId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired(false);

            modelBuilder.Entity<Cart>()
                .HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired(false);

            // Favorite relationships
            modelBuilder.Entity<Favorite>()
                .HasOne(f => f.User)
                .WithMany()
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired(false);

            modelBuilder.Entity<Favorite>()
                .HasOne(f => f.Book)
                .WithMany(b => b.Favorites)
                .HasForeignKey(f => f.BookId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired(false);

            // Orders relationships
            modelBuilder.Entity<orders>()
                .HasOne(o => o.Book)
                .WithMany(b => b.Orders)
                .HasForeignKey(o => o.bookId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            modelBuilder.Entity<orders>()
                .HasOne(o => o.User)
                .WithMany()
                .HasForeignKey(o => o.userid)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            // RefreshToken relationship
            modelBuilder.Entity<kütüphaneuygulaması.Services.RefreshToken>()
                .HasOne(rt => rt.User)
                .WithMany()
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Audit fields configuration
            modelBuilder.Entity<Book>()
                .Property(b => b.CreatedDate)
                .HasDefaultValueSql("GETDATE()");

            modelBuilder.Entity<Category>()
                .Property(c => c.CreatedDate)
                .HasDefaultValueSql("GETDATE()");

            modelBuilder.Entity<Cart>()
                .Property(c => c.CreatedDate)
                .HasDefaultValueSql("GETDATE()");

            modelBuilder.Entity<Favorite>()
                .Property(f => f.CreatedDate)
                .HasDefaultValueSql("GETDATE()");

            modelBuilder.Entity<orders>()
                .Property(o => o.CreatedDate)
                .HasDefaultValueSql("GETDATE()");

            modelBuilder.Entity<report>()
                .Property(r => r.CreatedDate)
                .HasDefaultValueSql("GETDATE()");

            modelBuilder.Entity<usersaccounts>()
                .Property(u => u.CreatedDate)
                .HasDefaultValueSql("GETDATE()");

            modelBuilder.Entity<kütüphaneuygulaması.Services.RefreshToken>()
                .Property(rt => rt.CreatedAt)
                .HasDefaultValueSql("GETDATE()");

            // Indexes for better performance
            modelBuilder.Entity<Book>()
                .HasIndex(b => b.title);

            modelBuilder.Entity<Book>()
                .HasIndex(b => b.author);

            modelBuilder.Entity<Book>()
                .HasIndex(b => b.ISBN)
                .IsUnique();

            modelBuilder.Entity<usersaccounts>()
                .HasIndex(u => u.name)
                .IsUnique();

            modelBuilder.Entity<usersaccounts>()
                .HasIndex(u => u.email)
                .IsUnique();

            modelBuilder.Entity<Category>()
                .HasIndex(c => c.Name);

            modelBuilder.Entity<kütüphaneuygulaması.Services.RefreshToken>()
                .HasIndex(rt => rt.Token)
                .IsUnique();

            modelBuilder.Entity<kütüphaneuygulaması.Services.RefreshToken>()
                .HasIndex(rt => rt.UserId);

            modelBuilder.Entity<kütüphaneuygulaması.Services.RefreshToken>()
                .HasIndex(rt => rt.ExpiresAt);

            // Soft delete filter
            modelBuilder.Entity<Book>()
                .HasQueryFilter(b => b.IsActive);

            modelBuilder.Entity<Category>()
                .HasQueryFilter(c => c.IsActive);

            modelBuilder.Entity<usersaccounts>()
                .HasQueryFilter(u => u.IsActive);

            modelBuilder.Entity<kütüphaneuygulaması.Services.RefreshToken>()
                .HasQueryFilter(rt => rt.IsActive);
        }

        public override int SaveChanges()
        {
            UpdateAuditFields();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateAuditFields();
            return base.SaveChangesAsync(cancellationToken);
        }

        private void UpdateAuditFields()
        {
            var entries = ChangeTracker.Entries()
                .Where(e => e.Entity is Book || e.Entity is Category || e.Entity is Cart || 
                           e.Entity is Favorite || e.Entity is orders || e.Entity is report || 
                           e.Entity is usersaccounts)
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

            foreach (var entry in entries)
            {
                if (entry.State == EntityState.Added)
                {
                    if (entry.Entity is Book book)
                        book.CreatedDate = DateTime.Now;
                    else if (entry.Entity is Category category)
                        category.CreatedDate = DateTime.Now;
                    else if (entry.Entity is Cart cart)
                        cart.CreatedDate = DateTime.Now;
                    else if (entry.Entity is Favorite favorite)
                        favorite.CreatedDate = DateTime.Now;
                    else if (entry.Entity is orders order)
                        order.CreatedDate = DateTime.Now;
                    else if (entry.Entity is report report)
                        report.CreatedDate = DateTime.Now;
                    else if (entry.Entity is usersaccounts user)
                        user.CreatedDate = DateTime.Now;
                }
                else if (entry.State == EntityState.Modified)
                {
                    if (entry.Entity is Book book)
                        book.UpdatedDate = DateTime.Now;
                    else if (entry.Entity is Category category)
                        category.UpdatedDate = DateTime.Now;
                    else if (entry.Entity is usersaccounts user)
                        user.UpdatedDate = DateTime.Now;
                }
            }
        }
    }
}
