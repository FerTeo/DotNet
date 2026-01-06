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

        // Calculăm totalul pentru paginare (dacă vei implementa paginarea mai jos)
        int totalUsers = usersQuery.Count();
        ViewBag.TotalUsers = totalUsers;

        // Logica pentru URL-ul de paginare
        if (!string.IsNullOrEmpty(search))
        {
            ViewBag.PaginationBaseUrl = "/Profiles/Index/?search=" + search + "&page=";
        }
        else
        {
            ViewBag.PaginationBaseUrl = "/Profiles/Index/?page=";
        }

        return View();
    }
    
    
    // /Profiles/Show/admin
    [HttpGet("Show/{username}")]
    //profilul unui utilizator
    public async Task<ActionResult> ShowAsync(string username)
    {
        ApplicationUser? user = await db.ApplicationUsers
            .Include(u => u.Posts) 
            .FirstOrDefaultAsync(u => u.NormalizedUserName == username.ToUpper());
        
        if (user is null)
        {
            return NotFound();
        }
        else
        {
            var roles = await _userManager.GetRolesAsync(user);

            // pentru a afisa postarile utilizatorului pe profilul acestuia
            
            ViewBag.Roles = roles;

            ViewBag.UserCurent = await _userManager.GetUserAsync(User);

            return View(user);
        } 
    }
    
    
    
    
    
    
    

}