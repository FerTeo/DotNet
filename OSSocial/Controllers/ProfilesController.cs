using Microsoft.AspNetCore.Mvc;
using OSSocial.Models;
using OSSocial.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace OSSocial.Controllers;

[Route("Profiles")]
public class ProfilesController : Controller
{
    private readonly ApplicationDbContext db;

    private readonly UserManager<ApplicationUser> _userManager;

    private readonly RoleManager<IdentityRole> _roleManager;

    public ProfilesController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager
    )
    {
        db = context;

        _userManager = userManager;

        _roleManager = roleManager;
    }
    
    // /Profiles/Index
    [HttpGet]
    //afisarea utilizatorilor
    public IActionResult Index()
    {
        // se ia termenul de cautare din url
        var search = Convert.ToString(HttpContext.Request.Query["search"])?.Trim();

        // se construieste interogarea de baza
        var usersQuery = db.ApplicationUsers.AsQueryable();

        // filtrez dupa numele de utilizator daca s-a dat un termen de cautare
        if (!string.IsNullOrEmpty(search))
        {
            usersQuery = usersQuery.Where(u => u.UserName.Contains(search));
        }

        usersQuery = usersQuery.OrderBy(u => u.UserName);

        // setare ViewBag-uri pentru View
        ViewBag.SearchString = search;
        
        // lista propriu-zisa 
        ViewBag.UsersList = usersQuery.ToList(); 

        return View();
    }
    
    
    // /Profiles/Show/admin
    //profilul unui utilizator
    [HttpGet("Show/{username}")]
    public async Task<ActionResult> ShowAsync(string username)
    {
        ApplicationUser? targetUser = await db.ApplicationUsers
            .Include(u => u.Posts) 
            .FirstOrDefaultAsync(u => u.NormalizedUserName == username.ToUpper());

        if (targetUser is null)
        {
            TempData["Error"] = "User not found.";
            return RedirectToAction("Index", "Profiles");
        }

        // utilizatorul curent (poate fi null daca nu e intregistart!)
        var currentUser = await _userManager.GetUserAsync(User);

        // numarul de urmaritori si urmarii
        var followersCount = await db.Follows.CountAsync(f => f.FolloweeId == targetUser.Id && f.Status == FollowStatus.Accepted);
        var followingCount = await db.Follows.CountAsync(f => f.FollowerId == targetUser.Id && f.Status == FollowStatus.Accepted);

        // determinam realtia dintre utilizatorul curent si profilul
        bool isFollowing = false;
        bool isPending = false;
        bool showFollowButton = false;

        // daca nu e un user care isi vizualizeaza propriul profil 
        if (currentUser != null && currentUser.Id != targetUser.Id)
        {
            showFollowButton = true;
            var relationship = await db.Follows
                .FirstOrDefaultAsync(f => f.FollowerId == currentUser.Id 
                                          && f.FolloweeId == targetUser.Id);
            if (relationship != null)
            {
                if (relationship.Status == FollowStatus.Accepted)
                {
                    isFollowing = true;
                }

                if (relationship.Status == FollowStatus.Pending)
                {
                    isPending = true;
                }
            }
        }
        
        
        //verificam daca se pot vedea postarile
        bool ShowPosts = false;

        if (isFollowing == true || targetUser.IsPrivate == false || targetUser==currentUser)
        {
            ShowPosts = true;
        }
        
        

        var roles = await _userManager.GetRolesAsync(targetUser);

        ViewBag.Roles = roles;
        ViewBag.UserCurent = currentUser;
        ViewBag.IsCurrentUser = currentUser != null && currentUser.Id == targetUser.Id;
        ViewBag.FollowersCount = followersCount;
        ViewBag.FollowingCount = followingCount;
        ViewBag.IsFollowing = isFollowing;
        ViewBag.IsPending = isPending;
        ViewBag.ShowFollowButton = showFollowButton;
        ViewBag.ShowPosts = ShowPosts;

        return View(targetUser);
    }
}