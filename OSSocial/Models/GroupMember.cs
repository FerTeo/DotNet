using System.ComponentModel.DataAnnotations.Schema;

namespace OSSocial.Models
{
    public class GroupMember
    {
        // tabelul asociativ!!
        // legatura intre ApplicationUser si Group 
        
        // cheie primara compusa (PK) - UserId + GroupId
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string? UserId { get; set; }
        public int? GroupId { get; set; }
        
        public virtual ApplicationUser? User { get; set; }
        
        public virtual Group? Group { get; set; }
        
        public DateTime JoinDate { get; set; } = DateTime.Now;

        public bool IsModerator { get; set; } = false;
    }
}
