using System.ComponentModel.DataAnnotations;

namespace OSSocial.Models
{
    public enum NotificationType
    {
        Follow,
        Reaction,
        Comment,
        GroupRequest
    }
    public class Notification
    {
        [Key]
        public int Id {get;set;}

        public string UserId { get; set; } = null!;//userul care primeste notificarea
        public string? ActorUserId { get; set; }//userul care trimite notificare
        public NotificationType Type { get; set; }
        public string? ReferenceId {get;set;}
        public string? Message {get;set;}
        public DateTime Date {get;set;}
    }
    
}

