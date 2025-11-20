using System.ComponentModel.DataAnnotations;

namespace Library2.Models
{
    public class Genre
    {
        [Key]
        public int IdGenre { get; set; }

        [Required]
        [StringLength(45)]
        public string Name { get; set; }

        // Навигационные свойства для связи многие-ко-многим с книгами
        public virtual ICollection<BookGenre> BookGenres { get; set; } = new List<BookGenre>();
    }
}
