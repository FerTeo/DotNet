using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Net.Mime;
using Microsoft.EntityFrameworkCore;

namespace OSSocial.Models
{
    public class Post
    {
        [Key]
        public int Id { get; set; }

        public string Title { get; set; }

        [Required(ErrorMessage = "oops... seems you're uploading a post without a content :(")]
        public string? Content { get; set; }

        // for now im implementing it in an easy manner aka keeping only post date&time  
        public DateTime Time { get; set; }

        [NotMapped]// documentation: "Denotes that a property or class should be excluded from database mapping."
        public string formattedTime
        {
            get { return Time.ToString("HH:mm"); }
        }

        // public List<DateTime> Dates { get; set; }
        // keeps a list of all edit times -> including the time it was posted 
        // btw should be using IEnumerable(AN INTERFACE) for the DateTime because we should be able to iterate through it with foreach in the .cshtml file (i think that's how it works for now)
        
        // mai trebuie adaugata ideea de postare a imaginilor/ video-urilor etc.
        
        // foreign key-ul utilizatorului 
        public string UserId { get; set; } // string pt ca identity users maps to string
        
        public virtual ApplicationUser? User { get; set; }
    }    
}
