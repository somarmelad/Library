using System.ComponentModel.DataAnnotations;

namespace Library2.Models
{
    public class UserActivityLog
    {
        [Key]
        public int Id { get; set; }

        public int ReaderId { get; set; } 
        public int BookId { get; set; }   
        public string ActivityType { get; set; } 
        public DateTime DateAction { get; set; } 
        public string Details { get; set; } 
        


        public Reader Reader { get; set; }
        public Book Book { get; set; }
    }
}