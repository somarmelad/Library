using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Library2.Models
{
    public class Book
    {
        [Key]
        public int IdBook { get; set; }

        [Required]
        [StringLength(45)]
        public string Title { get; set; }

        [Required]
        public int PublisherId { get; set; }

        [Required]

        public int Quantity { get; set; }
        [Range(1, int.MaxValue, ErrorMessage = "Количество должно быть > 0")]
        // Навигационные свойства
        [ForeignKey("PublisherId")]
        public virtual Publisher Publisher { get; set; }

        // Связи многие-ко-многим
        public virtual ICollection<BookAuthor> BookAuthors { get; set; } = new List<BookAuthor>();
        public virtual ICollection<BookGenre> BookGenres { get; set; } = new List<BookGenre>();
        public virtual ICollection<BookLoan> BookLoans { get; set; } = new List<BookLoan>();
    }
}
