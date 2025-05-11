using System;
using System.Web;
using System.Linq;
using System.Web.Mvc;
using Larana.Data;
using Larana.Models;
using System.Data.Entity;

namespace Larana.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly OrdersDbContext _ordersContext;

        public CartController()
        {
            _context = new ApplicationDbContext();
            _ordersContext = new OrdersDbContext();
        }

        // Helper method to get the current user ID
        private int GetCurrentUserId()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return -1;
            }

            var username = User.Identity.Name;
            var user = _context.Users.FirstOrDefault(u => u.Username == username);
            
            if (user == null)
            {
                // Log the issue - username exists in authentication but not in the database
                System.Diagnostics.Debug.WriteLine($"Authentication issue: User '{username}' is authenticated but not found in database");
                // Instead of throwing an exception, return -1 to indicate user not found
                return -1;
            }
            
            return user.Id;
        }

        public ActionResult Index()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }

            var userId = GetCurrentUserId();
            if (userId == -1)
            {
                return RedirectToAction("Login", "Account");
            }

            var cart = _context.Carts
                .Include("CartItems.Product")
                .FirstOrDefault(c => c.UserId == userId);

            if (cart == null)
            {
                cart = new Cart { UserId = userId };
                _context.Carts.Add(cart);
                _context.SaveChanges();
            }

            return View(cart);
        }

        [HttpPost]
        public ActionResult AddToCart(int productId, int quantity = 1)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Json(new { success = false, message = "Please login to add items to cart" });
            }

            var userId = GetCurrentUserId();
            if (userId == -1)
            {
                return Json(new { success = false, message = "Please login to add items to cart" });
            }

            var cart = _context.Carts
                .Include("CartItems")
                .FirstOrDefault(c => c.UserId == userId);

            if (cart == null)
            {
                cart = new Cart { UserId = userId };
                _context.Carts.Add(cart);
            }

            var cartItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == productId);
            if (cartItem != null)
            {
                cartItem.Quantity += quantity;
            }
            else
            {
                cartItem = new CartItem
                {
                    CartId = cart.Id,
                    ProductId = productId,
                    Quantity = quantity
                };
                cart.CartItems.Add(cartItem);
            }

            _context.SaveChanges();
            return Json(new { success = true, message = "Item added to cart" });
        }

        [HttpPost]
        public ActionResult UpdateQuantity(int productId, int quantity)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Json(new { success = false, message = "Please login to update cart" });
            }

            var userId = GetCurrentUserId();
            if (userId == -1)
            {
                return Json(new { success = false, message = "Please login to update cart" });
            }

            var cart = _context.Carts
                .Include(c => c.CartItems)
                .FirstOrDefault(c => c.UserId == userId);

            if (cart == null)
            {
                return Json(new { success = false, message = "Cart not found" });
            }

            var cartItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == productId);

            if (cartItem == null)
            {
                return Json(new { success = false, message = "Cart item not found" });
            }

            if (quantity <= 0)
            {
                _context.CartItems.Remove(cartItem);
            }
            else
            {
                cartItem.Quantity = quantity;
            }

            _context.SaveChanges();
            
            decimal total = cart.CartItems.Sum(ci => ci.Quantity * _context.Products.First(p => p.Id == ci.ProductId).Price);
            
            return Json(new { 
                success = true, 
                itemTotal = (cartItem.Quantity * _context.Products.First(p => p.Id == cartItem.ProductId).Price).ToString("C"),
                cartTotal = total.ToString("C")
            });
        }

        [HttpGet]
        public ActionResult GetCartTotal()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Json(new { success = false, message = "Please login to view cart" }, JsonRequestBehavior.AllowGet);
            }

            var userId = GetCurrentUserId();
            if (userId == -1)
            {
                return Json(new { success = false, message = "Please login to view cart" }, JsonRequestBehavior.AllowGet);
            }

            var cart = _context.Carts
                .Include(c => c.CartItems.Select(ci => ci.Product))
                .FirstOrDefault(c => c.UserId == userId);

            if (cart == null || !cart.CartItems.Any())
            {
                return Json(new { totalFormatted = "$0.00" }, JsonRequestBehavior.AllowGet);
            }

            decimal total = cart.CartItems.Sum(ci => ci.Quantity * ci.Product.Price);
            
            return Json(new { totalFormatted = total.ToString("C") }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult RemoveItem(int cartItemId)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Json(new { success = false, message = "Please login to remove items" });
            }

            var userId = GetCurrentUserId();
            if (userId == -1)
            {
                return Json(new { success = false, message = "Please login to remove items" });
            }

            var cartItem = _context.CartItems
                .FirstOrDefault(ci => ci.Id == cartItemId && ci.Cart.UserId == userId);

            if (cartItem == null)
            {
                return Json(new { success = false, message = "Cart item not found" });
            }

            _context.CartItems.Remove(cartItem);
            _context.SaveChanges();
            return Json(new { success = true });
        }

        [HttpPost]
        [Authorize]
        public ActionResult RemoveFromCart(int productId)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Json(new { success = false, message = "Please login to remove items" });
            }

            var userId = GetCurrentUserId();
            if (userId == -1)
            {
                return Json(new { success = false, message = "Please login to remove items" });
            }

            var cart = _context.Carts
                .Include(c => c.CartItems)
                .FirstOrDefault(c => c.UserId == userId);

            if (cart != null)
            {
                var cartItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == productId);
                if (cartItem != null)
                {
                    _context.CartItems.Remove(cartItem);
                    _context.SaveChanges();
                    return Json(new { success = true, message = "Product removed from cart." });
                }
            }

            return Json(new { success = false, message = "Failed to remove product from cart." });
        }

        [HttpPost]
        [Authorize]
        public ActionResult Checkout(string RecipientName, string ShippingCompany, string Address, string PhoneNumber, string CardInfo, bool IsClickAndCollect = false)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Json(new { success = false, message = "Please login to checkout" });
            }

            var userId = GetCurrentUserId();
            if (userId == -1)
            {
                return Json(new { success = false, message = "Please login to checkout" });
            }

            var cart = _context.Carts
                .Include(c => c.CartItems.Select(ci => ci.Product))
                .FirstOrDefault(c => c.UserId == userId);

            if (cart == null || !cart.CartItems.Any())
            {
                return Json(new { success = false, message = "Your cart is empty." });
            }

            try 
            {
                // Update product stock and sales directly in the database
                foreach (var cartItem in cart.CartItems)
                {
                    var product = _context.Products.FirstOrDefault(p => p.Id == cartItem.ProductId);
                    if (product == null || product.Stock < cartItem.Quantity)
                    {
                        return Json(new { success = false, message = $"Insufficient stock for product: {cartItem.Product.Name}" });
                    }

                    product.Stock -= cartItem.Quantity;
                    product.Sales += cartItem.Quantity;
                }
                _context.SaveChanges();

                // Create the order
                var order = new Order
                {
                    UserId = userId,
                    CartId = cart.Id,
                    OrderDate = DateTime.Now,
                    TotalAmount = cart.CartItems.Sum(ci => ci.Quantity * ci.Product.Price),
                    Status = OrderStatus.Pending,
                    IsClickAndCollect = IsClickAndCollect
                };

                // Set shipping details based on order type
                if (!IsClickAndCollect)
                {
                    order.Address = Address;
                    order.ShippingCompany = ShippingCompany;
                    order.PhoneNumber = PhoneNumber;
                    order.RecipientName = RecipientName;
                }
                else
                {
                    // Set default values for Click & Collect orders
                    order.Address = "In-Store Pickup";
                    order.ShippingCompany = "N/A - Click & Collect";
                    order.PhoneNumber = PhoneNumber ?? "N/A";
                    order.RecipientName = RecipientName ?? "In-Store Pickup";
                }

                // Create order details directly without transaction
                var orderDetails = cart.CartItems.Select(ci => new OrderDetail
                {
                    CartItemId = ci.Id,
                    ProductId = ci.ProductId,
                    Quantity = ci.Quantity,
                    UnitPrice = ci.Product.Price
                }).ToList();
                
                order.OrderDetails = orderDetails;
                
                // Add order to context
                _context.Orders.Add(order);
                _context.SaveChanges();

                // Clean up the cart
                _context.CartItems.RemoveRange(cart.CartItems);
                _context.SaveChanges();

                return Json(new { success = true, message = IsClickAndCollect ? 
                    "Order placed successfully. You can pick up your items at the store." : 
                    "Order placed successfully. Your items will be shipped to the provided address." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _context.Dispose();
                _ordersContext.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
