using Microsoft.AspNetCore.Authorization;
using OSSocial.Data;
using OSSocial.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OSSocial.Services;
using ContentResult = OSSocial.Services.ContentResult;

namespace OSSocial.Controllers
{
    [Route("Comments")]
    public class CommentsController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IContentAnalysisService contentService) : Controller

    {
        private readonly ApplicationDbContext db = context;
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly RoleManager<IdentityRole> _roleManager = roleManager;
        private readonly IContentAnalysisService _contentService = contentService;

        // Add comment (asociat unei postari si unui utilizator)
        [HttpPost("New")]
        [Authorize] // orice utilizator logat poate adauga comentarii
        public async Task<IActionResult> New(Comment comentariu)
        {
            comentariu.DateCreated = DateTime.Now;
            comentariu.UserId = _userManager.GetUserId(User);

            ModelState.Remove(nameof(comentariu.Post));
            ModelState.Remove(nameof(comentariu.User));
            ModelState.Remove(nameof(comentariu.UserId));
            
            // validare model
            if (!ModelState.IsValid)
            {
                return Redirect("/Post/Details/" + comentariu.PostId + "?err=empty");
            }
            
            if (!string.IsNullOrWhiteSpace(comentariu.Content))
            {
                var analysisResult = await _contentService.AnalyzeContentAsync(comentariu.Content);

                // if API call failed
                if (!analysisResult.Success)
                {
                    TempData["message"] = $"{analysisResult.ErrorMessage}. Please wait a moment and try again.";
                    TempData["messageType"] = "alert-danger";
                    return Redirect("/Post/Details/" + comentariu.PostId);
                }

                // if content is explicitly rejected by the AI
                if (!analysisResult.IsAccepted)
                {
                    TempData["message"] = $"Sorry, but you comment has been rejected :( \n: {analysisResult.Reason}";
                    TempData["messageType"] = "alert-danger";

                    // provide admin debug info
                    TempData["ApiIsAccepted"] = analysisResult.IsAccepted.ToString();
                    TempData["ApiErrorMessage"] = analysisResult.ErrorMessage ?? string.Empty;

                    return Redirect("/Post/Details/" + comentariu.PostId);
                }
            }

            db.Comments.Add(comentariu);
            db.SaveChanges();
            return Redirect("/Post/Details/" + comentariu.PostId); // afiseaza postarea dupa adaugarea comentariului
        }

        // Delete comment
        [HttpPost("Delete/{id}")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "User,Editor,Admin")]
        public IActionResult Delete(int id, string? returnUrl)
        {
            // Load comment including its Post to safely access post owner and postId
            Comment? comentariu = db.Comments
                .Include(c => c.Post)
                .FirstOrDefault(c => c.Id == id);

            if (comentariu == null)
            {
                return NotFound();
            }

            var postId = comentariu.PostId;
            var postOwnerId = comentariu.Post?.UserId;
            var currentUserId = _userManager.GetUserId(User);

            // un comentariu poate fi sters de: 
            //  - persoana care l-a scris
            //  - un admin 
            //  - detinatorul contului postarii 
            if (comentariu.UserId == currentUserId
                || User.IsInRole("Admin")
                || postOwnerId == currentUserId)
            {
                db.Comments.Remove(comentariu);
                db.SaveChanges();

                // Prefer returning to a provided local returnUrl (the view includes one), otherwise go to Post/Details
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                return RedirectToAction("Details", "Post", new { id = postId });
            }
            else
            {
                TempData["message"] = "Can't delete a comment that isn't yours!";
                TempData["messageType"] = "alert-danger";

                return RedirectToAction("Details", "Post", new { id = postId });
            }
        }

        
        [HttpGet("Edit/{id}")]
        [Authorize(Roles = "User,Editor,Admin")]
        public IActionResult Edit(int id)
        {
            var comentariu = db.Comments
                .Include(c => c.Post)
                .FirstOrDefault(c => c.Id == id);

            if (comentariu == null) return NotFound();

            var currentUserId = _userManager.GetUserId(User);
            var postOwnerId = comentariu.Post?.UserId;

            if (comentariu.UserId != currentUserId && !User.IsInRole("Admin") && postOwnerId != currentUserId)
            {
                return Forbid();
            }

            var redirectUrl = Url.Action("Details", "Post", new { id = comentariu.PostId, edit = id }) + "#comment-" + id;
            return Redirect(redirectUrl);
        }

        [HttpPost("Edit/{id}")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "User,Editor,Admin")]
        public IActionResult Edit(int id, Comment comentariu)
        {
            Comment? comentariuDeModificat = db.Comments
                .Include(c => c.Post)
                .FirstOrDefault(c => c.Id == id);

            if (comentariuDeModificat == null)
            {
                return NotFound();
            }

            var currentUserId = _userManager.GetUserId(User);
            var postOwnerId = comentariuDeModificat.Post?.UserId;

            if (comentariuDeModificat.UserId != currentUserId && !User.IsInRole("Admin") && postOwnerId != currentUserId)
            {
                return Forbid();
            }

            ModelState.Remove(nameof(comentariu.Post));
            ModelState.Remove(nameof(comentariu.User));
            ModelState.Remove(nameof(comentariu.UserId));
            ModelState.Remove(nameof(comentariu.PostId));

            if (!ModelState.IsValid)
            {
                // Redirect back to details showing the edit form again
                var errorRedirect = Url.Action("Details", "Post", new { id = comentariuDeModificat.PostId, edit = id }) + "#comment-" + id;
                return Redirect(errorRedirect);
            }

            comentariuDeModificat.Content = comentariu.Content;
            db.SaveChanges();

            var redirect = Url.Action("Details", "Post", new { id = comentariuDeModificat.PostId }) + "#comment-" + comentariuDeModificat.Id;
            return Redirect(redirect);
        }
    }
}
