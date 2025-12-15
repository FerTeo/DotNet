using Microsoft.AspNetCore.Mvc;
using OSSocial.Models;
using OSSocial.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace OSSocial.Controllers;

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
    
    
    //afisarea utilizatorilor
    [HttpGet]
    public IActionResult Index()
    {
        var users = db.Users.OrderBy(u => u.UserName);
                      
        ViewBag.UsersList = users;

        return View();
    }
    
    //profilul unui utilizator
    [HttpGet("Show/{username}")]
    public async Task<ActionResult> ShowAsync(string username)
    {
        ApplicationUser? user = await db.Users
            .FirstOrDefaultAsync(u=>u.NormalizedUserName == username.ToUpper());

        if (user is null)
        {
            return NotFound();
        }
        else
        {
            var roles = await _userManager.GetRolesAsync(user);
                 
            ViewBag.Roles = roles;

            ViewBag.UserCurent = await _userManager.GetUserAsync(User);

            return View(user);
        } 
    }
    
    
    
    
    
    
    

}