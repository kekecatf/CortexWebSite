using System;
using System.Linq;
using System.Web.Mvc;
using Larana.Data;
using Larana.Models;

namespace Larana.Controllers
{
    public class RatingController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // POST: Rating/Add
        [HttpPost]
        [Authorize] // User must be logged in to rate
        public ActionResult Add(Rating model)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction("Details", "Dukkan", new { id = model.DukkanId, error = "Invalid rating data" });
            }

            try
            {
                // Get current user
                var username = User.Identity.Name;
                var user = db.Users.FirstOrDefault(u => u.Username == username);
                
                if (user == null)
                {
                    return RedirectToAction("Login", "Account", new { returnUrl = Request.UrlReferrer.ToString() });
                }

                // Set the user ID for the rating model
                model.UserId = user.Id;

                // Check if user has already rated this shop
                var existingRating = db.Ratings.FirstOrDefault(r => r.DukkanId == model.DukkanId && r.UserId == user.Id);
                
                if (existingRating != null)
                {
                    // Update existing rating
                    existingRating.Value = model.Value;
                    existingRating.Comment = model.Comment;
                    existingRating.CreatedAt = DateTime.Now;
                }
                else
                {
                    // Create new rating
                    var rating = new Rating
                    {
                        DukkanId = model.DukkanId,
                        UserId = user.Id,
                        Value = model.Value,
                        Comment = model.Comment,
                        CreatedAt = DateTime.Now
                    };
                    
                    db.Ratings.Add(rating);
                }

                // Update the shop's average rating and rating count
                var shop = db.Dukkans.Find(model.DukkanId);
                
                if (shop != null)
                {
                    // Calculate new average rating - forcing execution with ToList()
                    var ratings = db.Ratings.Where(r => r.DukkanId == model.DukkanId).ToList();
                    shop.RatingCount = ratings.Count;
                    
                    if (shop.RatingCount > 0)
                    {
                        // Calculate average and ensure it's a decimal
                        decimal averageRating = ratings.Average(r => (decimal)r.Value);
                        shop.Rating = Math.Round(averageRating, 1);
                    }
                    else
                    {
                        shop.Rating = 0;
                    }
                    
                    // Save changes to ensure rating is persisted
                    db.SaveChanges();
                }

                return RedirectToAction("Details", "Dukkan", new { id = model.DukkanId, success = "Rating added successfully" });
            }
            catch (Exception ex)
            {
                return RedirectToAction("Details", "Dukkan", new { id = model.DukkanId, error = "Error saving rating: " + ex.Message });
            }
        }
        
        // GET: Rating/Get/5
        [HttpGet]
        public ActionResult Get(int id)
        {
            // Get shop ratings
            var ratings = db.Ratings
                .Where(r => r.DukkanId == id)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new
                {
                    r.Id,
                    r.Value,
                    r.Comment,
                    r.CreatedAt,
                    UserName = r.User.Username
                })
                .ToList();
                
            return Json(ratings, JsonRequestBehavior.AllowGet);
        }
        
        // POST: Rating/Delete/5
        [HttpPost]
        [Authorize]
        public ActionResult Delete(int id)
        {
            var rating = db.Ratings.Find(id);
            
            if (rating == null)
            {
                return Json(new { success = false, message = "Rating not found" });
            }
            
            // Check if user is authorized to delete this rating
            var username = User.Identity.Name;
            var user = db.Users.FirstOrDefault(u => u.Username == username);
            
            if (user == null || (rating.UserId != user.Id && !User.IsInRole("Admin")))
            {
                return Json(new { success = false, message = "Unauthorized" });
            }
            
            try
            {
                db.Ratings.Remove(rating);
                
                // Update shop rating
                var shop = db.Dukkans.Find(rating.DukkanId);
                
                if (shop != null)
                {
                    // Calculate new average rating - forcing execution with ToList()
                    var ratings = db.Ratings.Where(r => r.DukkanId == shop.Id).ToList();
                    shop.RatingCount = ratings.Count;
                    
                    if (shop.RatingCount > 0)
                    {
                        // Calculate average and ensure it's a decimal
                        decimal averageRating = ratings.Average(r => (decimal)r.Value);
                        shop.Rating = Math.Round(averageRating, 1);
                    }
                    else
                    {
                        shop.Rating = 0;
                    }
                }
                
                db.SaveChanges();
                
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
} 