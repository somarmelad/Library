// Models/Book.cs
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

        [Required(ErrorMessage = "Название обязательно")]
        [StringLength(45, ErrorMessage = "Название не длиннее 45 символов")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Издатель обязателен")]
        public int PublisherId { get; set; }

        // Основной автор (single FK)
        [Required(ErrorMessage = "Основной автор обязателен")]
        public int AuthorId { get; set; }

        [Required(ErrorMessage = "Количество обязательно")]
        [Range(1, int.MaxValue, ErrorMessage = "Количество должно быть больше 0")]
        public int Quantity { get; set; }

        // Навигационные свойства
        [ForeignKey("PublisherId")]
        public virtual Publisher Publisher { get; set; }

        [ForeignKey("AuthorId")]
        public virtual Author Author { get; set; }

        // Many-to-many связи (дополнительные авторы и жанры)
        public virtual ICollection<BookAuthor> BookAuthors { get; set; } = new List<BookAuthor>();
        public virtual ICollection<BookGenre> BookGenres { get; set; } = new List<BookGenre>();
        public virtual ICollection<BookLoan> BookLoans { get; set; } = new List<BookLoan>();
    }
}