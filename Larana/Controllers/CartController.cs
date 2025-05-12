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
        public ActionResult AddToCart(int productId, int? shopProductId, int? storeId, int quantity = 1)
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

            // First check if we have enough stock
            if (shopProductId.HasValue && shopProductId.Value > 0)
            {
                // This is a shop variant product
                var shopProduct = _context.ShopProducts.Find(shopProductId.Value);
                if (shopProduct == null)
                {
                    return Json(new { success = false, message = "Product not found" });
                }

                if (shopProduct.Stock < quantity)
                {
                    return Json(new { success = false, message = "Not enough stock available" });
                }

                var cartItem = cart.CartItems.FirstOrDefault(ci => 
                    ci.ProductId == productId && 
                    ci.ShopProductId == shopProductId.Value);

                if (cartItem != null)
                {
                    // Update existing cart item
                    if (shopProduct.Stock < cartItem.Quantity + quantity)
                    {
                        return Json(new { success = false, message = "Not enough stock available" });
                    }

                    cartItem.Quantity += quantity;
                    cartItem.UnitPrice = shopProduct.Price; // Use the shop-specific price
                }
                else
                {
                    // Create new cart item with shop product info
                    cartItem = new CartItem
                    {
                        CartId = cart.Id,
                        ProductId = productId,
                        ShopProductId = shopProductId.Value,
                        DukkanId = shopProduct.DukkanId,
                        Quantity = quantity,
                        UnitPrice = shopProduct.Price // Use the shop-specific price
                    };
                    cart.CartItems.Add(cartItem);
                }
            }
            else
            {
                // This is a regular product
                var product = _context.Products.Find(productId);
                if (product == null)
                {
                    return Json(new { success = false, message = "Product not found" });
                }

                if (product.Stock < quantity)
                {
                    return Json(new { success = false, message = "Not enough stock available" });
                }

                var cartItem = cart.CartItems.FirstOrDefault(ci => 
                    ci.ProductId == productId && 
                    ci.ShopProductId == null);

                if (cartItem != null)
                {
                    // Update existing cart item
                    if (product.Stock < cartItem.Quantity + quantity)
                    {
                        return Json(new { success = false, message = "Not enough stock available" });
                    }

                    cartItem.Quantity += quantity;
                    cartItem.UnitPrice = product.Price;
                }
                else
                {
                    // Create new cart item
                    cartItem = new CartItem
                    {
                        CartId = cart.Id,
                        ProductId = productId,
                        DukkanId = product.DukkanId,
                        Quantity = quantity,
                        UnitPrice = product.Price
                    };
                    cart.CartItems.Add(cartItem);
                }
            }

            _context.SaveChanges();
            return Json(new { success = true, message = "Item added to cart" });
        }

        // GET version of AddToCart to handle direct links
        [HttpGet]
        public ActionResult AddToCart(int productId, int? shopProductId, int quantity = 1)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account", new { returnUrl = Request.Url.ToString() });
            }

            var userId = GetCurrentUserId();
            if (userId == -1)
            {
                return RedirectToAction("Login", "Account");
            }

            var cart = _context.Carts
                .Include("CartItems")
                .FirstOrDefault(c => c.UserId == userId);

            if (cart == null)
            {
                cart = new Cart { UserId = userId };
                _context.Carts.Add(cart);
            }

            // First check if we have enough stock
            if (shopProductId.HasValue && shopProductId.Value > 0)
            {
                // This is a shop variant product
                var shopProduct = _context.ShopProducts.Find(shopProductId.Value);
                if (shopProduct == null)
                {
                    TempData["error"] = "Product not found";
                    return RedirectToAction("Index", "Home");
                }

                if (shopProduct.Stock < quantity)
                {
                    TempData["error"] = "Not enough stock available";
                    return RedirectToAction("Details", "Product", new { id = productId });
                }

                var cartItem = cart.CartItems.FirstOrDefault(ci => 
                    ci.ProductId == productId && 
                    ci.ShopProductId == shopProductId.Value);

                if (cartItem != null)
                {
                    // Update existing cart item
                    if (shopProduct.Stock < cartItem.Quantity + quantity)
                    {
                        TempData["error"] = "Not enough stock available";
                        return RedirectToAction("Details", "Product", new { id = productId });
                    }

                    cartItem.Quantity += quantity;
                    cartItem.UnitPrice = shopProduct.Price; // Use the shop-specific price
                }
                else
                {
                    // Create new cart item with shop product info
                    cartItem = new CartItem
                    {
                        CartId = cart.Id,
                        ProductId = productId,
                        ShopProductId = shopProductId.Value,
                        DukkanId = shopProduct.DukkanId,
                        Quantity = quantity,
                        UnitPrice = shopProduct.Price // Use the shop-specific price
                    };
                    cart.CartItems.Add(cartItem);
                }
            }
            else
            {
                // This is a regular product
                var product = _context.Products.Find(productId);
                if (product == null)
                {
                    TempData["error"] = "Product not found";
                    return RedirectToAction("Index", "Home");
                }

                if (product.Stock < quantity)
                {
                    TempData["error"] = "Not enough stock available";
                    return RedirectToAction("Details", "Product", new { id = productId });
                }

                var cartItem = cart.CartItems.FirstOrDefault(ci => 
                    ci.ProductId == productId && 
                    ci.ShopProductId == null);

                if (cartItem != null)
                {
                    // Update existing cart item
                    if (product.Stock < cartItem.Quantity + quantity)
                    {
                        TempData["error"] = "Not enough stock available";
                        return RedirectToAction("Details", "Product", new { id = productId });
                    }

                    cartItem.Quantity += quantity;
                    cartItem.UnitPrice = product.Price;
                }
                else
                {
                    // Create new cart item
                    cartItem = new CartItem
                    {
                        CartId = cart.Id,
                        ProductId = productId,
                        DukkanId = product.DukkanId,
                        Quantity = quantity,
                        UnitPrice = product.Price
                    };
                    cart.CartItems.Add(cartItem);
                }
            }

            _context.SaveChanges();
            TempData["success"] = "Item added to cart";
            return RedirectToAction("Index", "Cart");
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
            
            decimal total = cart.CartItems.Sum(ci => ci.Quantity * ci.UnitPrice);
            
            return Json(new { 
                success = true, 
                itemTotal = (cartItem.Quantity * cartItem.UnitPrice).ToString("C"),
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

            decimal total = cart.CartItems.Sum(ci => ci.Quantity * ci.UnitPrice);
            
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

            try
            {
                // Get cart with all necessary includes to avoid lazy loading
                var cart = _context.Carts
                    .Include(c => c.CartItems.Select(ci => ci.Product))
                    .FirstOrDefault(c => c.UserId == userId);

                if (cart == null || !cart.CartItems.Any())
                {
                    return Json(new { success = false, message = "Your cart is empty." });
                }

                // Verify stock availability in a separate operation
                foreach (var cartItem in cart.CartItems)
                {
                    var product = _context.Products.Find(cartItem.ProductId);
                    if (product == null)
                    {
                        return Json(new { success = false, message = $"Product not found: {cartItem.Product.Name}" });
                    }
                    if (product.Stock < cartItem.Quantity)
                    {
                        return Json(new { success = false, message = $"Insufficient stock for product: {cartItem.Product.Name}" });
                    }
                }

                // Create a new context and transaction for the checkout process
                using (var context = new ApplicationDbContext())
                using (var transaction = context.Database.BeginTransaction())
                {
                    try
                    {
                        // Update product stock and sales
                        foreach (var cartItem in cart.CartItems)
                        {
                            var product = context.Products.Find(cartItem.ProductId);
                            product.Stock -= cartItem.Quantity;
                            product.Sales += cartItem.Quantity;
                            
                            // Determine which shop to update order count for
                            int shopId;
                            
                            if (cartItem.ShopProductId.HasValue)
                            {
                                // If this is a specific shop's variant of a product
                                var shopProduct = context.ShopProducts.Find(cartItem.ShopProductId.Value);
                                if (shopProduct != null)
                                {
                                    // Update the specific shop's stock and order count
                                    shopId = shopProduct.DukkanId;
                                    shopProduct.Stock -= cartItem.Quantity;
                                }
                                else
                                {
                                    // Fallback to original product's shop if ShopProduct not found
                                    shopId = product.DukkanId ?? 0;
                                }
                            }
                            else
                            {
                                // This is a direct purchase from the original shop
                                shopId = product.DukkanId ?? 0;
                            }
                            
                            // Update shop order count if a valid shop was found
                            if (shopId > 0)
                            {
                                var shop = context.Dukkans.Find(shopId);
                                if (shop != null)
                                {
                                    shop.OrderCount += 1;
                                }
                            }
                        }
                        
                        // Create the order
                        var order = new Order
                        {
                            UserId = userId,
                            CartId = cart.Id,
                            OrderDate = DateTime.Now,
                            TotalAmount = cart.CartItems.Sum(ci => ci.Quantity * ci.UnitPrice),
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
                            order.PaymentMethod = "Credit Card";
                        }
                        else
                        {
                            // Set default values for Click & Collect orders
                            order.Address = "In-Store Pickup";
                            order.ShippingCompany = "N/A - Click & Collect";
                            order.PhoneNumber = PhoneNumber ?? "N/A";
                            order.RecipientName = RecipientName ?? "In-Store Pickup";
                            order.PaymentMethod = "Cash on Pickup";
                        }

                        context.Orders.Add(order);
                        context.SaveChanges();

                        // Create order details
                        foreach (var cartItem in cart.CartItems)
                        {
                            var orderDetail = new OrderDetail
                            {
                                OrderId = order.Id,
                                ProductId = cartItem.ProductId,
                                Quantity = cartItem.Quantity,
                                UnitPrice = cartItem.UnitPrice,
                                ShopProductId = cartItem.ShopProductId
                            };
                            
                            context.OrderDetails.Add(orderDetail);
                        }
                        context.SaveChanges();

                        // Clean up the cart items in the original context
                        foreach (var item in _context.CartItems.Where(ci => ci.CartId == cart.Id).ToList())
                        {
                            _context.CartItems.Remove(item);
                        }
                        _context.SaveChanges();

                        // Commit the transaction
                        transaction.Commit();

                        return Json(new { success = true, message = IsClickAndCollect ? 
                            "Order placed successfully. You can pick up your items at the store." : 
                            "Order placed successfully. Your items will be shipped to the provided address." });
                    }
                    catch (Exception ex)
                    {
                        // Roll back the transaction in case of error
                        transaction.Rollback();
                        throw; // Rethrow to be caught by the outer try-catch
                    }
                }
            }
            catch (Exception ex)
            {
                // Get the complete exception chain
                string errorDetails = GetFullExceptionMessage(ex);
                return Json(new { success = false, message = $"Unable to place your order: {errorDetails}" });
            }
        }

        // Helper method to get the complete exception chain
        private string GetFullExceptionMessage(Exception ex)
        {
            var messages = new System.Text.StringBuilder();
            var currentEx = ex;
            
            while (currentEx != null)
            {
                messages.AppendLine(currentEx.Message);
                currentEx = currentEx.InnerException;
            }
            
            return messages.ToString();
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
