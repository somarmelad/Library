using System.ComponentModel.DataAnnotations;

namespace Library2.Models
{
    public class Publisher
    {
        [Key]
        public int IdPublisher { get; set; }

        [Required]
        [StringLength(45)]
        public string Name { get; set; }

        // Навигационные свойства
        public virtual ICollection<Book> Books { get; set; } = new List<Book>();
    }
}
