using Microsoft.AspNetCore.Mvc;
using OSSocial.Data;
using OSSocial.Models;

namespace OSSocial.Controllers
{
    public class PostController : Controller
    {
        private readonly ApplicationDbContext db;
        
        public PostController(ApplicationDbContext context)
        {
            db = context;
        }

        public IActionResult Feed()
        {
            // practic functie de index dar in social media terms 
            var posts = from post in db.Posts 
                        orderby post.Time descending
                        select post;
            
            ViewBag.Posts = posts;
            
            return View();
        }

        public IActionResult Details(int id)
        {
            Post postare = db.Posts.Find(id);
            ViewBag.Post = postare;
            return View();
        }

        public IActionResult CreatePost() // ar tb sa returneze un formular
        {
            return View();
        }

        [HttpPost]
        public IActionResult CreatePost(Post postare) // metoda apelata cand se da submit la formular
        {
            try
            {
                db.Posts.Add(postare);
                db.SaveChanges();
                return RedirectToAction("Feed"); // dupa ce creezi o postare te intorci pe feed
            }
            catch (Exception)
            {
                return View("Error");
            }
        }

        public IActionResult EditPost(int id)
        {
            Post postare = db.Posts.Find(id);
            ViewBag.Post = postare;
            return View();
        }

        [HttpPost]
        public IActionResult EditPost(int id, Post postareFormular)
        {
            Post postareModif = db.Posts.Find(id);

            try
            {
                postareModif.Title = postareFormular.Title;
                postareModif.Content = postareFormular.Content;
                postareModif.Id = postareFormular.Id;
                postareModif.UserId = postareFormular.UserId;
                // postareModif.Time; pt ca pastrez doar ora postarii nu modific timpul, in viitor va tb adaugata chestia asta 
                
                return RedirectToAction("Feed");
            }
            catch (Exception)
            {
                return View("Error");
            }
        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
            Post postare = db.Posts.Find(id);
            db.Posts.Remove(postare);
            db.SaveChanges();
            
            return RedirectToAction("Feed");
        }

    }
}

