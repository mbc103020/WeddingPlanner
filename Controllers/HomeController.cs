using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Http; 
using Microsoft.EntityFrameworkCore; 
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity; 
using Microsoft.Extensions.Logging;
using WeddingPlanner.Models;

namespace WeddingPlanner.Controllers
{
    public class HomeController : Controller
    {
        private MyContext _context;
     
        // here we can "inject" our context service into the constructor
        public HomeController(MyContext context)
        {
            _context = context;
        }
        private User LoggedInUser
        {
            get 
                {
                   return _context.Users.FirstOrDefault(user => user.UserId == HttpContext.Session.GetInt32("userId")); 
                }
        }

        [HttpGet("/")]
        public IActionResult Index()
        {
            return View(); 
        }

        [HttpPost("/register")]
        public IActionResult Register(User newUser)
        {
            
            if(ModelState.IsValid)
            {
                if(_context.Users.Any(u=> u.Email == newUser.Email))
                {
                    ModelState.AddModelError("Email", "Email already in use!"); 
                    return View("Index"); 
                }
                PasswordHasher<User> hasher = new PasswordHasher<User>(); 
                newUser.Password = hasher.HashPassword(newUser, newUser.Password); 

                var addNewUser = _context.Users.Add(newUser); 
                _context.SaveChanges(); 

                HttpContext.Session.SetInt32("userId", newUser.UserId); 

                return RedirectToAction("Dashboard"); 
            }
            return View("Index"); 

        }
        [HttpPost("/loginuser")]
        public IActionResult Login(User userLogin)
        {
            
            var userInDatabase = _context.Users.FirstOrDefault(u=> u.Email == userLogin.Email); 
            if(userInDatabase == null)
            {
                TempData["error_message"] = "Invalid Login";
                // ModelState.AddModelError("Email", "Invalid email/password"); 
                return RedirectToAction("Index"); 
            }

            var hasher = new PasswordHasher<User>(); 
            var result = hasher.VerifyHashedPassword(userLogin, userInDatabase.Password, userLogin.Password); 

            if(result == 0) //invalid password
            {
                TempData["error_message"] = "Invalid Login";
                // ModelState.AddModelError("Password", "Invalid email/password"); 
                return RedirectToAction("Index"); 
            }
            //log user into session
            TempData["success_message"] = $"Welcome, {userInDatabase.FirstName}!";
            HttpContext.Session.SetInt32("userId", userLogin.UserId); 
            return RedirectToAction("Dashboard", userInDatabase); 
        }

        [HttpGet("/logout")]
        public IActionResult Logout()
        {
            
            TempData["success_message"] = "Thank you for visiting!";
            HttpContext.Session.Clear();
            return RedirectToAction("Index");
        }

        [HttpGet("/dashboard/{userid}")]
        public IActionResult Dashboard(int userId)
        {
            if(HttpContext.Session.GetInt32("userId") == null)
            {
                return View("Index"); 
            }
            var userInDb = _context.Users.FirstOrDefault(b => b.UserId == userId);
            ViewBag.User = userInDb;
            // ViewBag.UserId = LoggedInUser.UserId; 
            
          

            var weddings = _context.Weddings
            .Include(wedd => wedd.Responses)
            .OrderBy(wedd=> wedd.Date); 

            var responsed = weddings.Where(w => w.Responses.Any(r => r.UserId == 1));

            // ViewBag.Weddings = _context.Weddings; 
            return View(weddings.ToList()); 

        }

        [HttpGet("/viewwedding/{weddingId}")]
        public IActionResult ViewWedding(int weddingId)
        {
             if(HttpContext.Session.GetInt32("userId") == null)
            {
                return View("Index"); 
            } 

            Wedding model = _context.Weddings
            .Include(wedd => wedd.Responses)
            .ThenInclude(resp=>resp.Guest)
            .FirstOrDefault(wedd => wedd.WeddingId == weddingId); 

            return View(model); 

        }

        [HttpGet("/planwedding/{userId}")]
        public IActionResult PlanWedding()
        {
            if(HttpContext.Session.GetInt32("userId") == null)
            {
                return View("Index"); 
            }

            return View(); 
        }

        [HttpPost("/planwedding/{userId}")]
        public IActionResult AddNewWedding(Wedding newWedding)
        {
             if(HttpContext.Session.GetInt32("userId") == null)
            {
                return View("Index"); 
            }
            else if(ModelState.IsValid)
            {
                _context.Weddings.Add(newWedding); 
                _context.SaveChanges();
                return RedirectToAction("Dashboard"); 
            }
            return View("PlanWedding"); 

        }

        [HttpGet("/remove/{weddingId}/{userId}")]
        public IActionResult Remove(int weddingId, int userId)
        {
            if(HttpContext.Session.GetInt32("userId") == null)
            {
                return View("Index"); 
            }
            var userInDb = _context.Users.FirstOrDefault(usr => usr.UserId == userId);

            // query for wedding
            Wedding toDelete = _context.Weddings.SingleOrDefault(wedd => wedd.WeddingId == weddingId && wedd.UserId == userInDb.UserId);
              
            
            // if null, redirect
            if(toDelete == null)
            {
                 return RedirectToAction("Dashboard");

            }
            
            _context.Weddings.Remove(toDelete);
            _context.SaveChanges();
            return RedirectToAction("Dashboard");
        }

        [HttpGet("/rsvp/{weddingId}/{userId}/{status}")]
        public IActionResult RSVP(int weddingId, int userId, string status)
        {
              if(HttpContext.Session.GetInt32("userId") == null)
            {
                return View("Index"); 
            }

            if(!_context.Weddings.Any(wedd=> wedd.WeddingId == weddingId))
            {
                return View("Dashboard"); 
            }

            if(status == "add")
            {
                AddRsvp(weddingId, userId); 
            }
            else if(status == "remove")
            {
                UnRsvp(weddingId, userId); 

            }

            return RedirectToAction("Dashboard"); 

        }

        private void AddRsvp(int weddingId, int userId)
        {
            
            
            Response newRSVP = new Response() 
            {
                
                WeddingId = weddingId, 
                UserId = userId
            }; 

            _context.Responses.Add(newRSVP); 
            _context.SaveChanges(); 
        }

        private void UnRsvp(int weddingId, int userId)
        {
             var userInDb = _context.Users.FirstOrDefault(usr => usr.UserId == userId);
             Response rsvp = _context.Responses.FirstOrDefault(resp => resp.UserId == userInDb.UserId && resp.WeddingId == weddingId);

            if(rsvp != null)
            {
                _context.Responses.Remove(rsvp);
                _context.SaveChanges(); 
            }
        }

    
        
    }
}
