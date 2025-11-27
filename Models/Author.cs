// Models/Author.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Library2.Models
{
    public class Author
    {
        [Key]
        public int IdAuthor { get; set; }

        [Required]
        [StringLength(45)]
        public string FirstName { get; set; }

        [Required]
        [StringLength(45)]
        public string LastName { get; set; }

        [StringLength(45)]
        public string? MiddleName { get; set; }

        // Computed property для отображения в SelectList
        public string FullName => $"{FirstName} {LastName}" + (string.IsNullOrEmpty(MiddleName) ? "" : $" {MiddleName}");

        // Навигационные свойства
        public virtual ICollection<BookAuthor> BookAuthors { get; set; } = new List<BookAuthor>();
        public virtual ICollection<Book> Books { get; set; } = new List<Book>(); // Для нового FK
    }
}