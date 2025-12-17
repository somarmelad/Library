using System.ComponentModel.DataAnnotations;

namespace Library2.Models
{
    public class Reader
    {

        [Key]
        public int IdReader { get; set; }
        [Required]
        [StringLength(50)]
        public string Login { get; set; }

        [Required]
        [StringLength(100)]
        public string PasswordHash { get; set; }

        
        [Required]
        public string Role { get; set; } = "Reader";
        [Required]
        [StringLength(45)]
        public string LastName { get; set; }

        [Required]
        [StringLength(45)]
        public string FirstName { get; set; }

        [StringLength(45)]
        public string MiddleName { get; set; }

        [StringLength(25)]
        public string Phone { get; set; }

        [DataType(DataType.Date)]
        public DateTime? BirthDate { get; set; }

        
        public virtual ICollection<BookLoan> BookLoans { get; set; } = new List<BookLoan>();
    }
}
