using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OSSocial.Data;
using OSSocial.Models;
using OSSocial.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using ContentResult = OSSocial.Services.ContentResult;

namespace OSSocial.Controllers
{
    [Route("Post")]
    public class PostController (
        UserManager<ApplicationUser> _userManager,
        ApplicationDbContext _context) : Controller
    {
        private readonly ApplicationDbContext db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IContentAnalysisService _contentService;

        public PostController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager, 
            RoleManager<IdentityRole> roleManager, 
            IContentAnalysisService contentService)
        {
            db = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _contentService = contentService;
        }

        [HttpGet("")]
        public IActionResult Index()
        {
            return RedirectToAction("Explore");
        }

        [HttpGet("Explore")] // GET /Post/Explore
        public IActionResult Explore()
        {
            // practic functie de index dar in social media terms 
            var posts = db.Posts
                .Include(p => p.User) // nume autor
                .Include(p => p.Comments) // nr comentarii
                .Include(p => p.Reactions) // numar like-uri
                .Where(p => p.User.IsPrivate == false &&
                            !p.Reactions.Any(r => r.UserId == userManager.GetUserId(User)) && 
                            p.UserId != userManager.GetUserId(User)) // doar postarile userilor publici la care NU s-a dat like inca
                .OrderByDescending(p => p.Time) // cele mai noi postari prima data
                .ToList();
            
            ViewBag.Posts = posts;
            
            return View();
        }

        [Authorize] // login required 
        [HttpGet("Feed")]
        public IActionResult Feed()
        {
            var currentUserID = userManager.GetUserId(User);
            
            // lista de id-uri ale userilor caruia currentUser ii da follow
            var following = db.Follows
                .Where(f => f.FollowerId == currentUserID && f.Status == FollowStatus.Accepted)
                .Select(f => f.FolloweeId)
                .ToList();
            
            // postarile userilor din lista 'following'
            var posts = db.Posts
                .Include(p => p.User)      //  pentru username, poza profil)
                .Include(p => p.Comments)  // nr comentariilor
                .Include(p => p.Reactions) // includem reactiile
                .Where(p => following.Contains(p.UserId) && 
                            !p.Reactions.Any(r => r.UserId == userManager.GetUserId(User)
                            )) // la fel ca la explore -> totusi fiindca following nu primeste niciodata postarile user-ului curent nu tb sa le scoatem printr-o conditie where 
                .OrderByDescending(p => p.Time) // crescator dupa timp
                .ToList();

            // se trimit datele in viewbag
            ViewBag.Posts = posts;

            // vreau sa refolosesc codul de la Explore (vuew-ul va avea un toggle intre explore si feed)
            return View("Explore");
        }

        [HttpGet("Details/{id}")] // GET /Post/Details/5
        public IActionResult Details(int id, int? edit)
        {
            Post? postare = db.Posts
                .Include(p => p.User)                    // Include Post Author
                .Include(p => p.Comments)                // Include Comments
                .ThenInclude(c => c.User)                // Include Comment Authors
                .FirstOrDefault(p => p.Id == id);
            
            if (postare == null)
            {
                return NotFound();
            }
            
            // se trimit catre view informatii despre user-ul care apeleaza Details
            if (User.Identity?.IsAuthenticated == true)
            {
                ViewBag.CurrentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                ViewBag.EsteAdmin = User.IsInRole("Admin");
            }
            else
            {
                ViewBag.CurrentUserId = null;
                ViewBag.EsteAdmin = false;
            }

            
            // daca se editeaza un comentariu, paseaza edit id-ul
            ViewBag.EditCommentId = edit;
             
             return View(postare);
         }
         
         private bool HelperCheckAcceptedPost(ref Post postare, ContentResult analysisResult)
         {
             // Cazul ideal: API-ul a răspuns cu succes
             if (analysisResult.Success)
             {
                 if (analysisResult.IsAccepted)
                 {
                     postare.ContainsInappropriateContent = false;
                     postare.InappropriateContentReason = null;
                     postare.DateReviewed = DateTime.Now;
                     return true;
                 }
                 else
                 {
                     // API-ul a zis NU -> Blocăm
                     return false;
                 }
             }

             // Cazul de eroare (API picat/cheie greșită/parse error)
             ModelState.AddModelError(string.Empty, $"Eroare filtru AI: {analysisResult.ErrorMessage}");
             return false; // <--- MODIFICARE: Blochează postarea dacă AI-ul nu merge
         }

         [Authorize] // necesar ca user-ul sa fie logat, altfel nu poate crea o postare
         [HttpGet("CreatePost")] // GET /Post/CreatePost - returneaza formularul
         public IActionResult CreatePost() // ar tb sa returneze un formular
         {
             return View();
         }

        [HttpPost("CreatePost")]
        [Authorize] // te trimite direct la login daca nu esti logat!!
        public async Task<IActionResult> CreatePost(Post postare, IFormFile image) // removed Group? group parameter
        { 
            // time and userId set automatically 
            postare.Time = DateTime.Now;
            
            var currentUserId = _userManager.GetUserId(User);
            if (currentUserId == null)
            {
                return BadRequest();
            }
            postare.UserId = currentUserId;

            // validare/ clear validation errors
            ModelState.Remove(nameof(postare.UserId));
            ModelState.Remove(nameof(postare.Time));
            ModelState.Remove(nameof(postare.Group));
            
            // pt a posta ceva tb atat titlul cat si continutul sa fie adecvate
            // pe viitor ar tb implementat si analiza video dar...
            
            // pt ca am primit eroarea "TooManyRequests" .. faceam separat verificarea pentru titlu si content.... 
            // unesc titlu si continutul ca sa nu mai primesc aceasta eroare....
            string textToAnalyze = $"Title: {postare.Title}\nContent: {postare.Content}";
            
            // apel catre API facut doar pe text empty
            if (!string.IsNullOrWhiteSpace(textToAnalyze))
            {
                var analysisResult = await _contentService.AnalyzeContentAsync(textToAnalyze);

                if (!HelperCheckAcceptedPost(ref postare, analysisResult))
                {
                    string errorMessage;
                    
                    if (!analysisResult.Success)
                    {
                        // eroare tehnica (ex: TooManyRequests)
                        errorMessage = $"{analysisResult.ErrorMessage}. Please wait a moment and try again.";
                    }
                    else
                    {
                        // respins de AI (continut vulgar)
                        errorMessage = $"Post rejected: {analysisResult.Reason}";
                    }

                    ViewBag.ContinutInadecvat = errorMessage;
                    
                    // debug info ca admin!
                    ViewBag.EsteAdmin = User.IsInRole("Admin");
                    ViewBag.ApiIsAccepted = $"{analysisResult.IsAccepted}";
                    ViewBag.ApiErrorMessage = $"{analysisResult.ErrorMessage}";

                    return View(postare);
                }
            }
            
            ViewBag.EsteAdmin = User.IsInRole("Admin");

            // pentru fisiere media
            if (image != null && image.Length > 0)
            {
                // info din articles app lab 9
                // verif extensie
                var allowedExtension = new[] { ".jpg", ".jpeg", ".png", ".gif", ".mp4", ".mov" };
                var fileExtrension = Path.GetExtension(image.FileName).ToLower();

                if (!allowedExtension.Contains(fileExtrension))
                {
                    ModelState.AddModelError("Image", $"Image {fileExtrension} does not have the correct format. Media to be of an image (jpg, jpeg, png, gif) or a video (mp4,  mov)");
                    return View(postare);
                }
                
                // stocare cale
                var webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
        
                // Ensure folder exists
                if (!Directory.Exists(webRootPath))
                {
                    Directory.CreateDirectory(webRootPath);
                }
                
                var uniqueFileName = Guid.NewGuid().ToString() + "_" + image.FileName;
                
                var filePath = Path.Combine(webRootPath, uniqueFileName); // local save path
                var dbPath = "/images/" + uniqueFileName; // db save path
                
                // salvare fisier
                using (var fileStream = new FileStream(filePath, System.IO.FileMode.Create))
                {
                    // await only used on async methods!
                    await image.CopyToAsync(fileStream);
                }
                
                ModelState.Remove(nameof(postare.Media));
                
                postare.Media = dbPath;
            }
            
            db.Posts.Add(postare);
            db.SaveChanges();
            
            // verifici daca postarea apartine unui grup
            if (postare.GroupId != null && postare.GroupId != 0)
            {
                var groupId = postare.GroupId.Value;
                var group = db.Groups.Find(groupId);
                if (group != null)
                {
                    // allow posting user to be redirected to the group page if they have access
                    // reuse the currentUserId from above
                    var creatorUserId = currentUserId;

                    bool isAdmin = User?.IsInRole("Admin") == true;
                    bool isOwner = group.UserId == creatorUserId;
                    bool isMember = db.GroupMembers.Any(gm => gm.GroupId == groupId && gm.UserId == creatorUserId);

                    if (isAdmin || isOwner || isMember)
                    {
                        return RedirectToAction("GroupProfile", "Groups", new { id = groupId });
                    }
                    // otherwise fallthrough to Feed
                }
            }

            return RedirectToAction("Feed"); // dupa ce creezi o postare te intorci pe feed
        }

        [Authorize]
        [HttpGet("EditPost/{id}")] // GET /Post/EditPost/5
        public IActionResult EditPost(int id)
        {
            Post? postare = db.Posts.Find(id);
            if (postare == null)
            {
                return NotFound();
            }
            
            // if care verifica daca modificarile provin de la acelasi user care a creat proiectul 
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (postare.UserId != currentUserId)
            {
                return Forbid();
            }
            
            ViewBag.Post = postare;
            return View(postare);
        }

        
        [HttpPost("EditPost/{id}")]
        [Authorize]
        public IActionResult EditPost(int id, Post postareFormular)
        {
            Post? postareModif = db.Posts.Find(id);
            if (postareModif == null)
            {
                return NotFound();
            }
            
            try
            {
                postareModif.Title = postareFormular.Title;
                postareModif.Content = postareFormular.Content;

                db.SaveChanges();
                
                // postareModif.Time; pt ca pastrez doar ora postarii nu modific timpul, in viitor va tb adaugata chestia asta (adic edit time; sa apara ca postarea a fost editata)
                
                return RedirectToAction("Feed");
            }
            catch (Exception)
            {
                return View("Error");
            }
        }

        [HttpPost("Delete/{id}")]
        public IActionResult Delete(int id)
        {
            Post? postare = db.Posts.Find(id);
            if (postare == null)
            {
                return NotFound();
            }
            
            // if care verifica daca modificarile provin de la acelasi user care a creat proiectul 
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (postare.UserId != currentUserId && !User.IsInRole("Admin"))
            {
                return Forbid();
            }
            
            db.Posts.Remove(postare);
            db.SaveChanges();
            
            return RedirectToAction("Feed");
        }

    }
}
