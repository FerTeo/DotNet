using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using OSSocial.Data;
using OSSocial.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace OSSocial.Controllers
{
    [Authorize]
    [Route("Notifications")]
    public class NotificationsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public NotificationsController(ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _db = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        
        
        [NonAction]
        public async Task<Notification> CreateNotificationAsync(string userId, NotificationType type, string? actorId = null,
            string? referenceId = null, string? message = null)
        {
            //cream notificarea din alte controllere
            
            var notification = new Notification
            {
                UserId = userId,
                ActorUserId = actorId,
                Type = type,
                ReferenceId = referenceId,
                Message = message,
                Date = DateTime.UtcNow,
            };
            

            await _db.Notifications.AddAsync(notification);
            await _db.SaveChangesAsync();
            return notification;
        }
        
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                return RedirectToRoute("/Identity/Account/Login");
            }

            var notifications = await _db.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.Date)
                .ToListAsync();
            
            return View("~/Views/Notification/Index.cshtml", notifications);
        }

        [HttpPost("ReadNotification/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                return RedirectToRoute("/Identity/Account/Login");
            }

            var notification = await _db.Notifications.FindAsync(id);

            if (notification == null)
            {
                return NotFound();
            }
            
            _db.Notifications.Remove(notification);

            _db.SaveChanges();
            

            return RedirectToAction(nameof(Index));
        }

        [HttpPost("Accept/{id}")]
        public async Task<IActionResult> Accept(int id)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                return RedirectToRoute("/Identity/Account/Login");
            }

            var notification = await _db.Notifications.FindAsync(id);

            if (notification == null)
            {
                return NotFound();
            }
            

            if (notification.Type == NotificationType.Follow)
            {
                if (string.IsNullOrEmpty(notification.ActorUserId))
                {
                    return BadRequest();
                }

                var followerId = notification.ActorUserId;
                var follow = await _db.Follows
                    .FirstOrDefaultAsync(f => f.FollowerId == followerId && f.FolloweeId == userId);

                if (follow != null)
                {
                    follow.Status = FollowStatus.Accepted;
                    follow.RespondedAt = DateTime.UtcNow;
                }
                else
                {
                    _db.Follows.Add(new Follow
                    {
                        FollowerId = followerId,
                        FolloweeId = userId,
                        Status = FollowStatus.Accepted,
                        RequestedAt = DateTime.UtcNow,
                        RespondedAt = DateTime.UtcNow
                    });
                }
            }
            else if (notification.Type == NotificationType.GroupRequest)
            {
                if (string.IsNullOrEmpty(notification.ActorUserId) || string.IsNullOrEmpty(notification.ReferenceId))
                {
                    return BadRequest();
                }

                if (!int.TryParse(notification.ReferenceId ?? string.Empty, out var groupId))
                {
                    return BadRequest();
                }

                var group = await _db.Groups.FindAsync(groupId);
                if (group == null) return NotFound();

                var isOwner = group.UserId == userId;
                var isSiteAdmin = User.IsInRole("Admin");
                var isGroupAdmin = await _db.GroupMembers
                    .AnyAsync(gm => gm.GroupId == groupId && gm.UserId == userId && gm.IsModerator);

                if (!isOwner && !isSiteAdmin && !isGroupAdmin) return Forbid();

                var alreadyMember = await _db.GroupMembers
                    .AnyAsync(gm => gm.GroupId == groupId && gm.UserId == notification.ActorUserId);

                if (!alreadyMember)
                {
                    _db.GroupMembers.Add(new GroupMember
                    {
                        GroupId = groupId,
                        UserId = notification.ActorUserId,
                        IsModerator = false
                    });
                }
            }

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}