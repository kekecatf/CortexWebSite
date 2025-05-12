using System;
using System.Linq;
using System.Web.Mvc;
using Larana.Data;
using Larana.Models;
using System.Collections.Generic;
using System.Data.Entity;

namespace Larana.Controllers
{
    [RoutePrefix("Home")]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController()
        {
            _context = new ApplicationDbContext();
        }
        [Route("Index")] //it is domain/Home/Index because of Prefix Routing
        [Route("~/")] //~means domain and it is domain's itself
        public ActionResult Index()
        {
            var newProducts = _context.Products
                                      .Where(p => p.IsActive)
                                      .OrderByDescending(p => p.CreatedAt)
                                      .Include(p => p.Reviews)
                                      .Include(p => p.Dukkan)
                                      .Take(9)
                                      .ToList();

            var bestSellers = _context.Products
                                      .Where(p => p.IsActive)
                                      .OrderByDescending(p => p.Sales)
                                      .Include(p => p.Reviews)
                                      .Include(p => p.Dukkan)
                                      .Take(9)
                                      .ToList();
                                      
            // Get popular shops based on multiple popularity factors - simplify the query to ensure shops appear
            var popularShops = _context.Dukkans
                                      .Where(d => d.IsActive)  // Removed IsPublished condition to show more shops
                                      .Include("Ratings")
                                      .OrderByDescending(d => d.Rating)
                                      .ThenByDescending(d => d.OrderCount)
                                      .ThenByDescending(d => d.ViewCount)
                                      .ThenByDescending(d => d.CreatedAt) 
                                      .Take(8)
                                      .ToList();
                                      
            // If we still don't have any shops, just get any shops available
            if (popularShops.Count == 0)
            {
                popularShops = _context.Dukkans
                                      .OrderByDescending(d => d.CreatedAt)
                                      .Take(8)
                                      .ToList();
            }
            
            // Make sure ratings are up to date for all shops in the list
            foreach (var shop in popularShops)
            {
                // Ensure ratings are loaded
                var ratings = _context.Ratings.Where(r => r.DukkanId == shop.Id).ToList();
                shop.RatingCount = ratings.Count;
                
                if (shop.RatingCount > 0)
                {
                    // Calculate average and ensure it's a decimal
                    decimal averageRating = ratings.Average(r => (decimal)r.Value);
                    shop.Rating = Math.Round(averageRating, 1);
                }
            }
            
            // Save updated ratings
            _context.SaveChanges();

            ViewBag.NewProducts = newProducts;
            ViewBag.BestSellers = bestSellers;
            ViewBag.PopularShops = popularShops;

            return View();
        }

        [Route("About")] //it is domain/Home/About because of Prefix Routing
        [Route("~/About")] //~means domain and it is domain/About
        public ActionResult About()
        {
            return View();
        }
        [Route("Contact")] //it is domain/Home/Contact because of Prefix Routing
        [Route("~/Contact")] //~means domain and it is domain/Contact
        public ActionResult Contact()
        {
            return View();
        }
        [Route("FAQ")] //it is domain/Home/FAQ because of Prefix Routing
        [Route("~/FAQ")] //~means domain and it is domain/FAQ
        public ActionResult FAQ()
        {
            return View();
        }
    }
}