using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Larana.Data;
using Larana.Models;

namespace Larana.Controllers
{
    public class ShopController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ShopController()
        {
            _context = new ApplicationDbContext();
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

        public ActionResult Index(string[] category, string[] brand, decimal? minPrice, decimal? maxPrice, int? shop, bool? clickAndCollect, string sort = "newest")
        {
            var products = _context.Products.Include("Dukkan").AsQueryable();

            // Filter by shop if specified
            if (shop.HasValue)
                products = products.Where(p => p.DukkanId == shop.Value);

            if (category != null && category.Any())
                products = products.Where(p => category.Contains(p.Category));

            if (brand != null && brand.Any())
                products = products.Where(p => brand.Contains(p.Brand));

            if (minPrice.HasValue)
                products = products.Where(p => p.Price >= minPrice.Value);

            if (maxPrice.HasValue)
                products = products.Where(p => p.Price <= maxPrice.Value);
                
            if (clickAndCollect.HasValue && clickAndCollect.Value)
                products = products.Where(p => p.IsClickAndCollect);

            switch (sort)
            {
                case "low-to-high":
                    products = products.OrderBy(p => p.Price);
                    break;
                case "high-to-low":
                    products = products.OrderByDescending(p => p.Price);
                    break;
                case "best-sellers":
                    products = products.OrderByDescending(p => p.Sales);
                    break;
                case "new-products":
                    products = products.OrderByDescending(p => p.Id);
                    break;
                default:
                    products = products.OrderByDescending(p => p.Id);
                    break;
            }

            var initialProducts = products.Take(9).ToList();

            // Get all shops for the shop filter
            ViewBag.Shops = _context.Dukkans.Where(d => d.IsActive).OrderBy(d => d.Name).ToList();
            ViewBag.Categories = _context.Products.Select(p => p.Category).Distinct().ToList();
            ViewBag.Brands = _context.Products.Select(p => p.Brand).Distinct().ToList();
            ViewBag.SelectedCategories = category ?? new string[] { };
            ViewBag.SelectedBrands = brand ?? new string[] { };
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;
            ViewBag.SelectedShop = shop;
            ViewBag.ClickAndCollect = clickAndCollect;
            ViewBag.Sort = sort;

            return View(initialProducts);
        }

        public ActionResult LoadMoreProducts(int page = 1, int pageSize = 12, string[] category = null, string[] brand = null, decimal? minPrice = null, decimal? maxPrice = null, int? shop = null, bool? clickAndCollect = null, string sort = "newest")
        {
            var products = _context.Products
                .Include("Dukkan")
                .Include("Reviews")
                .AsQueryable();

            // Filter by shop if specified
            if (shop.HasValue)
                products = products.Where(p => p.DukkanId == shop.Value);

            if (category != null && category.Any())
                products = products.Where(p => category.Contains(p.Category));

            if (brand != null && brand.Any())
                products = products.Where(p => brand.Contains(p.Brand));

            if (minPrice.HasValue)
                products = products.Where(p => p.Price >= minPrice.Value);

            if (maxPrice.HasValue)
                products = products.Where(p => p.Price <= maxPrice.Value);
            
            if (clickAndCollect.HasValue && clickAndCollect.Value)
                products = products.Where(p => p.IsClickAndCollect);

            switch (sort)
            {
                case "low-to-high":
                    products = products.OrderBy(p => p.Price);
                    break;
                case "high-to-low":
                    products = products.OrderByDescending(p => p.Price);
                    break;
                case "best-sellers":
                    products = products.OrderByDescending(p => p.Sales);
                    break;
                case "new-products":
                    products = products.OrderByDescending(p => p.Id);
                    break;
                default:
                    products = products.OrderByDescending(p => p.Id);
                    break;
            }

            products = products.Skip((page - 1) * pageSize).Take(pageSize);

            return PartialView("_ProductList", products.ToList());
        }

        public ActionResult Details(int id)
        {
            var product = _context.Products
                .Include("Dukkan")
                .Include("Reviews.User")
                .SingleOrDefault(p => p.Id == id);

            if (product == null)
            {
                return HttpNotFound();
            }

            // Increment view count
            product.ViewCount += 1;
            _context.SaveChanges();

            // Fetch product sellers/prices from ShopProducts
            var shopProducts = _context.ShopProducts
                .Include("Dukkan")
                .Where(sp => sp.ProductId == id && sp.DukkanId != product.DukkanId && sp.Dukkan.IsActive)
                .ToList();
            
            ViewBag.ShopProducts = shopProducts;
            
            // Always initialize empty lists for the ViewBag properties to avoid null references
            ViewBag.OwnerShops = new List<Larana.Models.Dukkan>();
            
            // If user is authenticated, check if they are a seller and get their shops
            if (User.Identity.IsAuthenticated)
            {
                // Debug user roles 
                // var roles = System.Web.Security.Roles.GetRolesForUser(User.Identity.Name);
                // ViewBag.UserRoles = string.Join(", ", roles);
                ViewBag.UserRoles = User.IsInRole("Seller") ? "Seller" : "Not a seller";
                
                bool isSeller = User.IsInRole("Seller");
                ViewBag.IsSeller = isSeller;
                
                if (isSeller)
                {
                    var userId = GetCurrentUserId();
                    var userShops = _context.Dukkans
                        .Where(d => d.OwnerId == userId && d.IsActive)
                        .ToList();
                    
                    // Filter out shops that already sell this product
                    var existingShopProductIds = _context.ShopProducts
                        .Where(sp => sp.ProductId == id)
                        .Select(sp => sp.DukkanId)
                        .ToList();
                    
                    if (product.DukkanId.HasValue)
                    {
                        existingShopProductIds.Add(product.DukkanId.Value);
                    }
                    
                    userShops = userShops
                        .Where(s => !existingShopProductIds.Contains(s.Id))
                        .ToList();
                    
                    ViewBag.OwnerShops = userShops;
                    
                    // Check if user is the owner of this product's shop
                    bool isProductShopOwner = false;
                    if (product.DukkanId.HasValue)
                    {
                        var productShop = _context.Dukkans.Find(product.DukkanId.Value);
                        isProductShopOwner = productShop != null && productShop.OwnerId == userId;
                    }
                    
                    // Set a flag to control whether to show the "Sell in your shop" option
                    ViewBag.CanSellInOwnShop = !isProductShopOwner && userShops.Count > 0;
                }
            }

            // Get sorted reviews and add to ViewBag
            if (product.Reviews != null && product.Reviews.Count > 0)
            {
                var sortedReviews = product.Reviews.OrderByDescending(r => r.CreatedAt).ToList();
                
                // Get purchase information for each review
                var reviewUserIds = sortedReviews.Select(r => r.UserId).Distinct().ToList();
                
                // Load all relevant order details in a single query for better performance
                var orderDetails = _context.OrderDetails
                    .Include("ShopProduct.Dukkan")
                    .Include("Order") // Explicitly include Order to ensure it's loaded
                    .Where(od => od.ProductId == id && od.Order != null) // Filter out null Orders first
                    .ToList()
                    .Where(od => reviewUserIds.Contains(od.Order.UserId)) // Further filtering after loading
                    .ToList();
                
                // Create a Dictionary for purchase info - add safety checks
                Dictionary<int, OrderDetail> purchaseInfoByUser = new Dictionary<int, OrderDetail>();
                
                foreach (var userId in reviewUserIds)
                {
                    // Find the most recent order for this user and product
                    var userOrders = orderDetails
                        .Where(od => od.Order.UserId == userId)
                        .OrderByDescending(od => od.Order.OrderDate);
                        
                    if (userOrders.Any())
                    {
                        purchaseInfoByUser[userId] = userOrders.First();
                    }
                }
                
                ViewBag.PurchaseInfoByUser = purchaseInfoByUser;
                ViewBag.SortedReviews = sortedReviews;
            }
            else
            {
                ViewBag.SortedReviews = new List<Larana.Models.Review>();
                ViewBag.PurchaseInfoByUser = new Dictionary<int, Larana.Models.OrderDetail>();
            }

            var relatedProducts = _context.Products
                .Include("Dukkan")
                .Where(p => p.Category == product.Category && p.Id != product.Id)
                .Take(4)
                .ToList();

            ViewBag.RelatedProducts = relatedProducts;

            // Check if user has purchased this product
            bool userCanReview = false;
            bool hasReviewed = false;
            
            if (User.Identity.IsAuthenticated)
            {
                int userId = GetCurrentUserId();
                
                // Check if user has already reviewed this product
                hasReviewed = _context.Reviews.Any(r => r.ProductId == id && r.UserId == userId);
                
                // Check if user has purchased this product
                userCanReview = _context.OrderDetails
                    .Where(od => od.ProductId == id)
                    .Any(od => od.Order.UserId == userId);
            }
            
            ViewBag.UserCanReview = userCanReview && !hasReviewed;
            ViewBag.HasReviewed = hasReviewed;

            return View(product);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult AddReview(int productId, int rating, string comment)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction("Details", new { id = productId });
            }

            int userId = GetCurrentUserId();
            
            // Check if user has already reviewed this product
            bool hasReviewed = _context.Reviews.Any(r => r.ProductId == productId && r.UserId == userId);
            if (hasReviewed)
            {
                TempData["ErrorMessage"] = "You have already reviewed this product.";
                return RedirectToAction("Details", new { id = productId });
            }
            
            // Check if user has purchased this product
            bool hasPurchased = _context.OrderDetails
                .Where(od => od.ProductId == productId)
                .Any(od => od.Order.UserId == userId);
                
            if (!hasPurchased)
            {
                TempData["ErrorMessage"] = "You can only review products you have purchased.";
                return RedirectToAction("Details", new { id = productId });
            }
            
            var review = new Larana.Models.Review
            {
                ProductId = productId,
                UserId = userId,
                Rating = rating,
                Comment = comment,
                CreatedAt = DateTime.Now
            };
            
            _context.Reviews.Add(review);
            _context.SaveChanges();
            
            TempData["SuccessMessage"] = "Your review has been added successfully.";
            return RedirectToAction("Details", new { id = productId });
        }

        [HttpPost]
        [Authorize]
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

            var cart = _context.Carts.FirstOrDefault(c => c.UserId == userId);
            if (cart == null)
            {
                cart = new Cart { UserId = userId, CreatedAt = DateTime.Now };
                _context.Carts.Add(cart);
                _context.SaveChanges();
            }

            var cartItem = _context.CartItems
                .FirstOrDefault(ci => ci.CartId == cart.Id && ci.ProductId == productId);

            if (cartItem == null)
            {
                cartItem = new CartItem
                {
                    CartId = cart.Id,
                    ProductId = productId,
                    Quantity = quantity
                };
                _context.CartItems.Add(cartItem);
            }
            else
            {
                cartItem.Quantity += quantity;
            }

            _context.SaveChanges();
            return Json(new { success = true, message = "Product added to cart successfully." });
        }

        public ActionResult SearchProducts(string query)
        {
            var products = string.IsNullOrWhiteSpace(query)
                ? _context.Products.Include("Dukkan").Include("Reviews").ToList()
                : _context.Products
                    .Include("Dukkan")
                    .Include("Reviews")
                    .Where(p => p.Name.Contains(query) || p.Category.Contains(query) || p.Brand.Contains(query))
                    .ToList();

            var productData = products.Select(p => new {
                Id = p.Id,
                Name = p.Name,
                Price = string.Format("{0:N}", p.Price),
                PhotoPath = p.PhotoPath,
                DukkanName = p.Dukkan != null ? p.Dukkan.Name : null,
                IsClickAndCollect = p.IsClickAndCollect
            }).ToList();

            return Json(productData, JsonRequestBehavior.AllowGet);
        }

        // New Search method to handle combined product and shop searches from homepage
        public ActionResult Search(string q, string searchType = "products")
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                // If no search query, redirect to respective index pages
                return searchType == "shops" 
                    ? RedirectToAction("Index", "Dukkan") 
                    : RedirectToAction("Index");
            }
            
            ViewBag.SearchQuery = q;
            ViewBag.SearchType = searchType;
            
            if (searchType == "shops")
            {
                // Search for shops
                var shops = _context.Dukkans
                    .Where(s => s.Name.Contains(q) || 
                               (s.Description != null && s.Description.Contains(q)) ||
                               (s.Location != null && s.Location.Contains(q)))
                    .ToList();
                    
                ViewBag.SearchResults = shops;
                ViewBag.ResultCount = shops.Count;
                return View("SearchResults");
            }
            else
            {
                // Search for products (default)
                var products = _context.Products
                    .Include("Dukkan")
                    .Include("Reviews")
                    .Where(p => p.Name.Contains(q) || 
                               (p.Description != null && p.Description.Contains(q)) ||
                                p.Category.Contains(q) || 
                                p.Brand.Contains(q))
                    .OrderByDescending(p => p.Id)
                    .ToList();
                    
                ViewBag.SearchResults = products;
                ViewBag.ResultCount = products.Count;
                return View("SearchResults");
            }
        }

        [Authorize]
        public ActionResult MyShops()
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

            var shops = _context.Dukkans
                .Where(d => d.OwnerId == userId)
                .ToList();

            return View(shops);
        }

        [Authorize]
        public ActionResult CreateShop()
        {
            // Check if user already has a shop
            var userId = GetCurrentUserId();
            if (userId == -1)
            {
                return RedirectToAction("Login", "Account");
            }

            var existingShop = _context.Dukkans.FirstOrDefault(d => d.OwnerId == userId);
            
            if (existingShop != null)
            {
                TempData["Message"] = "You already have a shop!";
                return RedirectToAction("Details", "Dukkan", new { id = existingShop.Id });
            }

            return View();
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult CreateShop(Dukkan dukkan)
        {
            if (ModelState.IsValid)
            {
                var userId = GetCurrentUserId();
                if (userId == -1)
                {
                    return RedirectToAction("Login", "Account");
                }
                
                // Check if user already has a shop
                var existingShop = _context.Dukkans.FirstOrDefault(d => d.OwnerId == userId);
                if (existingShop != null)
                {
                    TempData["Message"] = "You already have a shop!";
                    return RedirectToAction("Details", "Dukkan", new { id = existingShop.Id });
                }

                dukkan.OwnerId = userId;
                dukkan.CreatedAt = DateTime.Now;
                dukkan.IsActive = true;

                _context.Dukkans.Add(dukkan);
                _context.SaveChanges();

                TempData["Message"] = "Your shop has been created successfully!";
                return RedirectToAction("Details", "Dukkan", new { id = dukkan.Id });
            }

            return View(dukkan);
        }

        [Authorize]
        public ActionResult Edit(int id)
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

            var shop = _context.Dukkans.FirstOrDefault(d => d.Id == id && d.OwnerId == userId);
            if (shop == null)
            {
                return HttpNotFound();
            }

            return View(shop);
        }

        [HttpPost]
        [Authorize]
        public ActionResult Edit(Dukkan shop)
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

            var existingShop = _context.Dukkans.FirstOrDefault(d => d.Id == shop.Id && d.OwnerId == userId);
            if (existingShop == null)
            {
                return HttpNotFound();
            }

            // Update shop properties
            existingShop.Name = shop.Name;
            existingShop.Description = shop.Description;
            existingShop.IsActive = shop.IsActive;

            _context.SaveChanges();
            TempData["Message"] = "Shop updated successfully!";
            return RedirectToAction("Details", "Dukkan", new { id = shop.Id });
        }

        // New action to handle adding to cart from other sellers
        [HttpPost]
        [Authorize]
        public ActionResult AddToCartFromSeller(int shopProductId, int quantity = 1)
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

            // Get the ShopProduct record
            var shopProduct = _context.ShopProducts
                .Include("Product")
                .FirstOrDefault(sp => sp.Id == shopProductId);
                
            if (shopProduct == null)
            {
                return Json(new { success = false, message = "Product not found" });
            }
            
            if (shopProduct.Stock < quantity)
            {
                return Json(new { success = false, message = "Not enough stock available" });
            }

            var cart = _context.Carts.FirstOrDefault(c => c.UserId == userId);
            if (cart == null)
            {
                cart = new Cart { UserId = userId, CreatedAt = DateTime.Now };
                _context.Carts.Add(cart);
                _context.SaveChanges();
            }

            // Create special cart item that references the shop product
            var cartItem = _context.CartItems
                .FirstOrDefault(ci => ci.CartId == cart.Id && ci.ProductId == shopProduct.ProductId && ci.ShopProductId == shopProductId);

            if (cartItem == null)
            {
                cartItem = new CartItem
                {
                    CartId = cart.Id,
                    ProductId = shopProduct.ProductId,
                    ShopProductId = shopProductId, // Store the specific shop product ID
                    Quantity = quantity,
                    UnitPrice = shopProduct.Price // Use the shop's specific price
                };
                _context.CartItems.Add(cartItem);
            }
            else
            {
                cartItem.Quantity += quantity;
            }

            // Update ShopProduct stock
            shopProduct.Stock -= quantity;

            _context.SaveChanges();
            return Json(new { success = true, message = "Product added to cart successfully." });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _context.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}