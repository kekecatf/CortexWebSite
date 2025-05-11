using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Larana.Data;
using Larana.Models;
using Hangfire;

namespace Larana.Controllers
{
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext db;

        public OrderController(ApplicationDbContext db)
        {
            this.db = db;
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult Checkout(OrderViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var username = User.Identity.Name;
            var user = db.Users.FirstOrDefault(u => u.Username == username);
            
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }
            
            var cart = db.Carts
                .Include("CartItems")
                .Include("CartItems.Product")
                .FirstOrDefault(c => c.UserId == user.Id);
                
            if (cart == null || !cart.CartItems.Any())
            {
                TempData["Error"] = "Your cart is empty";
                return RedirectToAction("Index", "Cart");
            }
            
            // Create the order
            var order = new Order
            {
                UserId = user.Id,
                CartId = cart.Id,
                OrderDate = DateTime.Now,
                Status = OrderStatus.Pending,
                ShippingAddress = model.ShippingAddress,
                PaymentMethod = model.PaymentMethod,
                PaymentStatus = "Pending"
            };
            
            db.Orders.Add(order);
            db.SaveChanges();
            
            // Create order details
            foreach (var item in cart.CartItems)
            {
                var orderDetail = new OrderDetail
                {
                    OrderId = order.Id,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.Product.Price
                };
                
                db.OrderDetails.Add(orderDetail);
                
                // Update product stock
                var product = db.Products.Find(item.ProductId);
                if (product != null)
                {
                    product.Stock -= item.Quantity;
                    product.Sales += item.Quantity;
                    product.OrderCount += 1;
                }
                
                // Update shop order count if product belongs to a shop
                if (product.DukkanId.HasValue)
                {
                    var shop = db.Dukkans.Find(product.DukkanId.Value);
                    if (shop != null)
                    {
                        shop.OrderCount += 1;
                    }
                }
            }
            
            // Clear the cart
            db.CartItems.RemoveRange(cart.CartItems);
            db.SaveChanges();
            
            // Queue a background job to recalculate ratings
            Hangfire.BackgroundJob.Enqueue<Services.PopularityService>(x => x.RecalculateShopRatings());
            
            return RedirectToAction("OrderConfirmation", new { orderId = order.Id });
        }
    }
} 