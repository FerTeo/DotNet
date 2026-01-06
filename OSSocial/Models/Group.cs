using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace OSSocial.Models
{
    public class Group 
    {
        [Key]
        public int Id { get; set; }

        public int GetGroupId()
        {
            return Id;
        }

        [Required(ErrorMessage = "Can't create a group without a name :(")]
        public string Name { get; set; } = string.Empty;
        
        public string? Description { get; set; }
        
        // cheie externa (FK) - un grup este creat de catre un singur user
        // practic OwnerId!! pt ca grupul are si o alta relatie cu utilizatorii, una many to many (membrii grupului)
        public string? UserId { get; set; }
        
        public virtual ApplicationUser? User { get; set; }

        // un grup poate avea o colectie de postari 
        public virtual ICollection<Post> GroupPosts { get; set; } = new List<Post>();
        
        // relatia many-to-many dintre Group si ApplicationUser (membrii grupului)
        // un membru poate fi utilizator simplu sau moderator
        public virtual ICollection<GroupMember> Members { get; set; } = new List<GroupMember>();

        // daca grupul este privat doar membrii grupului pot vedea postarile din el
        // altfel toata lumea le poate vedea
        // by default grupurile sunt publice
        [DefaultValue(true)]
        public bool IsPublic { get; set; } = true;
    }
    
}
