using System.ComponentModel.DataAnnotations.Schema;

namespace OSSocial.Models
{

    public enum RequestStatus
    {
        Pending =0,
        Accepted =1
    }
    public class GroupMember
    {
        // tabelul asociativ!!
        // legatura intre ApplicationUser si Group 
        
        // cheie primara compusa (PK) - UserId + GroupId
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // database generated -> nu e nevoie sa setez valoarea in constructor
        public int Id { get; set; }
        public string? UserId { get; set; }
        public int? GroupId { get; set; }
        
        public virtual ApplicationUser? User { get; set; }
        
        public virtual Group? Group { get; set; }
        
        public RequestStatus Status { get; set; } = RequestStatus.Accepted;
        
        public DateTime JoinDate { get; set; }

        public bool IsModerator { get; set; }

        // constructori
        public GroupMember() { }

        public GroupMember(string userId, int groupId, RequestStatus status)
        {
            UserId = userId;
            GroupId = groupId;
            JoinDate = DateTime.Now;
            IsModerator = false;
            Status = status;
        }
    }
}
