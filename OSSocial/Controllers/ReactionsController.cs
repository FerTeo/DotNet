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


        //aflarea nr de reactii ale unei postari
        public IActionResult GetCounts(int postId)
        {
            var list = _db.Reactions
                .Where(r => r.PostId == postId)
                .ToList();

            int numberLikes = list.Count;
            return Ok(numberLikes);
        }

        [HttpPost]
        public IActionResult NewReaction(Reaction reaction)
        {
            reaction.DateCreated = DateTime.Now;
            reaction.UserId = _userManager.GetUserId(User);
            
            //aceste proprietati nu sunt trimise in formular
            ModelState.Remove(nameof(reaction.Post));
            ModelState.Remove(nameof(reaction.User));
            ModelState.Remove(nameof(reaction.UserId));

            if (ModelState.IsValid)
            {
                _db.Reactions.Add(reaction);
                _db.SaveChanges();
                
            }
            return Redirect("/Post/Details/" + reaction.PostId);
        }

        [HttpPost ]
        public IActionResult Toggle(int postId)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var existingReaction = _db.Reactions
                .FirstOrDefault(r => r.PostId == postId && r.UserId == userId);


            //daca nu exista creeam o noua reacti
            if (existingReaction == null)
            {
                var reaction = new Reaction
                {
                    PostId = postId,
                    UserId = userId,
                    DateCreated = DateTime.Now,
                };

                _db.Reactions.Add(reaction);

            }
            else
            {
                _db.Reactions.Remove(existingReaction);

            }

            _db.SaveChanges();

            return RedirectToAction("Details","Post", new { id = postId });
        }

    }
}