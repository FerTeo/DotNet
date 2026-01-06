using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace OSSocial.Models
{
    public enum FollowStatus
    {
        Pending =0,
        Accepted = 1,
    }
    public class Follow
    {
        [Key]
        public int Id { get; set; }
        
        //urmaritorul
        [Required]
        public string FollowerId { get; set; } = null!;
        [ForeignKey("FollowerId")]
        public ApplicationUser Follower { get; set; } = null!;
        
        //urmaritul
         public string FolloweeId { get; set; } = null!; 

        [ForeignKey("FolloweeId")]
        public ApplicationUser Followee { get; set; } = null!;

        public FollowStatus Status { get; set; } = FollowStatus.Pending;
        public DateTime RequestedAt { get; set; } = DateTime.Now;
        public DateTime? RespondedAt { get; set; }
        
    }
}
