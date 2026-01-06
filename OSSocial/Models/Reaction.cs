using System.ComponentModel.DataAnnotations;

namespace OSSocial.Models
{

    public class Reaction
    { 
        
        [Key]
       public int Id { get; set; }
       
       [Required]
       public int PostId { get; set; }
       
              
       [Required]
       public DateTime DateCreated { get; set; }
       
       [Required]
       public string? UserId { get; set; }

       
       public virtual Post? Post { get; set; }
       
       public virtual ApplicationUser? User { get; set; }
       
       
       
    }
}