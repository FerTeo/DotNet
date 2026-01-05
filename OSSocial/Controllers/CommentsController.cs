using Microsoft.AspNetCore.Authorization;
using OSSocial.Data;
using OSSocial.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace OSSocial.Controllers
{
    public class CommentsController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager) : Controller
    {
        private readonly ApplicationDbContext db = context;
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly RoleManager<IdentityRole> _roleManager = roleManager;

        // Add comment (asociat unei postari si unui utilizator)
        [HttpPost]
        [Authorize] // orice utilizator logat poate adauga comentarii
        public IActionResult New(Comment comentariu)
        {
            comentariu.DateCreated = DateTime.Now;
            comentariu.UserId = _userManager.GetUserId(User);

            ModelState.Remove(nameof(comentariu.Post));
            ModelState.Remove(nameof(comentariu.User));
            ModelState.Remove(nameof(comentariu.UserId));
            
            if (ModelState.IsValid) // verifica datele 
            {
                db.Comments.Add(comentariu);
                db.SaveChanges();
                return Redirect("/Post/Details/" + comentariu.PostId); // afiseaza postarea dupa adaugarea comentariului
            }
            else
            {
                return Redirect("/Post/Details/" + comentariu.PostId);
            }
        }

        // Delete comment
        [HttpPost]
        [Authorize(Roles = "User,Editor,Admin")]
        public IActionResult Delete(int id)
        {
            Comment? comentariu = db.Comments.Find(id); // iau comentariul din baza de date

            if (comentariu == null)
            {
                return NotFound();
            }
            else
            {
                // un comentariu poate fi sters de: 
                //  - persoana care l-a scris
                //  - un admin 
                //  - detinatorul contului postarii 
                if (comentariu.UserId == _userManager.GetUserId(User)
                    || User.IsInRole("Admin")
                    || comentariu.Post.UserId == _userManager.GetUserId(User))
                {
                    db.Comments.Remove(comentariu);
                    db.SaveChanges();
                    return Redirect("/Posts/Details/" + comentariu.PostId);
                }
                else
                {
                    TempData["message"] = "Can't delete a comment that isn't yours!";
                    TempData["messageType"] = "alert-danger";

                    return RedirectToAction("Index", "Post");
                }
            }
        }

        // Edit comment 
        [HttpPost]
        [Authorize(Roles = "User,Editor,Admin")]
        public IActionResult Edit(int id, string content)
        {
            Comment? comentariu = db.Comments.Find(id);

            if (comentariu == null)
            {
                return NotFound();
            }
            else
            {
                if (comentariu.UserId == _userManager.GetUserId(User)
                    || User.IsInRole("Admin")
                    || comentariu.Post.UserId == _userManager.GetUserId(User))
                {
                    return View(comentariu);
                }
                else
                {
                    TempData["message"] = "Can't delete a comment that isn't yours!";
                    TempData["messageType"] = "alert-danger";

                    return RedirectToAction("Index", "Post");
                }
            }
        }

        [HttpPost]
        [Authorize(Roles = "User,Editor,Admin")]
        public IActionResult EditConfirm(int id, Comment comentariu)
        {
            Comment? comentariuDeModificat = db.Comments.Find(id);

            if (comentariuDeModificat == null)
            {
                return NotFound();
            }
            else
            {
                if (comentariuDeModificat.UserId == _userManager.GetUserId(User)
                    || User.IsInRole("Admin")
                    || comentariuDeModificat.Post.UserId == _userManager.GetUserId(User))
                {
                    if (ModelState.IsValid)
                    {
                        comentariuDeModificat.Content = comentariu.Content;
                        db.SaveChanges();
                        return Redirect("/Post/Details/" + comentariuDeModificat.PostId);
                    }
                    else
                    {
                        return View(comentariu);
                    }
                }
                else
                {
                    TempData["message"] = "Can't edit a comment that isn't yours!";
                    TempData["messageType"] = "alert-danger";

                    return RedirectToAction("Index", "Post");
                }
            }
        }
    }
}


