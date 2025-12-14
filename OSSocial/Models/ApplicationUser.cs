using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations.Schema;

namespace OSSocial.Models
{
    public class ApplicationUser : IdentityUser
    {
        
        // atribute suplimentare adaugate pentru user
        public string? FirstName { get; set; }

        public string? LastName { get; set; }
        
        public string? Bio { get; set; }
        
        public bool? IsPrivate { get; set; }
        
        
        // un user poate posta mai multe postari
        //TODO
        // public virtual ICollection<Posts> Posts { get; set; }

        // variabila in care vom retine rolurile existente in baza de date
        // pentru popularea unui dropdown list
        [NotMapped]
        public IEnumerable<SelectListItem>? AllRoles { get; set; }
    }
}