
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OSSocial.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace OSSocial.Areas.Identity.Pages.Account.Manage
{
    public class CustomizeProfileModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IWebHostEnvironment _env;

        public CustomizeProfileModel(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IWebHostEnvironment env)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _env = env;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string? CurrentImageUrl { get; set; }

        public class InputModel
        {
            public IFormFile? ProfileImage { get; set; }

            [Display(Name = "Bio")]
            [MaxLength(1000)]
            public string? Bio { get; set; }

            [Display(Name = "Private account")]
            public bool IsPrivate { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            Input = new InputModel
            {
                Bio = user.Bio,
                IsPrivate = user.IsPrivate ?? false
            };
            CurrentImageUrl = user.ProfileImageUrl;
            ViewData["ActivePage"] = ManageNavPages.CustomizeProfile;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            if (!ModelState.IsValid)
            {
                CurrentImageUrl = user.ProfileImageUrl;
                ViewData["ActivePage"] = ManageNavPages.CustomizeProfile;
                return Page();
            }

            // la fel ca in PostController.CreatePost
            if (Input.ProfileImage != null && Input.ProfileImage.Length > 0)
            {
                var allowedExtension = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var fileExtension = Path.GetExtension(Input.ProfileImage.FileName).ToLowerInvariant();

                if (!allowedExtension.Contains(fileExtension))
                {
                    ModelState.AddModelError("Input.ProfileImage", $"File {fileExtension} does not have the correct format.");
                    CurrentImageUrl = user.ProfileImageUrl;
                    ViewData["ActivePage"] = ManageNavPages.CustomizeProfile;
                    return Page();
                }

                
                //stocare cale
                var webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");

                // asiguram ca folderul exista
                if (!Directory.Exists(webRootPath))
                {
                    Directory.CreateDirectory(webRootPath);
                }

                var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(Input.ProfileImage.FileName);
                var filePath = Path.Combine(webRootPath, uniqueFileName);
                var dbPath = "/images/" + uniqueFileName;

                //salvare fisier
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await Input.ProfileImage.CopyToAsync(fileStream);
                }

                user.ProfileImageUrl = dbPath;
            }

            user.Bio = Input.Bio;
            user.IsPrivate = Input.IsPrivate;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                foreach (var e in result.Errors) ModelState.AddModelError(string.Empty, e.Description);
                CurrentImageUrl = user.ProfileImageUrl;
                ViewData["ActivePage"] = ManageNavPages.CustomizeProfile;
                return Page();
            }

            await _signInManager.RefreshSignInAsync(user);
            TempData["StatusMessage"] = "Profile updated";
            return RedirectToPage("./Index");
        }
    }
}
