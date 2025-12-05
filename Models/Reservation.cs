using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Library2.Models
{
    public class Reservation
    {
        [Key]
        public int IdReservation { get; set; }

        // Связь с книгой
        public int BookId { get; set; }
        [ForeignKey("BookId")]
        public Book Book { get; set; }

        // Связь с читателем (если пользователь аутентифицирован как читатель)
        // Если аутентификация не реализована, можно использовать IdReader, 
        // но здесь сделаем универсально, предполагая, что читатель будет выбран позже.
        public int? ReaderId { get; set; }
        [ForeignKey("ReaderId")]
        public Reader Reader { get; set; } // Читатель, который бронирует

        [Required]
        [DataType(DataType.DateTime)]
        public DateTime ReservationDate { get; set; } = DateTime.Now; // Дата бронирования

        [DataType(DataType.DateTime)]
        public DateTime? ExpirationDate { get; set; } // Срок действия брони

        // Статус: Pending (Ожидает), Ready (Готово к выдаче), Completed (Выдано), Canceled (Отменено)
        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Pending";
    }
}
