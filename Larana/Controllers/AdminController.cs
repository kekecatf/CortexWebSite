using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Larana.Models;
using Larana.Data;

namespace Larana.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private Larana.Data.DbAccount db = new Larana.Data.DbAccount();
        private OrdersDbContext ordersDb = new OrdersDbContext();
        private ApplicationDbContext productDb = new ApplicationDbContext();

        public ActionResult Index()
        {
            ViewBag.TotalUsers = db.Users.Count();
            ViewBag.TotalProducts = productDb.Products.Count();
            ViewBag.TotalOrders = ordersDb.Orders.Count();
            ViewBag.TotalRevenue = ordersDb.Orders.Any() ? 
                ordersDb.Orders.Sum(o => o.TotalAmount ?? 0) : 0;

            return View();
        }

        public ActionResult CreateUser()
        {
            return View();
        }

        [HttpPost]
        public ActionResult CreateUser(User user)
        {
            if (ModelState.IsValid)
            {
                user.Role = UserRole.Admin;
                user.Roles = "Admin";
                db.Users.Add(user);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(user);
        }
    }
}

