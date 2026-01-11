using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OSSocial.Data;
using Microsoft.EntityFrameworkCore;
using OSSocial.Models;

namespace OSSocial.Controllers;

public class FollowController
(
    ApplicationDbContext context,
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager
) : Controller
{
    private readonly ApplicationDbContext _db=context;
    private readonly UserManager<ApplicationUser> _userManager=userManager;

    
    /// <summary>
    ///  Urmarire unui utilizator
    /// </summary>
    /// <param name="targetId">
    /// Id-ul utilizatorului pe care user-ul il urmaresye
    /// </param>
    /// <returns></returns>

    [HttpPost]
    public async Task<IActionResult> Follow(string targetId)
    {
        var targetUser = await _userManager.FindByIdAsync(targetId);
        if (targetUser == null)
        {
            TempData["Error"] = "User not found.";
            return RedirectToAction("Index", "Profiles");
        }


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
                TempData["Message_Profile"] = "You are already following this user.";
                return RedirectToAction("Show", "Profiles", new { username = targetUser.UserName });
            }
            if (followRequest.Status == FollowStatus.Pending)
            {
                TempData["Message_Profile"] = "Follow request is pending.";
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
        // necesar GetValueOrDefault pentru ca IsPrivate e nullable si ramane in null daca nu e setat
        // GetValueOrDefault returneaza false daca e null
        if (targetUser.IsPrivate.GetValueOrDefault() == false)
        {
            follow.Status = FollowStatus.Accepted;
            follow.RequestedAt = DateTime.Now;
        }

        //cream notificarea
        if (targetUser.IsPrivate == true)
        {
            var notification = new Notification
            {
                UserId = targetUser.Id,
                ActorUserId = currentUser.Id,
                Type = NotificationType.Follow,
                ReferenceId = null,
                Message = currentUser.UserName + " wants to follow.",
                Date = DateTime.Now,
            };
            
            _db.Notifications.Add(notification);
            await _db.SaveChangesAsync();
        }

        _db.Follows.Add(follow);

        await _db.SaveChangesAsync();
        
        // setam un mesaj simplu si redirectionam inapoi la profil
        TempData["Message_Profile"] = follow.Status == FollowStatus.Accepted ? "You are now following this user." : "Follow request sent.";
        return RedirectToAction("Show", "Profiles", new { username = targetUser.UserName });

    }
    
    /// <summary>
    ///  Unfollow unui utilizator
    /// </summary>
    /// <param name="targetId">
    /// Id-ul userului caruia vrem sa ii dam unfollow
    /// </param>
    /// <returns></returns>

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
}