using Microsoft.AspNetCore.Authorization;
using OSSocial.Data;
using OSSocial.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace OSSocial.Controllers
{

    public class ReactionController
    (
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager
    ) : Controller
    {
        private readonly ApplicationDbContext _db = context;
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly RoleManager<IdentityRole> _roleManager = roleManager;


        /// <summary>
        ///  Aflarea numarului de reactii ale unei postari
        /// </summary>
        /// <param name="postId">
        ///  Postarea la care vrem sa aflam numarul de like-uri
        /// </param>
        /// <returns>
        /// Returneaza numarul de reactii
        /// </returns>
        public IActionResult GetCounts(int postId)
        {
            var list = _db.Reactions
                .Where(r => r.PostId == postId)
                .ToList();

            int numberLikes = list.Count;
            return Ok(numberLikes);
        }

        
        
        /// <summary>
        ///  Crearea sau stergerea unei reactii noi.
        /// </summary>
        /// <param name="postId">
        ///  Postarea la care se reactioneaza
        /// </param>
        /// <returns></returns>
        [HttpPost ]
        public IActionResult Toggle(int postId)
        {
            var currentUserId = _userManager.GetUserId(User);
            if (currentUserId==null)
            {
                return RedirectToAction("Details", "Post", new { id = postId });
            }
            
            //verificam daca reactia exista
            var existingReaction = _db.Reactions
                .FirstOrDefault(r => r.PostId == postId && r.UserId == currentUserId);


            //daca nu exista creeam o noua reactie
            if (existingReaction == null)
            {
                var reaction = new Reaction
                {
                    PostId = postId,
                    UserId = currentUserId,
                    DateCreated = DateTime.Now,
                };

                _db.Reactions.Add(reaction);

            }//daca exista stergem reactia existenta
            else
            {
                _db.Reactions.Remove(existingReaction);

            }

            _db.SaveChanges();

            return RedirectToAction("Details","Post", new { id = postId });
        }

    }
}