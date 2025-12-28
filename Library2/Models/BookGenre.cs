using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Library2.Models
{
    public class BookGenre
    {
        [Key]
        public int BookId { get; set; }

        [Key]
        public int GenreId { get; set; }

        // Навигационные свойства
        [ForeignKey("BookId")]
        public virtual Book Book { get; set; }

        [ForeignKey("GenreId")]
        public virtual Genre Genre { get; set; }
    }
}
