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
    public class PostController 
    (
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IContentAnalysisService contentService
    ) : Controller
    {
        private readonly ApplicationDbContext _db=context;
        private readonly UserManager<ApplicationUser> _userManager=userManager;
        private readonly RoleManager<IdentityRole> _roleManager=roleManager;
        private readonly IContentAnalysisService _contentService=contentService;


        [HttpGet("")]
        public IActionResult Index()
        {
            return RedirectToAction("Explore");
        }



        /// <summary>
        ///  Afisarea postarilor publice
        /// </summary>
        /// <returns></returns>
        [HttpGet("~/")]
        [HttpGet("")]
        [HttpGet("Explore")]

        public IActionResult Explore()
        {
            // practic functie de index dar in social media terms
            if (User.IsInRole("Admin"))
            {
                var posts = _db.Posts
                    .Include(p => p.User) // nume autor
                    .Include(p => p.Comments) // nr comentarii
                    .Include(p => p.Reactions) // numar like-uri
                    .OrderByDescending(p => p.Time) // cele mai noi postari prima data
                    .ToList();
                ViewBag.Posts = posts;
            }
            else
            {
                var posts = _db.Posts
                    .Include(p => p.User) // nume autor
                    .Include(p => p.Comments) // nr comentarii
                    .Include(p => p.Reactions) // numar like-uri
                    .Where(p => p.User.IsPrivate == false &&
                                !p.Reactions.Any(r => r.UserId == _userManager.GetUserId(User)) &&
                                p.UserId  != _userManager.GetUserId(User)) // doar postarile userilor publici la care NU s-a dat like inca
                    .OrderByDescending(p => p.Time) // cele mai noi postari prima data
                    .ToList();
                ViewBag.Posts = posts;
            }
            
            
            return View();
        }
        
        
        
        /// <summary>
        ///  Afisarea feed ului personalizat
        /// </summary>
        /// <returns></returns>
        [Authorize] // login required
        [HttpGet("Feed")]
        public IActionResult Feed()
        {
            var currentUserId = _userManager.GetUserId(User);
            
            // lista de id-uri ale userilor caruia currentUser ii da follow
            var following = _db.Follows
                .Where(f => f.FollowerId == currentUserId && f.Status == FollowStatus.Accepted)
                .Select(f => f.FolloweeId)
                .ToList();
            
            // postarile userilor din lista 'following'
            var posts = _db.Posts
                .Include(p => p.User)      //  pentru username, poza profil)
                .Include(p => p.Comments)  // nr comentariilor
                .Include(p => p.Reactions) // includem reactiile
                .Where(p => following.Contains(p.UserId) && 
                            !p.Reactions.Any(r => r.UserId == _userManager.GetUserId(User)
                            )) // la fel ca la explore -> totusi fiindca following nu primeste niciodata postarile user-ului curent nu tb sa le scoatem printr-o conditie where
                .OrderByDescending(p => p.Time) // crescator dupa timp
                .ToList();

            // se trimit datele in viewbag
            ViewBag.Posts = posts;

            // vreau sa refolosesc codul de la Explore (vuew-ul va avea un toggle intre explore si feed)
            return View("Explore");
        }
        
        
        /// <summary>
        ///  Afisarea unei postari
        /// </summary>
        /// <param name="id">
        ///  Id-ul postarii care este afisata
        /// </param>
        /// <param name="edit">
        ///  Daca se editeaza un comentariu, se va afisa formularul de edit
        /// </param>
        /// <returns></returns>
        [HttpGet("Details/{id}")] // GET /Post/Details/5
        public IActionResult Details(int id, int? edit)
        {
            Post? post = _db.Posts
                .Include(p => p.User)                    // Include Post Author
                .Include(p => p.Comments)                // Include Comments
                .ThenInclude(c => c.User)                // Include Comment Authors
                .FirstOrDefault(p => p.Id == id);
            
            if (post == null)
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
             
             return View(post);
         }
         
        /// <summary>
        ///  Helper pentru integrarea AI-ului
        /// </summary>
        /// <param name="postare"></param>
        /// <param name="analysisResult"></param>
        /// <returns></returns>
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
        
        /// <summary>
        ///  Afisarea paginii dedicate crearii postarilor
        /// </summary>
        /// <returns></returns>
        [Authorize] // necesar ca user-ul sa fie logat, altfel nu poate crea o postare
        [HttpGet("CreatePost")] // GET /Post/CreatePost - returneaza formularul
        public IActionResult CreatePost() // ar tb sa returneze un formular
        {
            return View();
        }
        
        /// <summary>
        ///  Crearea unei postari + integrare AI
        /// </summary>
        /// <param name="post">
        /// Obiect de tip postare primit din formularul din view-ul CreatePost
        /// </param>
        /// <param name="image"></param>
        /// <returns></returns>
        [HttpPost("CreatePost")]
        [Authorize] // te trimite direct la login daca nu esti logat!!
        public async Task<IActionResult> CreatePost(Post post, IFormFile image) 
        { 
            // time and userId set automatically 
            post.Time = DateTime.Now;
            
            var currentUserId = _userManager.GetUserId(User);
            if (currentUserId == null)
            {
                return BadRequest();
            }
            post.UserId = currentUserId;

            // validare/ clear validation errors
            ModelState.Remove(nameof(post.UserId));
            ModelState.Remove(nameof(post.Time));
            ModelState.Remove(nameof(post.Group));
            
            // pt a posta ceva tb atat titlul cat si continutul sa fie adecvate
            // pe viitor ar tb implementat si analiza video dar...
            
            // pt ca am primit eroarea "TooManyRequests" .. faceam separat verificarea pentru titlu si content.... 
            // unesc titlu si continutul ca sa nu mai primesc aceasta eroare....
            string textToAnalyze = $"Title: {post.Title}\nContent: {post.Content}";
            
            // apel catre API facut doar pe text empty
            if (!string.IsNullOrWhiteSpace(textToAnalyze))
            {
                var analysisResult = await _contentService.AnalyzeContentAsync(textToAnalyze);

                if (!HelperCheckAcceptedPost(ref post, analysisResult))
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

                    return View(post);
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
                    return View(post);
                }
                
                // stocare cale
                var webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
        
                // verificam ca folderul sa existe
                if (!Directory.Exists(webRootPath))
                {
                    Directory.CreateDirectory(webRootPath);
                }
                
                var uniqueFileName = Guid.NewGuid().ToString() + "_" + image.FileName;
                
                var filePath = Path.Combine(webRootPath, uniqueFileName); //cale locala
                var dbPath = "/images/" + uniqueFileName; // cale baza de date
                
                // salvare fisier
                await using (var fileStream = new FileStream(filePath, System.IO.FileMode.Create))
                {
                    // await doar pentru metode async
                    await image.CopyToAsync(fileStream);
                }
                
                ModelState.Remove(nameof(post.Media));
                
                post.Media = dbPath;
            }
            
            _db.Posts.Add(post);
            _db.SaveChanges();
            
            // verifici daca postarea apartine unui grup
            if (post.GroupId != null && post.GroupId != 0)
            {
                var groupId = post.GroupId.Value;
                var group = _db.Groups.Find(groupId);
                if (group != null)
                {
                    // allow posting user to be redirected to the group page if they have access
                    // reuse the currentUserId from above
                    var creatorUserId = currentUserId;

                    bool isAdmin = User?.IsInRole("Admin") == true;
                    bool isOwner = group.UserId == creatorUserId;
                    bool isMember = _db.GroupMembers.Any(gm => gm.GroupId == groupId && gm.UserId == creatorUserId);

                    if (isAdmin || isOwner || isMember)
                    {
                        return RedirectToAction("GroupProfile", "Groups", new { id = groupId });
                    }
                    // otherwise fallthrough to Feed
                }
            }

            return RedirectToAction("Feed"); // dupa ce creezi o postare te intorci pe feed
        }
        
        
        
        /// <summary>
        ///  Afisarea editarii unei postari
        /// </summary>
        /// <param name="id">
        /// Id ul postarii pe care vrem sa o modificam
        /// </param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("EditPost/{id}")] // GET /Post/EditPost/5
        public IActionResult EditPost(int id)
        {
            Post? postare = _db.Posts.Find(id);
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
        
        
        
        
        /// <summary>
        ///  Editarea unui postari
        /// </summary>
        /// <param name="id">
        /// Id-ul postarii pe care vrem sa il editam
        /// </param>
        /// <param name="formPost">
        /// Postarea primitia din formular
        /// </param>
        /// <returns></returns>
        [HttpPost("EditPost/{id}")]
        [Authorize]
        public IActionResult EditPost(int id, Post formPost)
        {
            Post? modifiedPost = _db.Posts.Find(id);
            if (modifiedPost == null)
            {
                return NotFound();
            }
            
            try
            {
                modifiedPost.Title = formPost.Title;
                modifiedPost.Content = formPost.Content;

                _db.SaveChanges();
                
                // postareModif.Time; pt ca pastrez doar ora postarii nu modific timpul, in viitor va tb adaugata chestia asta (adic edit time; sa apara ca postarea a fost editata)
                
                return RedirectToAction("Feed");
            }
            catch (Exception)
            {
                return View("Error");
            }
        }

        
        
        /// <summary>
        ///  Stergerea unei postari
        /// </summary>
        /// <param name="id">
        /// Id-ul postarii pe care o stergem
        /// </param>
        /// <returns></returns>
        [HttpPost("Delete/{id}")]
        public IActionResult Delete(int id)
        {
            Post? postare = _db.Posts.Find(id);
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
            
            var comments = _db.Comments.Where(c => c.PostId == id).ToList();
            if (comments.Any())
            {
                _db.Comments.RemoveRange(comments);
            }

            // pt reactii la fel ca la comentarii
            var reactions = _db.Reactions.Where(r => r.PostId == id).ToList();
            if (reactions.Any())
            {
                _db.Reactions.RemoveRange(reactions);
            }
            
            _db.Posts.Remove(postare);
            _db.SaveChanges();
            
            return RedirectToAction("Feed");
        }

    }
}
