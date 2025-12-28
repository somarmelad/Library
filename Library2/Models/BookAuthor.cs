using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Library2.Models
{
    public class BookAuthor
    {
        [Key]
        public int BookId { get; set; }

        [Key]
        public int AuthorId { get; set; }

        // Навигационные свойства
        [ForeignKey("BookId")]
        public virtual Book Book { get; set; }

        [ForeignKey("AuthorId")]
        public virtual Author Author { get; set; }
    }
}