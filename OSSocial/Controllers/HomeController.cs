using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using OSSocial.Models;
using Microsoft.AspNetCore.Identity;

namespace OSSocial.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly UserManager<ApplicationUser> _userManager;

    public HomeController(ILogger<HomeController> logger, UserManager<ApplicationUser> userManager)
    {
        _logger = logger;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        if (User?.Identity?.IsAuthenticated == true)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                // redirect to Profiles/Show with the user's username
                return RedirectToAction("Show", "Profiles", new { username = user.UserName });
            }
        }

        // not logged in -> fallback to Post Explore
        return RedirectToAction("Explore", "Post");
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
