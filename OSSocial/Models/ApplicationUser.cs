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
        
        public bool? IsPrivate { get; set; }
        
        public string? ProfileImageUrl { get; set; }
        
        // un user poate posta mai multe postari
        public virtual ICollection<Post>? Posts { get; set; }
        
        // lista ce contine toate postarile din baza de date (de forma id_postare - titlu postare)
        [NotMapped]
        public IEnumerable<SelectListItem>? UsedListPosts { get; set;}
        // variabila in care vom retine rolurile existente in baza de date
        // pentru popularea unui dropdown list
        [NotMapped]
        public IEnumerable<SelectListItem>? AllRoles { get; set; }
    }
}