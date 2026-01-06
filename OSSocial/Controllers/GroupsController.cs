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
                    .Include(g => g.User)
                    .ToList();

                if (!groups.Any())
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
                    .Where(g => g.UserId == currentUserId || g.Members.Any(m => m.UserId == currentUserId))
                    .ToList();
                
                if (!groups.Any())
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
        
        [HttpGet("Explore")]
        public IActionResult Explore()
        {
            SetAccessRights();

            var publicGroups = db.Groups
                .Include(g => g.Members)
                .Include(g => g.User)
                .Where(g => g.IsPublic)
                .ToList();

            ViewBag.PublicGroups = publicGroups;

            return View();
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

            // Determine viewing rights for current user (don't redirect; render page with info)
            var currentUserId = User?.FindFirstValue(ClaimTypes.NameIdentifier);
            bool isAdmin = User?.IsInRole("Admin") == true;
            bool isOwner = group.UserId == currentUserId;
            bool isMember = db.GroupMembers.Any(gm => gm.GroupId == group.Id && gm.UserId == currentUserId);

            bool canView = group.IsPublic || isAdmin || isOwner || isMember;

            // expose flags to the view so it can show a friendly message / hide controls when necessary
            ViewBag.CanView = canView;
            ViewBag.IsMember = isMember;
            ViewBag.IsOwner = isOwner;
            ViewBag.EsteAdmin = isAdmin;
            
            // pt a afisa toti membrii grupului
            var member = db.GroupMembers 
                .Include(gm => gm.User)
                .Where(gm => gm.GroupId == group.Id)
                .ToList();
            
            ViewBag.Members = member;

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
            // owner 
            group.UserId = _userManager.GetUserId(User);

            if (ModelState.IsValid)
            {
                db.Groups.Add(group);
                db.SaveChanges();
                
                // also need to add owner member entry
                GroupMember groupMember = new GroupMember(_userManager.GetUserId(User), group.Id);
                groupMember.IsModerator = true;
                
                db.GroupMembers.Add(groupMember);
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
        
        // functionalitatea propriu-zisa de a da join intr-un grup
        // poti da join unui grup doar daca esti logat
        [HttpPost("JoinGroup")]
        [Authorize(Roles =  "Admin, User, Editor")]
        public IActionResult JoinGroup(int GroupId)
        {
            var group = db.Groups.Find(GroupId);
            if (group == null)
            {
                TempData["message"] = "Group not found";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Explore");
            }

            var currentUserId = _userManager.GetUserId(User);

            // check if already a member
            bool isAlreadyMember = db.GroupMembers.Any(gm => gm.GroupId == GroupId && gm.UserId == currentUserId);
            if (isAlreadyMember)
            {
                TempData["message"] = "You are already a member of this group.";
                TempData["messageType"] = "alert-info";
                return RedirectToAction("GroupProfile", new { id = GroupId });
            }

            GroupMember newMember = new GroupMember(currentUserId, group.Id);
            db.GroupMembers.Add(newMember);
            db.SaveChanges();

            TempData["message"] = "You have joined the group!";
            TempData["messageType"] = "alert-success";
            return RedirectToAction("GroupProfile", new { id = GroupId });
        }

        [HttpPost("LeaveGroup")]
        [Authorize(Roles = "Admin, User, Editor")]
        public IActionResult LeaveGroup(int GroupId)
        {
            var group = db.Groups.Find(GroupId);

            if (group == null)
            {
                TempData["message"] = "Group not found";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Explore");
            }
            
            var currentUserId = _userManager.GetUserId(User);
            
            // owner-ul nu iese din grup, il sterge
            var isOwner = group.UserId == currentUserId;
            
            // un membru ar trebui sa poata iesi din grup normal
            var membership = db.GroupMembers.FirstOrDefault(gm => gm.GroupId == GroupId && gm.UserId == currentUserId);

            if (membership == null)
            {
                TempData["message"] = "You are not a member of this group.";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("GroupProfile", new { id = GroupId });
            }
            else
            if (isOwner) // daca e owner, sterge grupul
            {
                // ca sa stergi un entry din tabel MAI INTAI STERGI TOT CE E LEGAT DE EL
                // (ma intrebam de ce imi da eroare ...)
                
                // sterge postarile asociate grupului
                var groupPosts = db.Posts.Where(p => p.GroupId == GroupId).ToList();
                if (groupPosts.Any())
                {
                    db.Posts.RemoveRange(groupPosts);
                }

                // sterge membrii grupului (intrarile din tabela asociativa GroupMembers)
                var groupMembers = db.GroupMembers.Where(gm => gm.GroupId == GroupId).ToList();
                if (groupMembers.Any())
                {
                    db.GroupMembers.RemoveRange(groupMembers);
                }
                
                db.Groups.Remove(group);
                db.SaveChanges();   
                
                TempData["message"] = "Group deleted :(";
                return RedirectToAction("Explore");
            }
            else // doar membru simplu
            {
                db.GroupMembers.Remove(membership);
                db.SaveChanges();
                
                TempData["message"] = "Officially left the group...";
            }
            
            // can see public groups after leaving
            // grupurile private tho nu pot fi vazute 
            if (group.IsPublic)
            {
                return RedirectToAction("GroupProfile", new { id = GroupId });
            }
            else
            {
                return RedirectToAction("Explore");
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
            
            ViewBag.UserCurent = _user_manager_getuserid_safe();
        }

        // small helper to safely get current user id even if user manager is null
        private string? _user_manager_getuserid_safe()
        {
            try
            {
                return _userManager?.GetUserId(User);
            }
            catch
            {
                return null;
            }
        }
    }    
}
