using System.ComponentModel.DataAnnotations.Schema;

namespace OSSocial.Models
{
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
        
        public DateTime JoinDate { get; set; }

        public bool IsModerator { get; set; }

        // constructori
        public GroupMember() { }

        public GroupMember(string userId, int groupId)
        {
            UserId = userId;
            GroupId = groupId;
            JoinDate = DateTime.Now;
            IsModerator = false;
        }
    }
}
