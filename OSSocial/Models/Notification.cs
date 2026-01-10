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

        public string UserId { get; set; } = null!;
        public string? ActorUserId { get; set; }
        public NotificationType Type { get; set; }
        public string? ReferenceId {get;set;}
        public string? Message {get;set;}
        public DateTime Date {get;set;}
    }
    
}

