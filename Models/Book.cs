using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
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


        public int PublisherId { get; set; }
        [ValidateNever]
        [ForeignKey("PublisherId")]
        public virtual Publisher Publisher { get; set; }

        [Required(ErrorMessage = "Количество обязательно")]
        [Range(1, int.MaxValue, ErrorMessage = "Количество должно быть > 0")]
        public int Quantity { get; set; } = 1;

      
        [Display(Name = "Аннотация")]
        
        public string? Annotation { get; set; }
        
        public virtual ICollection<BookAuthor> BookAuthors { get; set; } = new List<BookAuthor>();
        public virtual ICollection<BookGenre> BookGenres { get; set; } = new List<BookGenre>();
        public virtual ICollection<BookLoan> BookLoans { get; set; } = new List<BookLoan>();
    }
}