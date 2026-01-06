using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OSSocial.Data;
using Microsoft.EntityFrameworkCore;
using OSSocial.Models;

namespace OSSocial.Controllers;

public class FollowController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    //constructor
    public FollowController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [HttpPost]
    public async Task<IActionResult> Follow(string targetId)
    {
        //userul pe care vrem sa il urmarim
        var targetUser = await _userManager.FindByIdAsync(targetId);
        if (targetUser == null)
        {
            TempData["Error"] = "User not found.";
            return RedirectToAction("Index", "Profiles");
        }

        // verificam userul curent
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null)
        {
            TempData["Error"] = "You must be logged in to follow users.";
            //redirectionam inapoi pe profilul targetului
            return RedirectToAction("Show", "Profiles", new { username = targetUser.UserName });
        }

        if (currentUser.Id == targetId)
        {
            TempData["Error"] = "You cannot follow yourself.";
            return RedirectToAction("Show", "Profiles", new { username = targetUser.UserName });
        }

        //verificam daca exista deja cererea
        var followRequest = await _db.Follows.FirstOrDefaultAsync(f => f.FollowerId == currentUser.Id && f.FolloweeId == targetUser.Id);
        if (followRequest != null)
        {
            if (followRequest.Status == FollowStatus.Accepted)
            {
                TempData["Message"] = "You are already following this user.";
                return RedirectToAction("Show", "Profiles", new { username = targetUser.UserName });
            }
            if (followRequest.Status == FollowStatus.Pending)
            {
                TempData["Message"] = "Follow request is pending.";
                return RedirectToAction("Show", "Profiles", new { username = targetUser.UserName });
            }
        }


        //cream cererea noua
        var follow = new Follow
        {
            FollowerId = currentUser.Id,
            FolloweeId = targetUser.Id,
            Status = FollowStatus.Pending
        };

        // daca nu are contul privat atunci accepta automat cererea
        if (targetUser.IsPrivate == false)
        {
            follow.Status = FollowStatus.Accepted;
        }

        _db.Follows.Add(follow);

        await _db.SaveChangesAsync();
        
        // setam un mesaj simplu si redirectionam inapoi la profil
        TempData["Message"] = follow.Status == FollowStatus.Accepted ? "You are now following this user." : "Follow request sent.";
        return RedirectToAction("Show", "Profiles", new { username = targetUser.UserName });

    }

    [HttpPost]
    public async Task<IActionResult> Unfollow(string targetId)
    {
        var targetUser = await _userManager.FindByIdAsync(targetId);
        if (targetUser == null)
        {
            TempData["Error"] = "User not found.";
            return RedirectToAction("Index", "Profiles");
        }

        //verificam ca userul sa fie logat
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null)
        {
            TempData["Error"] = "You must be logged in.";
            return RedirectToAction("Show", "Profiles", new { username = targetUser.UserName });
        }

        if (currentUser.Id == targetId)
        {
            TempData["Error"] = "You cant unfollow yourself";
            return RedirectToAction("Show", "Profiles", new { username = targetUser.UserName });
        }

        //cautam cererea
        var follow = await _db.Follows.FirstOrDefaultAsync(x =>
                x.FollowerId == currentUser.Id &&
                x.FolloweeId == targetUser.Id);
        
        
        if (follow == null)
        {
            TempData["Message"] = "You are not following this user.";
            return RedirectToAction("Show", "Profiles", new { username = targetUser.UserName });
        }

        _db.Follows.Remove(follow);
        await _db.SaveChangesAsync();
        TempData["Message"] = "Unfollowed.";
        return RedirectToAction("Show", "Profiles", new { username = targetUser.UserName });
    }

    [HttpPost]
    public async Task<IActionResult> Accept(int followId, bool accept)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null)
        {
            TempData["Error"] = "You must be logged in.";
            return RedirectToAction("PendingRequests");
        }

        //cautam cererea pending catre utilizator
        var follow = await _db.Follows.FirstOrDefaultAsync(x =>
            x.Id == followId &&
            x.FolloweeId == currentUser.Id &&
            x.Status == FollowStatus.Pending);

        if (follow == null)
        {
            TempData["Error"] = "Request not found.";
            return RedirectToAction("PendingRequests");
        }

        if (accept)
        {
            follow.Status = FollowStatus.Accepted;
            follow.RespondedAt = DateTime.Now;
            await _db.SaveChangesAsync();
            TempData["Message"] = "Follow request accepted.";
        }
        else
        {
            // daca respinge, stergem cererea
            _db.Follows.Remove(follow);
            await _db.SaveChangesAsync();
            TempData["Message"] = "Follow request rejected.";
        }

        // notificatile - TODO
        
        
        return RedirectToAction("PendingRequests");
    }

    [HttpGet]
    public async Task<IActionResult> PendingRequests()
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null)
            return BadRequest();

        //cautam toate cererile pending catre utilizator
        var pendingFollows = await _db.Follows
            .Include(f => f.Follower)
            .Where(f => f.FolloweeId == currentUser.Id && f.Status == FollowStatus.Pending)
            .ToListAsync();

        return View(model: pendingFollows);
    }
}