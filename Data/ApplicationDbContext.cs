using Library2.Models;
using Microsoft.EntityFrameworkCore;
using System;
using static System.Collections.Specialized.BitVector32;

namespace Library2.Data
{
    public class ApplicationDbContext : DbContext
    {

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // DbSet для всех таблиц
        public DbSet<Reader> Readers { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<Book> Books { get; set; }
        public DbSet<Author> Authors { get; set; }
        public DbSet<Genre> Genres { get; set; }
        public DbSet<Publisher> Publishers { get; set; }
        public DbSet<BookLoan> BookLoans { get; set; }
        public DbSet<BookAuthor> BookAuthors { get; set; }
        public DbSet<BookGenre> BookGenres { get; set; }
        public DbSet<Reservation> Reservations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Настройка составных ключей для связей многие-ко-многим
            modelBuilder.Entity<BookAuthor>()
                .HasKey(ba => new { ba.BookId, ba.AuthorId });

            modelBuilder.Entity<BookGenre>()
                .HasKey(bg => new { bg.BookId, bg.GenreId });

            // Настройка связей
            modelBuilder.Entity<BookLoan>()
                .HasOne(bl => bl.Book)
                .WithMany(b => b.BookLoans)
                .HasForeignKey(bl => bl.BookId);

            modelBuilder.Entity<BookLoan>()
                .HasOne(bl => bl.Reader)
                .WithMany(r => r.BookLoans)
                .HasForeignKey(bl => bl.ReaderId);

            modelBuilder.Entity<BookLoan>()
                .HasOne(bl => bl.Employee)
                .WithMany(e => e.BookLoans)
                .HasForeignKey(bl => bl.EmployeeId);

            modelBuilder.Entity<Book>()
                .HasOne(b => b.Publisher)
                .WithMany(p => p.Books)
                .HasForeignKey(b => b.PublisherId);

            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Book>()
                .Property(e => e.Status)
                .HasConversion<string>();
        }
    }
}
