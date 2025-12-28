using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Library2.Models
{
    public class BookLoan
    {
        
        [Key]
        public int IdBookLoan { get; set; }
        
        [Required]
        public int BookId { get; set; }
        
        [Required]
        public int ReaderId { get; set; }
       
        [Required]
        public int EmployeeId { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime LoanDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? ReturnDate { get; set; }

        // Навигационные свойства
        [ValidateNever]
        [ForeignKey("BookId")]
        public virtual Book Book { get; set; } = null!;

        [ValidateNever]
        [ForeignKey("ReaderId")]
        public virtual Reader Reader { get; set; } = null!;

        [ValidateNever]
        [ForeignKey("EmployeeId")]
        public virtual Employee Employee { get; set; } = null!;
    } 
}
