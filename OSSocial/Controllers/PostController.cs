using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OSSocial.Data;
using OSSocial.Models;
using Microsoft.EntityFrameworkCore;

namespace OSSocial.Controllers
{
    [Route("Post")]
    public class PostController : Controller
    {
        private readonly ApplicationDbContext db;
        
        public PostController(ApplicationDbContext context)
        {
            db = context;
        }

        [HttpGet("")]
        public IActionResult Index()
        {
            return RedirectToAction("Feed");
        }

        [HttpGet("Feed")] // GET /Post/Feed
        public IActionResult Feed()
        {
            // practic functie de index dar in social media terms 
            var posts = from post in db.Posts 
                        orderby post.Time descending
                        select post;
            
            ViewBag.Posts = posts;
            
            return View();
        }

        [HttpGet("Details/{id}")] // GET /Post/Details/5
        public IActionResult Details(int id)
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
            
            return View(postare);
        }

        [Authorize] // necesar ca user-ul sa fie logat, altfel nu poate crea o postare
        [HttpGet("CreatePost")] // GET /Post/CreatePost - returneaza formularul
        public IActionResult CreatePost() // ar tb sa returneze un formular
        {
            return View();
        }

        [HttpPost("CreatePost")]
        [Authorize] // te trimite direct la login daca nu esti logat!!
        public async Task<IActionResult> CreatePost(Post postare, IFormFile Image) // removed Group? group parameter
        { 
            // time and userId set automatically 
            postare.Time = DateTime.Now;
            
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            postare.UserId = currentUserId;

            // validare/ clear validation errors
            ModelState.Remove(nameof(postare.UserId));
            ModelState.Remove(nameof(postare.Time));
            ModelState.Remove(nameof(postare.Group));

            // pentru fisiere media
            if (Image != null && Image.Length > 0)
            {
                // info din articles app lab 9
                // verif extensie
                var allowedExtension = new[] { ".jpg", ".jpeg", ".png", ".gif", ".mp4", ".mov" };
                var fileExtrension = Path.GetExtension(Image.FileName).ToLower();

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
                
                var uniqueFileName = Guid.NewGuid().ToString() + "_" + Image.FileName;
                
                var filePath = Path.Combine(webRootPath, uniqueFileName); // local save path
                var dbPath = "/images/" + uniqueFileName; // db save path
                
                // salvare fisier
                using (var fileStream = new FileStream(filePath, System.IO.FileMode.Create))
                {
                    // await only used on async methods!
                    await Image.CopyToAsync(fileStream);
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
                    // allow redirect only if the group is public or the user is a member/owner/admin
                    // reuse the currentUserId from above
                    var creatorUserId = currentUserId;

                    bool isAdmin = User?.IsInRole("Admin") == true;
                    bool isOwner = group.UserId == creatorUserId;
                    bool isMember = db.GroupMembers.Any(gm => gm.GroupId == groupId && gm.UserId == creatorUserId);

                    if (group.IsPublic || isAdmin || isOwner || isMember)
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
