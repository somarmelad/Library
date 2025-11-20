using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
