using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Library2.Models
{
    public class Reservation
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdReservation { get; set; }

        
        public int BookId { get; set; }
        [ForeignKey("BookId")]
        public Book Book { get; set; }

        
        public int? ReaderId { get; set; }
        [ForeignKey("ReaderId")]
        public Reader Reader { get; set; } 

        [Required]
        [DataType(DataType.DateTime)]
        public DateTime ReservationDate { get; set; } = DateTime.Now; 

        [DataType(DataType.DateTime)]
        public DateTime? ExpirationDate { get; set; } 

       
        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Pending";
    }
}
