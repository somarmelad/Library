using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Library2.Models
{

    public enum BookStatus
    {
        [Display(Name = "В наличии")]
        Available,

        [Display(Name = "Зарезервирована")]
        Reserved,

        [Display(Name = "Выдана")]
        Issued
    }

    public class Book
    {
        [Key]
        public int IdBook { get; set; }

        [Required(ErrorMessage = "Название обязательно")]
        [StringLength(45, ErrorMessage = "Название не длиннее 45 символов")]
        public string Title { get; set; }


        public int PublisherId { get; set; }
        [ValidateNever]
        [ForeignKey("PublisherId")]
        public virtual Publisher Publisher { get; set; }
        public int GenreId { get; set; }
        [ValidateNever]
        [ForeignKey("GenreId")]
        public virtual Genre Genre { get; set; }

        [Required(ErrorMessage = "Статус обязателен")]
        public BookStatus Status { get; set; } = BookStatus.Available;


        [Display(Name = "Аннотация")]
        
        public string? Annotation { get; set; }
        
        public virtual ICollection<BookAuthor> BookAuthors { get; set; } = new List<BookAuthor>();
        public virtual ICollection<BookGenre> BookGenres { get; set; } = new List<BookGenre>();
        public virtual ICollection<BookLoan> BookLoans { get; set; } = new List<BookLoan>();
    }
}