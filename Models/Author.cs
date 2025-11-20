using System.ComponentModel.DataAnnotations;

namespace Library2.Models
{
    public class Author
    {

        [Key]
        public int IdAuthor { get; set; }

        [Required]
        [StringLength(45)]
        public string LastName { get; set; }

        [Required]
        [StringLength(45)]
        public string FirstName { get; set; }

        [StringLength(45)]
        public string MiddleName { get; set; }

        // Навигационные свойства для связи многие-ко-многим с книгами
        public virtual ICollection<BookAuthor> BookAuthors { get; set; } = new List<BookAuthor>();
    }
}
