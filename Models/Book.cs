using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static System.Runtime.InteropServices.JavaScript.JSType;

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

        // Навигационные свойства
        [ForeignKey("PublisherId")]
        public virtual Publisher Publisher { get; set; }

        [ForeignKey("ShelfId")]

        // Связи многие-ко-многим
        public virtual ICollection<BookAuthor> BookAuthors { get; set; } = new List<BookAuthor>();
        public virtual ICollection<BookGenre> BookGenres { get; set; } = new List<BookGenre>();
        public virtual ICollection<BookLoan> BookLoans { get; set; } = new List<BookLoan>();
    }
}
