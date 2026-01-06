using OSSocial.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OSSocial.Models;
using System.Security.Claims;

namespace OSSocial.Controllers
{
    [Route("Groups")]
    public class GroupsController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext db;

        public GroupsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            db = context;
            _userManager = userManager;
        }
        
        // show ALL available groups
        // doar admin-ul poate vedea toate grupurile
        [HttpGet("")] 
        [Authorize(Roles = "Admin, User, Editor")]
        public IActionResult Index()
        {
            if (TempData.ContainsKey("message"))
            {
                ViewBag.message = TempData["message"];
                ViewBag.Alert = TempData["messageType"];
            }

            SetAccessRights();

            if (User.IsInRole("Admin"))
            {
                var groups = db.Groups
                    .Include(g => g.Members)
                    .Include(g => g.User);

                if (groups is null)
                {
                    return NotFound();
                }
                else
                {
                    ViewBag.Groups = groups;
                    return View();
                }
            }
            else if (User.IsInRole("Editor") || User.IsInRole("User"))
            {
                var currentUserId = _userManager.GetUserId(User);
                
                var groups = db.Groups
                    .Include(g => g.Members)
                    .Include(g => g.User)
                    .Where(g => g.UserId == currentUserId || g.Members.Any(m => m.UserId == currentUserId));
                
                if (groups is null)
                {
                    return NotFound();
                }
                else
                {
                    ViewBag.Groups = groups;
                    return View();
                }
            }
            else
            {
                TempData["message"] = "Must be logged in to view groups...";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Index", "Post");
            }
        }
        
        // show all posts on a group's profile page
        [HttpGet("Group/{id}")]
        public IActionResult GroupProfile(int id)
        {
            SetAccessRights();

            // preiau grupul impreuna cu postarile si userii care au postat
            var group = db.Groups
                .Include(g => g.GroupPosts)
                .ThenInclude(p => p.User) // member who posted
                .Include(g => g.User) // include owner
                .FirstOrDefault(g => g.Id == id); 

            if (group is null)
            {
                TempData["message"] = "Group not found";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Index", "Post");
            }

            // verific daca grupul este privat
            if (group.IsPublic == false)
            {
                // caut membrul in baza de date
                var esteMembru = db.GroupMembers
                    .Where(gm => gm.GroupId == id)
                    .Where(gm => gm.UserId == _userManager.GetUserId(User))
                    .FirstOrDefault();

                // daca nu este gasit si nu e nici admin nu poate accesa continutul grupului
                if (esteMembru == null && !User.IsInRole("Admin"))
                {
                    TempData["message"] = "You must be a member of this group to view its content.";
                    TempData["messageType"] = "alert-danger";
                    return RedirectToAction("Index", "Post");
                }
            }
            
            return View(group);
        }

        // CREATE 
        // formularul de creare grup nou
        [HttpGet("Create")]
        [Authorize(Roles = "Admin, User, Editor")]
        public IActionResult Create()
        {
            return View();
        }
        
        // adaugarea propriu-zisa in baza de date
        [HttpPost("Create")]
        [Authorize(Roles = "Admin, User, Editor")]
        public IActionResult Create(Group group)
        {
            group.UserId = _userManager.GetUserId(User);

            if (ModelState.IsValid)
            {
                db.Groups.Add(group);
                db.SaveChanges();
                
                TempData["message"] = "Group created successfully :P";
                TempData["messageType"] = "alert-success";
                
                return RedirectToAction("Index");
            }
            else
            {
                return View(group);
            }
        }
        
        // ca sa afisezi butoane de editare/ stergere in view
        private void SetAccessRights()
        {
            ViewBag.AfisareButoane = false; // default - nu se afiseaza butoane de editare/stergere

            if (User.IsInRole("Editor") || User.IsInRole("User"))
            {
                ViewBag.AfisareButoane = true;
            }
            
            ViewBag.EsteAdmin = User.IsInRole("Admin");
            
            ViewBag.UserCurent = _userManager.GetUserId(User);
}
    }    
}
