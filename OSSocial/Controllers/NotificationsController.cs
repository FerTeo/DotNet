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

    public class NotificationsController
    (
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager
    ) : Controller
    {
        private readonly ApplicationDbContext _db=context;
        private readonly UserManager<ApplicationUser> _userManager=userManager;



        /// <summary>
        ///  Crearea unei notificari (apelate de controllerele Follow si Groups
        /// </summary>
        /// <param name="userId">
        ///  Id-ul utilizatorului care primeste notificarea 
        /// </param>
        /// <param name="type">
        /// Tipul notificarii (follow request sau request de intrare in grup)
        /// </param>
        /// <param name="actorId">
        ///  Cel care a declansat trimiterea notificarii
        /// </param>
        /// <param name="referenceId">
        ///  Obiectul catre care face trimitire notificarea
        /// </param>
        /// <param name="message">
        ///  Mesajul notificarii
        /// </param>
        /// <returns></returns>
        
        [NonAction] //Non action pentru ca user-ul nu intervine in notificari sunt facute de sistem
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
        
        
        
        /// <summary>
        ///  Afisarea notificarilor utilizatorului curent
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> Index()
        {
            var currentUserId = _userManager.GetUserId(User);
            if (currentUserId == null)
            {
                return RedirectToRoute("/Identity/Account/Login");
            }

            var notifications = await _db.Notifications
                .Where(n => n.UserId == currentUserId)
                .OrderByDescending(n => n.Date)
                .ToListAsync();
            
            return View("~/Views/Notification/Index.cshtml", notifications);
        }
        
        
        /// <summary>
        ///  Citirea unei notificari (adica o stergem)
        /// </summary>
        /// <param name="id">
        ///  Id-ul notificarii pe care o citim
        /// </param>
        /// <returns></returns>
        [HttpPost("ReadNotification/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var currentUserId = _userManager.GetUserId(User);
            if (currentUserId == null)
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
        
        
        /// <summary>
        /// Acceptarea unei notificari
        /// </summary>
        /// <param name="id">
        /// Id-ul unei notificarii
        /// </param>
        /// <returns></returns>
        [HttpPost("Accept/{id}")]
        public async Task<IActionResult> Accept(int id)
        {
            var currentUserId = _userManager.GetUserId(User);
            if (currentUserId == null)
            {
                return RedirectToRoute("/Identity/Account/Login");
            }

            var notification = await _db.Notifications.FindAsync(id);

            if (notification == null)
            {
                return NotFound();
            }
            
            //notificare de tip follow
            if (notification.Type == NotificationType.Follow)
            {
                if (string.IsNullOrEmpty(notification.ActorUserId))
                {
                    return BadRequest();
                }

                var followerId = notification.ActorUserId;
                var follow = await _db.Follows
                    .FirstOrDefaultAsync(f => f.FollowerId == followerId && f.FolloweeId == currentUserId);

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
                        FolloweeId = currentUserId,
                        Status = FollowStatus.Accepted,
                        RequestedAt = DateTime.UtcNow,
                        RespondedAt = DateTime.UtcNow
                    });
                }
            }
            else if (notification.Type == NotificationType.GroupRequest)
            {
                if (string.IsNullOrEmpty(notification.ActorUserId) || string.IsNullOrEmpty(notification.ReferenceId))
                    return BadRequest();

                // convertim din string in int
                if (!int.TryParse((string?)notification.ReferenceId, out var groupId))
                    return BadRequest();

                var group = await _db.Groups.FindAsync(groupId);
                if (group == null) return NotFound();

                // schimbam statusul
                var member = await _db.GroupMembers.FirstOrDefaultAsync(gm =>
                    gm.GroupId == group.Id && gm.UserId == notification.ActorUserId && gm.Status == RequestStatus.Pending);

                if (member != null)
                {
                    member.Status = RequestStatus.Accepted;
                }
                
            }

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}