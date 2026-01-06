using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations.Schema;

namespace OSSocial.Models
{
    public class ApplicationUser : IdentityUser
    {
        
        // atribute suplimentare adaugate pentru user
        public string? DisplayName { get; set; }
        public string? Bio { get; set; }
        public bool? IsPrivate { get; set; } = false;
        public string? ProfileImageUrl { get; set; }
        
        
        public ICollection<Follow> Followers { get; set; } = new List<Follow>();
        public ICollection<Follow> Following { get; set; } = new List<Follow>();
        
        // un user poate posta mai multe postari
        public virtual ICollection<Post>? Posts { get; set; }
        
        // lista ce contine toate postarile din baza de date (de forma id_postare - titlu postare)
        [NotMapped]
        public IEnumerable<SelectListItem>? UsedListPosts { get; set;}
        // variabila in care vom retine rolurile existente in baza de date
        // pentru popularea unui dropdown list
        [NotMapped]
        public IEnumerable<SelectListItem>? AllRoles { get; set; }
        
        // GRUPURILE PE CARE UTILIZATORUL LE-A CREAT
        // un user poate crea mai multe grupuri
        // un grup este creat de catre un SINGUR user
        public ICollection<Group>? OwnedGroups { get; set; }
        
        // GRUPURILE IN CARE UTILIZATORUL ESTE MEMBRU
        // colectie de grupuri pt fiecare utilizator
        // un user poate apartine mai multor grupuri 
        // un grup poate avea mai multi membri
        public ICollection<GroupMember>? GroupMembership { get; set; }
    }
}