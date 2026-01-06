using System.ComponentModel.DataAnnotations;

namespace OSSocial.Models
{
    public class Comment
    {
        [Key]
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Can't post an empty comment :(")]
        public string Content { get; set; }
        
        public DateTime DateCreated { get; set; }
        
        // cheie esterna (FK) - un comentariu apartine unei postari
        public int PostId { get; set; }
        
        public virtual Post Post { get; set; }
        
        // cheie externa (FK) - un comentariu este postat de catre un user
        public string? UserId { get; set; }
        
        public virtual ApplicationUser? User { get; set; }
    }
}

