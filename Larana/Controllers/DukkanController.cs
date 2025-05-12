using System;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Larana.Models;
using Larana.Data;
using System.Collections.Generic;
using System.Data.Entity;

namespace Larana.Controllers
{
    [RoutePrefix("Dukkan")]
    public class DukkanController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // Helper method to check if current user is the shop owner
        private bool IsCurrentUserShopOwner(int dukkanId)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return false;
            }
            
            var username = User.Identity.Name;
            var user = db.Users.FirstOrDefault(u => u.Username == username);
            
            if (user == null)
            {
                return false;
            }
            
            var dukkan = db.Dukkans.Find(dukkanId);
            
            if (dukkan == null)
            {
                return false;
            }
            
            // Return true if user is owner or admin
            return dukkan.OwnerId == user.Id || User.IsInRole("Admin");
        }

        // GET: Dukkan
        [AllowAnonymous]
        [Route("Index")]
        [Route("~/Dukkan")]
        public ActionResult Index(string search = "", string category = "", string location = "", string sort = "newest", int page = 1, int pageSize = 12)
        {
            var dukkans = db.Dukkans.Where(d => d.IsActive).AsQueryable();
            
            // Apply search filter
            if (!string.IsNullOrEmpty(search))
            {
                dukkans = dukkans.Where(d => d.Name.Contains(search) || d.Description.Contains(search));
            }
            
            // Apply category filter
            if (!string.IsNullOrEmpty(category))
            {
                dukkans = dukkans.Where(d => d.Category == category);
            }
            
            // Apply location filter
            if (!string.IsNullOrEmpty(location))
            {
                dukkans = dukkans.Where(d => d.Location.Contains(location));
            }
            
            // Apply sorting
            switch (sort)
            {
                case "newest":
                    dukkans = dukkans.OrderByDescending(d => d.CreatedAt);
                    break;
                case "popular":
                    dukkans = dukkans.OrderByDescending(d => d.Products.Count());
                    break;
                case "rating":
                    dukkans = dukkans.OrderByDescending(d => d.Rating);
                    break;
                default:
                    dukkans = dukkans.OrderByDescending(d => d.CreatedAt);
                    break;
            }
            
            // Calculate pagination
            var totalItems = dukkans.Count();
            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            
            // Apply pagination
            var paginatedDukkans = dukkans
                .Include("Ratings")
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
                
            // Make sure ratings are up to date for all shops in the list
            foreach (var dukkan in paginatedDukkans)
            {
                // Ensure ratings are loaded
                var ratings = db.Ratings.Where(r => r.DukkanId == dukkan.Id).ToList();
                dukkan.RatingCount = ratings.Count;
                
                if (dukkan.RatingCount > 0)
                {
                    // Calculate average and ensure it's a decimal
                    decimal averageRating = ratings.Average(r => (decimal)r.Value);
                    dukkan.Rating = Math.Round(averageRating, 1);
                }
            }
            
            // Save updated ratings
            db.SaveChanges();
            
            // Get all categories and locations for filters
            ViewBag.Categories = db.Dukkans
                .Where(d => d.IsActive && !string.IsNullOrEmpty(d.Category))
                .Select(d => d.Category)
                .Distinct()
                .ToList();
                
            ViewBag.Locations = db.Dukkans
                .Where(d => d.IsActive && !string.IsNullOrEmpty(d.Location))
                .Select(d => d.Location)
                .Distinct()
                .ToList();
            
            // Set pagination values
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.PageSize = pageSize;
            
            // Set filter values
            ViewBag.Search = search;
            ViewBag.Category = category;
            ViewBag.Location = location;
            ViewBag.Sort = sort;
            
            return View(paginatedDukkans);
        }
        
        [HttpGet]
        [AllowAnonymous]
        [Route("LoadMore")]
        public ActionResult LoadMoreShops(string search = "", string category = "", string location = "", string sort = "newest", int page = 1, int pageSize = 12)
        {
            var dukkans = db.Dukkans.Where(d => d.IsActive).AsQueryable();
            
            // Apply search filter
            if (!string.IsNullOrEmpty(search))
            {
                dukkans = dukkans.Where(d => d.Name.Contains(search) || d.Description.Contains(search));
            }
            
            // Apply category filter
            if (!string.IsNullOrEmpty(category))
            {
                dukkans = dukkans.Where(d => d.Category == category);
            }
            
            // Apply location filter
            if (!string.IsNullOrEmpty(location))
            {
                dukkans = dukkans.Where(d => d.Location.Contains(location));
            }
            
            // Apply sorting
            switch (sort)
            {
                case "newest":
                    dukkans = dukkans.OrderByDescending(d => d.CreatedAt);
                    break;
                case "popular":
                    dukkans = dukkans.OrderByDescending(d => d.Products.Count());
                    break;
                case "rating":
                    dukkans = dukkans.OrderByDescending(d => d.Rating);
                    break;
                default:
                    dukkans = dukkans.OrderByDescending(d => d.CreatedAt);
                    break;
            }
            
            // Apply pagination
            var paginatedDukkans = dukkans
                .Include("Ratings")
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            
            // Make sure ratings are up to date for all shops in the list
            foreach (var dukkan in paginatedDukkans)
            {
                // Ensure ratings are loaded
                var ratings = db.Ratings.Where(r => r.DukkanId == dukkan.Id).ToList();
                dukkan.RatingCount = ratings.Count;
                
                if (dukkan.RatingCount > 0)
                {
                    // Calculate average and ensure it's a decimal
                    decimal averageRating = ratings.Average(r => (decimal)r.Value);
                    dukkan.Rating = Math.Round(averageRating, 1);
                }
            }
            
            // Save updated ratings
            db.SaveChanges();
            
            return PartialView("_ShopList", paginatedDukkans);
        }

        // GET: Dukkan/ProductRedirect/5
        // This action is used to redirect from Product/Create to Dukkan/AddProduct
        [Authorize(Roles = "Admin,Seller")]
        public ActionResult ProductRedirect(int id)
        {
            return RedirectToAction("AddProduct", new { id = id });
        }

        // GET: Dukkan/Details/5
        [AllowAnonymous]
        [Route("Details/{id}")]
        public ActionResult Details(int id)
        {
            var dukkan = db.Dukkans
                .Include("Products")
                .Include("Ratings")
                .FirstOrDefault(d => d.Id == id);

            if (dukkan == null)
            {
                return HttpNotFound();
            }

            // Recalculate average rating to ensure it's up to date
            var ratings = db.Ratings.Where(r => r.DukkanId == id).ToList();
            dukkan.RatingCount = ratings.Count;
            
            if (dukkan.RatingCount > 0)
            {
                // Calculate average and ensure it's a decimal
                decimal averageRating = ratings.Average(r => (decimal)r.Value);
                dukkan.Rating = Math.Round(averageRating, 1);
                db.SaveChanges();
            }

            // Get products from ShopProducts table (added from other shops)
            var shopProducts = db.ShopProducts
                .Include("Product")
                .Where(sp => sp.DukkanId == id)
                .ToList();
            
            // Add these to ViewBag to display in view
            ViewBag.ShopProducts = shopProducts;

            // Increment view count
            dukkan.ViewCount += 1;
            db.SaveChanges();

            if (User.Identity.IsAuthenticated)
            {
                var username = User.Identity.Name;
                var user = db.Users.FirstOrDefault(u => u.Username == username);
                ViewBag.CurrentUserId = user?.Id;
                ViewBag.IsOwner = user?.Id == dukkan.OwnerId;
            }

            return View(dukkan);
        }

        // GET: Dukkan/Create
        [Authorize(Roles = "Seller")]
        [Route("Create")]
        public ActionResult Create()
        {
            var username = User.Identity.Name;
            var user = db.Users.FirstOrDefault(u => u.Username == username);

            if (user == null)
            {
                return HttpNotFound("User not found.");
            }

            // Check if user already has a store
            var existingStore = db.Dukkans.FirstOrDefault(d => d.OwnerId == user.Id);
            if (existingStore != null)
            {
                TempData["Message"] = "You already have a store!";
                return RedirectToAction("Details", new { id = existingStore.Id });
            }

            return View();
        }

        // POST: Dukkan/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Seller")]
        [Route("Create")]
        public ActionResult Create(Dukkan dukkan, HttpPostedFileBase shopLogo, HttpPostedFileBase coverImage, bool saveDraft = false)
        {
            if (ModelState.IsValid)
            {
                var username = User.Identity.Name;
                var user = db.Users.FirstOrDefault(u => u.Username == username);
                
                if (user == null)
                {
                    return HttpNotFound("User not found.");
                }

                // Check if user already has a store
                var existingStore = db.Dukkans.FirstOrDefault(d => d.OwnerId == user.Id);
                if (existingStore != null)
                {
                    TempData["Message"] = "You already have a store!";
                    return RedirectToAction("Details", new { id = existingStore.Id });
                }

                // Handle logo upload
                if (shopLogo != null && shopLogo.ContentLength > 0)
                {
                    var fileName = Path.GetFileName(shopLogo.FileName);
                    var extension = Path.GetExtension(fileName);
                    var newFileName = Guid.NewGuid().ToString() + extension;
                    var path = Path.Combine(Server.MapPath("~/Content/Images/Shops"), newFileName);
                    
                    // Create directory if it doesn't exist
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    
                    // Save the file
                    shopLogo.SaveAs(path);
                    dukkan.LogoPath = "/Content/Images/Shops/" + newFileName;
                }
                
                // Handle cover image upload
                if (coverImage != null && coverImage.ContentLength > 0)
                {
                    var fileName = Path.GetFileName(coverImage.FileName);
                    var extension = Path.GetExtension(fileName);
                    var newFileName = Guid.NewGuid().ToString() + extension;
                    var path = Path.Combine(Server.MapPath("~/Content/Images/Shops/Covers"), newFileName);
                    
                    // Create directory if it doesn't exist
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    
                    // Save the file
                    coverImage.SaveAs(path);
                    dukkan.CoverImagePath = "/Content/Images/Shops/Covers/" + newFileName;
                }

                dukkan.CreatedAt = DateTime.Now;
                dukkan.OwnerId = user.Id;
                dukkan.IsActive = true;
                dukkan.Rating = 0; // Initialize rating to 0
                dukkan.IsPublished = true; // Always publish by default, override the saveDraft option
                
                db.Dukkans.Add(dukkan);
                db.SaveChanges();

                TempData["Message"] = saveDraft ? "Your shop has been saved as draft." : "Your shop has been created and published!";
                return RedirectToAction("Details", new { id = dukkan.Id });
            }
            return View(dukkan);
        }

        // GET: Dukkan/Edit/5
        [Authorize]
        [Route("Edit/{id}")]
        public ActionResult Edit(int id)
        {
            var username = User.Identity.Name;
            var user = db.Users.FirstOrDefault(u => u.Username == username);
            var dukkan = db.Dukkans.Find(id);

            if (dukkan == null)
            {
                return HttpNotFound();
            }

            // Check if user is the owner or an admin
            if (user.UserType != UserType.Admin && dukkan.OwnerId != user.Id)
            {
                return new HttpStatusCodeResult(403, "You don't have permission to edit this store.");
            }

            return View(dukkan);
        }

        // POST: Dukkan/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        [Route("Edit/{id}")]
        public ActionResult Edit(Dukkan dukkan, HttpPostedFileBase shopLogo, HttpPostedFileBase coverImage, bool saveDraft = false)
        {
            if (ModelState.IsValid)
            {
                var username = User.Identity.Name;
                var user = db.Users.FirstOrDefault(u => u.Username == username);
                var existingDukkan = db.Dukkans.Find(dukkan.Id);

                if (existingDukkan == null)
                {
                    return HttpNotFound();
                }

                // Check if user is the owner or an admin
                if (user.UserType != UserType.Admin && existingDukkan.OwnerId != user.Id)
                {
                    return new HttpStatusCodeResult(403, "You don't have permission to edit this store.");
                }

                // Handle logo upload
                if (shopLogo != null && shopLogo.ContentLength > 0)
                {
                    // Delete old logo if exists
                    if (!string.IsNullOrEmpty(existingDukkan.LogoPath))
                    {
                        var oldPath = Server.MapPath("~" + existingDukkan.LogoPath);
                        if (System.IO.File.Exists(oldPath))
                        {
                            try
                            {
                                System.IO.File.Delete(oldPath);
                            }
                            catch (Exception ex)
                            {
                                // Log error but continue
                                System.Diagnostics.Debug.WriteLine($"Error deleting old logo: {ex.Message}");
                            }
                        }
                    }

                    // Save the new logo
                    var fileName = Path.GetFileName(shopLogo.FileName);
                    var extension = Path.GetExtension(fileName);
                    var newFileName = Guid.NewGuid().ToString() + extension;
                    var path = Path.Combine(Server.MapPath("~/Content/Images/Shops"), newFileName);
                    shopLogo.SaveAs(path);
                    existingDukkan.LogoPath = "/Content/Images/Shops/" + newFileName;
                }
                
                // Handle cover image upload
                if (coverImage != null && coverImage.ContentLength > 0)
                {
                    // Delete old cover image if exists
                    if (!string.IsNullOrEmpty(existingDukkan.CoverImagePath))
                    {
                        var oldPath = Server.MapPath("~" + existingDukkan.CoverImagePath);
                        if (System.IO.File.Exists(oldPath))
                        {
                            try
                            {
                                System.IO.File.Delete(oldPath);
                            }
                            catch (Exception ex)
                            {
                                // Log error but continue
                                System.Diagnostics.Debug.WriteLine($"Error deleting old cover image: {ex.Message}");
                            }
                        }
                    }

                    // Save the new cover image
                    var fileName = Path.GetFileName(coverImage.FileName);
                    var extension = Path.GetExtension(fileName);
                    var newFileName = Guid.NewGuid().ToString() + extension;
                    var path = Path.Combine(Server.MapPath("~/Content/Images/Shops/Covers"), newFileName);
                    
                    // Create directory if it doesn't exist
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    
                    coverImage.SaveAs(path);
                    existingDukkan.CoverImagePath = "/Content/Images/Shops/Covers/" + newFileName;
                }

                // Update shop properties - Basic info
                existingDukkan.Name = dukkan.Name;
                existingDukkan.Description = dukkan.Description;
                existingDukkan.IsActive = dukkan.IsActive;
                existingDukkan.Category = dukkan.Category;
                existingDukkan.Location = dukkan.Location;
                
                // Update shop properties - Social media
                existingDukkan.FacebookUrl = dukkan.FacebookUrl;
                existingDukkan.InstagramUrl = dukkan.InstagramUrl;
                existingDukkan.TwitterUrl = dukkan.TwitterUrl;
                existingDukkan.WebsiteUrl = dukkan.WebsiteUrl;
                
                // Update shop properties - Contact & Location info
                existingDukkan.FullAddress = dukkan.FullAddress;
                existingDukkan.PhoneNumber = dukkan.PhoneNumber;
                existingDukkan.Email = dukkan.Email;
                existingDukkan.OperatingHours = dukkan.OperatingHours;
                
                // Handle publish status
                existingDukkan.IsPublished = !saveDraft;

                // Only allow admins to update rating
                if (user.UserType == UserType.Admin)
                {
                    existingDukkan.Rating = dukkan.Rating;
                }

                db.SaveChanges();
                TempData["Message"] = saveDraft ? "Shop saved as draft." : "Shop updated and published!";
                return RedirectToAction("Details", new { id = dukkan.Id });
            }
            return View(dukkan);
        }

        // GET: Dukkan/Products/5
        [Authorize]
        [Route("Products/{id}")]
        public ActionResult Products(int id)
        {
            var username = User.Identity.Name;
            var user = db.Users.FirstOrDefault(u => u.Username == username);
            var dukkan = db.Dukkans
                .Include("Products")
                .FirstOrDefault(d => d.Id == id);

            if (dukkan == null)
            {
                return HttpNotFound();
            }

            // Check if user is the owner or an admin
            if (user.UserType != UserType.Admin && dukkan.OwnerId != user.Id)
            {
                return new HttpStatusCodeResult(403, "You don't have permission to manage this store's products.");
            }

            return View(dukkan);
        }

        // GET: Dukkan/Orders/5
        [Authorize]
        [Route("Orders/{id}")]
        public ActionResult Orders(int id)
        {
            var username = User.Identity.Name;
            var user = db.Users.FirstOrDefault(u => u.Username == username);
            var dukkan = db.Dukkans.Find(id);

            if (dukkan == null)
            {
                return HttpNotFound();
            }

            // Check if user is the owner or an admin
            if (user.UserType != UserType.Admin && dukkan.OwnerId != user.Id)
            {
                return new HttpStatusCodeResult(403, "You don't have permission to view this store's orders.");
            }

            // Set the dukkan ID in ViewBag so the view can use it for navigation
            ViewBag.DukkanId = id;

            // Get orders from two sources:
            // 1. Orders containing products originally from this shop
            // 2. Orders containing products from this shop via ShopProducts (multi-seller)
            var orders = db.Orders
                .Include("OrderDetails.Product") // Include Product navigation property
                .Include("User")
                .Where(o => 
                    // Match orders where product is directly owned by this shop
                    o.OrderDetails.Any(od => od.Product.DukkanId == id && od.ShopProductId == null) ||
                    // OR match orders where product was purchased from this shop via ShopProduct
                    o.OrderDetails.Any(od => od.ShopProductId != null && 
                                            db.ShopProducts
                                               .Any(sp => sp.Id == od.ShopProductId && 
                                                         sp.DukkanId == id))
                )
                .OrderByDescending(o => o.OrderDate)
                .ToList();

            return View(orders);
        }

        // GET: Dukkan/AddProduct/5
        [Authorize(Roles = "Admin,Seller")]
        [Route("AddProduct/{id}")]
        public ActionResult AddProduct(int id)
        {
            var username = User.Identity.Name;
            var user = db.Users.FirstOrDefault(u => u.Username == username);
            var dukkan = db.Dukkans.Find(id);

            if (dukkan == null)
            {
                return HttpNotFound("Shop not found");
            }

            // Check if user is the owner or an admin
            if (user.UserType != UserType.Admin && dukkan.OwnerId != user.Id)
            {
                return new HttpStatusCodeResult(403, "You don't have permission to add products to this shop");
            }

            ViewBag.DukkanId = id;
            ViewBag.DukkanName = dukkan.Name;
            
            // Prepare categories dropdown data
            var categories = db.Products
                .Select(p => p.Category)
                .Where(c => !string.IsNullOrEmpty(c))
                .Distinct()
                .ToList();
                
            ViewBag.Categories = new SelectList(categories);
            
            // Prepare brands dropdown data
            var brands = db.Products
                .Select(p => p.Brand)
                .Where(b => !string.IsNullOrEmpty(b))
                .Distinct()
                .ToList();
                
            ViewBag.Brands = new SelectList(brands);
            
            // Initialize a new product with default values
            var product = new Product
            {
                DukkanId = id,
                Stock = 0,
                IsClickAndCollect = false
            };

            return View(product);
        }

        // POST: Dukkan/AddProduct/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Seller")]
        [Route("AddProduct/{id}")]
        public ActionResult AddProduct(int id, Product product, HttpPostedFileBase productImage)
        {
            var username = User.Identity.Name;
            var user = db.Users.FirstOrDefault(u => u.Username == username);
            var dukkan = db.Dukkans.Find(id);

            if (dukkan == null)
            {
                return HttpNotFound("Shop not found");
            }

            // Check if user is the owner or an admin
            if (user.UserType != UserType.Admin && dukkan.OwnerId != user.Id)
            {
                return new HttpStatusCodeResult(403, "You don't have permission to add products to this shop");
            }

            // Ensure the DukkanId is set correctly
            product.DukkanId = id;
            
            if (ModelState.IsValid)
            {
                try
                {
                    // Handle image upload
                    if (productImage != null && productImage.ContentLength > 0)
                    {
                        var fileName = Path.GetFileName(productImage.FileName);
                        var extension = Path.GetExtension(fileName);
                        var newFileName = Guid.NewGuid().ToString() + extension;
                        var path = Path.Combine(Server.MapPath("~/Content/Images/Products"), newFileName);
                        
                        // Create directory if it doesn't exist
                        Directory.CreateDirectory(Path.GetDirectoryName(path));
                        
                        // Save the file
                        productImage.SaveAs(path);
                        product.PhotoPath = "/Content/Images/Products/" + newFileName;
                    }

                    db.Products.Add(product);
                    db.SaveChanges();

                    TempData["Message"] = "Product added successfully!";
                    return RedirectToAction("Products", new { id = id });
                }
                catch (Exception ex)
                {
                    // Log the error and add a user-friendly message
                    System.Diagnostics.Debug.WriteLine($"Error adding product: {ex.Message}");
                    ModelState.AddModelError("", "An error occurred while adding the product. Please try again.");
                }
            }

            // If we got this far, something failed, redisplay form
            ViewBag.DukkanId = id;
            ViewBag.DukkanName = dukkan.Name;
            
            // Prepare categories dropdown data
            var categories = db.Products
                .Select(p => p.Category)
                .Where(c => !string.IsNullOrEmpty(c))
                .Distinct()
                .ToList();
                
            ViewBag.Categories = new SelectList(categories);
            
            // Prepare brands dropdown data
            var brands = db.Products
                .Select(p => p.Brand)
                .Where(b => !string.IsNullOrEmpty(b))
                .Distinct()
                .ToList();
                
            ViewBag.Brands = new SelectList(brands);
            
            return View(product);
        }

        // GET: Dukkan/Dashboard
        [Authorize]
        [Route("Dashboard/{id}")]
        public ActionResult Dashboard(int id)
        {
            var username = User.Identity.Name;
            var user = db.Users.FirstOrDefault(u => u.Username == username);
            var dukkan = db.Dukkans
                .Include("Products")
                .FirstOrDefault(d => d.Id == id);

            if (dukkan == null)
            {
                return HttpNotFound();
            }

            // Check if user is the owner or an admin
            if (user.UserType != UserType.Admin && dukkan.OwnerId != user.Id)
            {
                return new HttpStatusCodeResult(403, "You don't have permission to manage this shop.");
            }

            // Set the active tab for the dashboard layout
            ViewBag.ActiveTab = "dashboard";
            ViewBag.DukkanId = id;
            ViewBag.DukkanName = dukkan.Name;

            // Get basic analytics for dashboard
            var totalOrders = db.Orders
                .Count(o => 
                    // Match orders where product is directly owned by this shop
                    o.OrderDetails.Any(od => od.Product.DukkanId == id && od.ShopProductId == null) ||
                    // OR match orders where product was purchased from this shop via ShopProduct
                    o.OrderDetails.Any(od => od.ShopProductId != null && 
                                          db.ShopProducts.Any(sp => sp.Id == od.ShopProductId && 
                                                                sp.DukkanId == id))
                );
                
            var totalRevenue = db.OrderDetails
                .Where(od => 
                    // Direct products owned by this shop
                    (od.Product.DukkanId == id && od.ShopProductId == null) ||
                    // OR products purchased via ShopProduct from this shop
                    (od.ShopProductId != null && 
                     db.ShopProducts.Any(sp => sp.Id == od.ShopProductId && sp.DukkanId == id))
                )
                .Sum(od => od.Quantity * od.UnitPrice);
                
            // Count both original products and products added via ShopProducts
            var ownedProductsCount = dukkan.Products.Count;
            var addedProductsCount = db.ShopProducts.Count(sp => sp.DukkanId == id);
            var productsCount = ownedProductsCount + addedProductsCount;
            
            var pendingOrders = db.Orders
                .Count(o => 
                    o.Status == OrderStatus.Pending && 
                    (
                        // Match orders where product is directly owned by this shop
                        o.OrderDetails.Any(od => od.Product.DukkanId == id && od.ShopProductId == null) ||
                        // OR match orders where product was purchased from this shop via ShopProduct
                        o.OrderDetails.Any(od => od.ShopProductId != null && 
                                              db.ShopProducts.Any(sp => sp.Id == od.ShopProductId && 
                                                                    sp.DukkanId == id))
                    )
                );
                
            var bestSellingProducts = db.OrderDetails
                .Where(od => 
                    // Direct products owned by this shop
                    (od.Product.DukkanId == id && od.ShopProductId == null) ||
                    // OR products purchased via ShopProduct from this shop
                    (od.ShopProductId != null && 
                     db.ShopProducts.Any(sp => sp.Id == od.ShopProductId && sp.DukkanId == id))
                )
                .GroupBy(od => od.ProductId)
                .Select(g => new { 
                    ProductId = g.Key, 
                    TotalSold = g.Sum(od => od.Quantity),
                    Revenue = g.Sum(od => od.Quantity * od.UnitPrice)
                })
                .OrderByDescending(x => x.TotalSold)
                .Take(5)
                .ToList();
                
            var bestSellingProductDetails = bestSellingProducts
                .Select(bs => new {
                    Product = db.Products.Find(bs.ProductId),
                    TotalSold = bs.TotalSold,
                    Revenue = bs.Revenue
                })
                .ToList();
            
            // Set analytics data for the view
            ViewBag.TotalOrders = totalOrders;
            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.ProductsCount = productsCount;
            ViewBag.PendingOrders = pendingOrders;
            ViewBag.BestSellingProducts = bestSellingProductDetails;
            
            // Get recent orders for the dashboard
            var recentOrders = db.Orders
                .Include("User")
                .Where(o => o.OrderDetails.Any(od => od.Product.DukkanId == id))
                .OrderByDescending(o => o.OrderDate)
                .Take(5)
                .ToList();
                
            ViewBag.RecentOrders = recentOrders;
            
            return View(dukkan);
        }

        // GET: Dukkan/Profile
        [Authorize]
        [Route("Profile/{id}")]
        public new ActionResult Profile(int id)
        {
            var username = User.Identity.Name;
            var user = db.Users.FirstOrDefault(u => u.Username == username);
            var dukkan = db.Dukkans
                .FirstOrDefault(d => d.Id == id);

            if (dukkan == null)
            {
                return HttpNotFound();
            }

            // Check if user is the owner or an admin
            if (user.UserType != UserType.Admin && dukkan.OwnerId != user.Id)
            {
                return new HttpStatusCodeResult(403, "You don't have permission to manage this shop.");
            }

            // Set the active tab for the dashboard layout
            ViewBag.ActiveTab = "profile";
            ViewBag.DukkanId = id;
            ViewBag.DukkanName = dukkan.Name;

            return View(dukkan);
        }
        
        // GET: Dukkan/ManageProducts
        [Authorize]
        [Route("ManageProducts/{id}")]
        public ActionResult ManageProducts(int id)
        {
            var username = User.Identity.Name;
            var user = db.Users.FirstOrDefault(u => u.Username == username);
            var dukkan = db.Dukkans
                .Include("Products")
                .FirstOrDefault(d => d.Id == id);

            if (dukkan == null)
            {
                return HttpNotFound();
            }

            // Check if user is the owner or an admin
            if (user.UserType != UserType.Admin && dukkan.OwnerId != user.Id)
            {
                return new HttpStatusCodeResult(403, "You don't have permission to manage this shop's products.");
            }

            // Get products from ShopProducts table (added from other shops) with explicit loading
            System.Diagnostics.Debug.WriteLine($"Looking for ShopProducts for shop ID: {id}");
            
            var shopProducts = db.ShopProducts
                .Include("Product")
                .Include("Product.Dukkan")
                .Where(sp => sp.DukkanId == id)
                .ToList();
            
            System.Diagnostics.Debug.WriteLine($"Found {shopProducts.Count} ShopProducts");
            
            // Add these to ViewBag to display in view
            ViewBag.ShopProducts = shopProducts;
            
            // Set the active tab for the dashboard layout
            ViewBag.ActiveTab = "products";
            ViewBag.DukkanId = id;
            ViewBag.DukkanName = dukkan.Name;

            return View(dukkan);
        }
        
        // GET: Dukkan/ManageOrders
        [Authorize]
        [Route("ManageOrders/{id}")]
        public ActionResult ManageOrders(int id)
        {
            var username = User.Identity.Name;
            var user = db.Users.FirstOrDefault(u => u.Username == username);
            var dukkan = db.Dukkans.Find(id);

            if (dukkan == null)
            {
                return HttpNotFound();
            }

            // Check if user is the owner or an admin
            if (user.UserType != UserType.Admin && dukkan.OwnerId != user.Id)
            {
                return new HttpStatusCodeResult(403, "You don't have permission to view this shop's orders.");
            }

            // Set the dukkan ID and active tab in ViewBag
            ViewBag.DukkanId = id;
            ViewBag.DukkanName = dukkan.Name;
            ViewBag.ActiveTab = "orders";

            var orders = db.Orders
                .Include("OrderDetails")
                .Include("User")
                .Where(o => o.OrderDetails.Any(od => od.Product.DukkanId == id))
                .OrderByDescending(o => o.OrderDate)
                .ToList();

            return View(orders);
        }
        
        // POST: Dukkan/UpdateOrderStatus
        [HttpPost]
        [Authorize]
        [Route("UpdateOrderStatus")]
        public ActionResult UpdateOrderStatus(int orderId, string status, int dukkanId)
        {
            var username = User.Identity.Name;
            var user = db.Users.FirstOrDefault(u => u.Username == username);
            var dukkan = db.Dukkans.Find(dukkanId);
            
            // Load order with OrderDetails and Products included
            var order = db.Orders
                .Include("OrderDetails.Product")
                .Include("OrderDetails.ShopProduct")
                .FirstOrDefault(o => o.Id == orderId);

            if (dukkan == null || order == null)
            {
                if (Request.IsAjaxRequest())
                {
                    return Json(new { success = false, message = "Order or shop not found" });
                }
                return HttpNotFound();
            }

            // Check if user is the owner or an admin
            if (user.UserType != UserType.Admin && dukkan.OwnerId != user.Id)
            {
                if (Request.IsAjaxRequest())
                {
                    return Json(new { success = false, message = "You don't have permission to manage this shop's orders" });
                }
                return new HttpStatusCodeResult(403, "You don't have permission to manage this shop's orders.");
            }

            // Validate that the order contains products from this shop (either direct or via ShopProducts)
            var orderContainsShopProducts = order.OrderDetails
                .Any(od => 
                    // Direct products owned by this shop
                    (od.Product != null && od.Product.DukkanId == dukkanId && od.ShopProductId == null) ||
                    // OR products purchased via ShopProduct from this shop
                    (od.ShopProductId != null && 
                     db.ShopProducts.Any(sp => sp.Id == od.ShopProductId && sp.DukkanId == dukkanId))
                );
                
            if (!orderContainsShopProducts)
            {
                if (Request.IsAjaxRequest())
                {
                    return Json(new { success = false, message = "This order does not contain products from your shop" });
                }
                return new HttpStatusCodeResult(400, "This order does not contain products from your shop.");
            }

            try
            {
                // Update the order status
                order.Status = (OrderStatus)Enum.Parse(typeof(OrderStatus), status);
                db.SaveChanges();

                TempData["Message"] = "Order status updated successfully.";

                if (Request.IsAjaxRequest())
                {
                    return Json(new { success = true, message = "Order status updated successfully" });
                }
                
                return RedirectToAction("Orders", new { id = dukkanId });
            }
            catch (Exception ex)
            {
                if (Request.IsAjaxRequest())
                {
                    return Json(new { success = false, message = $"Error updating order: {ex.Message}" });
                }
                
                TempData["Error"] = $"Error updating order: {ex.Message}";
                return RedirectToAction("Orders", new { id = dukkanId });
            }
        }
        
        // GET: Dukkan/DownloadOrdersCSV
        [Authorize]
        [Route("DownloadOrdersCSV/{id}")]
        public ActionResult DownloadOrdersCSV(int id)
        {
            var username = User.Identity.Name;
            var user = db.Users.FirstOrDefault(u => u.Username == username);
            var dukkan = db.Dukkans.Find(id);

            if (dukkan == null)
            {
                return HttpNotFound();
            }

            // Check if user is the owner or an admin
            if (user.UserType != UserType.Admin && dukkan.OwnerId != user.Id)
            {
                return new HttpStatusCodeResult(403, "You don't have permission to download this shop's orders.");
            }

            // Get orders from two sources:
            // 1. Orders containing products originally from this shop
            // 2. Orders containing products from this shop via ShopProducts (multi-seller)
            var orders = db.Orders
                .Include("OrderDetails.Product")
                .Include("User")
                .Where(o => 
                    // Match orders where product is directly owned by this shop
                    o.OrderDetails.Any(od => od.Product.DukkanId == id && od.ShopProductId == null) ||
                    // OR match orders where product was purchased from this shop via ShopProduct
                    o.OrderDetails.Any(od => od.ShopProductId != null && 
                                            db.ShopProducts
                                               .Any(sp => sp.Id == od.ShopProductId && 
                                                         sp.DukkanId == id))
                )
                .OrderByDescending(o => o.OrderDate)
                .ToList();

            // Build CSV content
            var csv = new System.Text.StringBuilder();
            csv.AppendLine("Order ID,Order Date,Customer Name,Total Amount,Status");

            foreach (var order in orders)
            {
                // Calculate total amount for this shop only (direct products or shop products)
                var totalForShop = order.OrderDetails
                    .Where(od => 
                        // Direct products owned by this shop
                        (od.Product.DukkanId == id && od.ShopProductId == null) ||
                        // OR products purchased via ShopProduct from this shop
                        (od.ShopProductId != null && 
                         db.ShopProducts.Any(sp => sp.Id == od.ShopProductId && sp.DukkanId == id))
                    )
                    .Sum(od => od.Quantity * od.UnitPrice);
                    
                var line = string.Format("{0},{1},{2},{3},{4}",
                    order.Id,
                    order.OrderDate.ToString("yyyy-MM-dd"),
                    order.User.FullName,
                    totalForShop.ToString("0.00"),
                    order.Status);
                    
                csv.AppendLine(line);
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", $"Shop_Orders_{id}_{DateTime.Now.ToString("yyyyMMdd")}.csv");
        }
        
        // GET: Dukkan/Analytics
        [Authorize]
        [Route("Analytics/{id}")]
        public ActionResult Analytics(int id)
        {
            var username = User.Identity.Name;
            var user = db.Users.FirstOrDefault(u => u.Username == username);
            var dukkan = db.Dukkans.Find(id);

            if (dukkan == null)
            {
                return HttpNotFound();
            }

            // Check if user is the owner or an admin
            if (user.UserType != UserType.Admin && dukkan.OwnerId != user.Id)
            {
                return new HttpStatusCodeResult(403, "You don't have permission to view this shop's analytics.");
            }

            // Set the active tab for the dashboard layout
            ViewBag.ActiveTab = "analytics";
            ViewBag.DukkanId = id;
            ViewBag.DukkanName = dukkan.Name;

            // Calculate monthly revenue for chart
            var sixMonthsAgo = DateTime.Now.AddMonths(-6);
            
            // Get revenue from both direct products and shop products
            var revenueByMonth = db.OrderDetails
                .Where(od => od.Order.OrderDate >= sixMonthsAgo && 
                    (
                        // Direct products owned by this shop
                        (od.Product.DukkanId == id && od.ShopProductId == null) ||
                        // OR products purchased via ShopProduct from this shop
                        (od.ShopProductId != null && 
                         db.ShopProducts.Any(sp => sp.Id == od.ShopProductId && sp.DukkanId == id))
                    ))
                .GroupBy(od => new { Year = od.Order.OrderDate.Year, Month = od.Order.OrderDate.Month })
                .Select(g => new {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Revenue = g.Sum(od => od.Quantity * od.UnitPrice)
                })
                .OrderBy(x => x.Year)
                .ThenBy(x => x.Month)
                .ToList();
                
            // Format for chart
            var months = new List<string>();
            var revenueData = new List<decimal>();
            
            for (int i = 0; i < 6; i++)
            {
                var date = DateTime.Now.AddMonths(-5 + i);
                var month = date.ToString("MMM yyyy");
                months.Add(month);
                
                var matchingMonth = revenueByMonth.FirstOrDefault(r => r.Year == date.Year && r.Month == date.Month);
                revenueData.Add(matchingMonth != null ? matchingMonth.Revenue : 0);
            }
            
            ViewBag.ChartMonths = months;
            ViewBag.ChartRevenue = revenueData;
            
            // Get best-selling products
            var bestSellingProducts = db.OrderDetails
                .Where(od => od.Product.DukkanId == id)
                .GroupBy(od => od.ProductId)
                .Select(g => new { 
                    ProductId = g.Key, 
                    TotalSold = g.Sum(od => od.Quantity),
                    Revenue = g.Sum(od => od.Quantity * od.UnitPrice)
                })
                .OrderByDescending(x => x.TotalSold)
                .Take(10)
                .ToList();
                
            var bestSellingProductDetails = bestSellingProducts
                .Select(bs => new {
                    Product = db.Products.Find(bs.ProductId),
                    TotalSold = bs.TotalSold,
                    Revenue = bs.Revenue
                })
                .ToList();
                
            ViewBag.BestSellingProducts = bestSellingProductDetails;
            
            // Get order status breakdown
            var orderStatusSummary = db.Orders
                .Where(o => o.OrderDetails.Any(od => od.Product.DukkanId == id))
                .GroupBy(o => o.Status)
                .Select(g => new {
                    Status = g.Key,
                    Count = g.Count()
                })
                .ToList();
                
            ViewBag.OrderStatusSummary = orderStatusSummary;
            
            return View(dukkan);
        }
        
        // POST: Dukkan/ReorderProducts
        [HttpPost]
        [Authorize]
        [Route("ReorderProducts")]
        public ActionResult ReorderProducts(int[] productIds, int dukkanId)
        {
            var username = User.Identity.Name;
            var user = db.Users.FirstOrDefault(u => u.Username == username);
            var dukkan = db.Dukkans.Find(dukkanId);

            if (dukkan == null)
            {
                return HttpNotFound();
            }

            // Check if user is the owner or an admin
            if (user.UserType != UserType.Admin && dukkan.OwnerId != user.Id)
            {
                return new HttpStatusCodeResult(403, "You don't have permission to manage this shop's products.");
            }

            // Ensure all products belong to this shop
            var shopProducts = db.Products.Where(p => p.DukkanId == dukkanId).ToList();
            
            for (int i = 0; i < productIds.Length; i++)
            {
                var product = shopProducts.FirstOrDefault(p => p.Id == productIds[i]);
                if (product != null)
                {
                    product.DisplayOrder = i;
                }
            }
            
            db.SaveChanges();
            
            return Json(new { success = true, message = "Products reordered successfully" });
        }

        // POST: Dukkan/QuickEditProduct
        [HttpPost]
        [Authorize]
        [Route("QuickEditProduct")]
        public ActionResult QuickEditProduct(int productId, string name, decimal price, int stock, bool isActive, int dukkanId)
        {
            var username = User.Identity.Name;
            var user = db.Users.FirstOrDefault(u => u.Username == username);
            var dukkan = db.Dukkans.Find(dukkanId);

            if (dukkan == null)
            {
                return Json(new { success = false, message = "Shop not found" });
            }

            // Check if user is the owner or an admin
            if (user.UserType != UserType.Admin && dukkan.OwnerId != user.Id)
            {
                return Json(new { success = false, message = "You don't have permission to edit this shop's products" });
            }

            // Find the product
            var product = db.Products.FirstOrDefault(p => p.Id == productId && p.DukkanId == dukkanId);
            
            if (product == null)
            {
                return Json(new { success = false, message = "Product not found" });
            }

            // Update product properties
            product.Name = name;
            product.Price = price;
            product.Stock = stock;
            product.IsActive = isActive;
            
            try
            {
                db.SaveChanges();
                return Json(new { success = true, message = "Product updated successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error updating product: " + ex.Message });
            }
        }
        
        // POST: Dukkan/DeleteProduct
        [HttpPost]
        [Authorize]
        [Route("DeleteProduct")]
        public ActionResult DeleteProduct(int productId, int dukkanId)
        {
            var username = User.Identity.Name;
            var user = db.Users.FirstOrDefault(u => u.Username == username);
            var dukkan = db.Dukkans.Find(dukkanId);

            if (dukkan == null)
            {
                return Json(new { success = false, message = "Shop not found" });
            }

            // Check if user is the owner or an admin
            if (user.UserType != UserType.Admin && dukkan.OwnerId != user.Id)
            {
                return Json(new { success = false, message = "You don't have permission to delete this shop's products" });
            }

            // Find the product
            var product = db.Products.FirstOrDefault(p => p.Id == productId && p.DukkanId == dukkanId);
            
            if (product == null)
            {
                return Json(new { success = false, message = "Product not found" });
            }
            
            // Check if the product is in any orders
            var isInOrders = db.OrderDetails.Any(od => od.ProductId == productId);
            
            if (isInOrders)
            {
                // If it's in orders, just mark it as inactive instead of deleting
                product.IsActive = false;
            }
            else
            {
                // If not in any orders, we can safely delete it
                // Delete product image if exists
                if (!string.IsNullOrEmpty(product.PhotoPath))
                {
                    var imagePath = Server.MapPath("~" + product.PhotoPath);
                    if (System.IO.File.Exists(imagePath))
                    {
                        try
                        {
                            System.IO.File.Delete(imagePath);
                        }
                        catch (Exception ex)
                        {
                            // Log error but continue
                            System.Diagnostics.Debug.WriteLine($"Error deleting product image: {ex.Message}");
                        }
                    }
                }
                
                db.Products.Remove(product);
            }
            
            try
            {
                db.SaveChanges();
                return Json(new 
                { 
                    success = true, 
                    message = isInOrders ? "Product has been marked as inactive because it's associated with orders" : "Product deleted successfully" 
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error deleting product: " + ex.Message });
            }
        }
        
        // GET: Dukkan/GetOrderDetails
        [HttpGet]
        [Authorize]
        [Route("GetOrderDetails")]
        public ActionResult GetOrderDetails(int orderId, int dukkanId)
        {
            var username = User.Identity.Name;
            var user = db.Users.FirstOrDefault(u => u.Username == username);
            var dukkan = db.Dukkans.Find(dukkanId);
            var order = db.Orders
                .Include("User")
                .Include("OrderDetails")
                .Include("OrderDetails.Product")
                .FirstOrDefault(o => o.Id == orderId);

            if (dukkan == null || order == null)
            {
                return Json(new { success = false, message = "Order or shop not found" }, JsonRequestBehavior.AllowGet);
            }

            // Check if user is the owner or an admin
            if (user.UserType != UserType.Admin && dukkan.OwnerId != user.Id)
            {
                return Json(new { success = false, message = "You don't have permission to view this order" }, JsonRequestBehavior.AllowGet);
            }

            // Check if the order contains products from this shop
            var shopItems = order.OrderDetails.Where(od => od.Product.DukkanId == dukkanId).ToList();
            if (shopItems.Count == 0)
            {
                return Json(new { success = false, message = "This order does not contain products from your shop" }, JsonRequestBehavior.AllowGet);
            }
            
            // Create a simplified order object for the response
            var orderDetails = new
            {
                orderId = order.Id,
                orderDate = order.OrderDate.ToString("MM/dd/yyyy HH:mm"),
                status = order.Status,
                customerName = order.User.FullName,
                customerEmail = order.User.Email,
                customerPhone = order.User.PhoneNumber ?? "Not provided",
                paymentMethod = order.PaymentMethod ?? "Not specified",
                
                items = shopItems.Select(item => new {
                    productId = item.ProductId,
                    productName = item.Product.Name,
                    quantity = item.Quantity,
                    unitPrice = item.UnitPrice,
                }).ToList()
            };
            
            return Json(new { success = true, order = orderDetails }, JsonRequestBehavior.AllowGet);
        }

        // GET: Dukkan/EditProduct
        [Authorize]
        [Route("EditProduct/{id}/{productId}")]
        public ActionResult EditProduct(int id, int productId)
        {
            var username = User.Identity.Name;
            var user = db.Users.FirstOrDefault(u => u.Username == username);
            var dukkan = db.Dukkans.Find(id);
            var product = db.Products.FirstOrDefault(p => p.Id == productId && p.DukkanId == id);

            if (dukkan == null)
            {
                return HttpNotFound("Shop not found");
            }

            if (product == null)
            {
                return HttpNotFound("Product not found");
            }

            // Check if user is the owner or an admin
            if (user.UserType != UserType.Admin && dukkan.OwnerId != user.Id)
            {
                return new HttpStatusCodeResult(403, "You don't have permission to edit this product");
            }

            ViewBag.DukkanId = id;
            ViewBag.DukkanName = dukkan.Name;
            
            // Prepare categories dropdown data
            var categories = db.Products
                .Select(p => p.Category)
                .Where(c => !string.IsNullOrEmpty(c))
                .Distinct()
                .ToList();
                
            ViewBag.Categories = new SelectList(categories, product.Category);
            
            // Prepare brands dropdown data
            var brands = db.Products
                .Select(p => p.Brand)
                .Where(b => !string.IsNullOrEmpty(b))
                .Distinct()
                .ToList();
                
            ViewBag.Brands = new SelectList(brands, product.Brand);
            
            return View(product);
        }
        
        // POST: Dukkan/EditProduct
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        [Route("EditProduct/{id}/{productId}")]
        public ActionResult EditProduct(int id, int productId, Product product, HttpPostedFileBase productImage)
        {
            var username = User.Identity.Name;
            var user = db.Users.FirstOrDefault(u => u.Username == username);
            var dukkan = db.Dukkans.Find(id);
            var existingProduct = db.Products.FirstOrDefault(p => p.Id == productId && p.DukkanId == id);

            if (dukkan == null)
            {
                return HttpNotFound("Shop not found");
            }

            if (existingProduct == null)
            {
                return HttpNotFound("Product not found");
            }

            // Check if user is the owner or an admin
            if (user.UserType != UserType.Admin && dukkan.OwnerId != user.Id)
            {
                return new HttpStatusCodeResult(403, "You don't have permission to edit this product");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Handle image upload if new image is selected
                    if (productImage != null && productImage.ContentLength > 0)
                    {
                        // Delete existing image if exists
                        if (!string.IsNullOrEmpty(existingProduct.PhotoPath))
                        {
                            var oldImagePath = Server.MapPath("~" + existingProduct.PhotoPath);
                            if (System.IO.File.Exists(oldImagePath))
                            {
                                try
                                {
                                    System.IO.File.Delete(oldImagePath);
                                }
                                catch (Exception ex)
                                {
                                    // Log error but continue
                                    System.Diagnostics.Debug.WriteLine($"Error deleting old product image: {ex.Message}");
                                }
                            }
                        }
                        
                        // Save new image
                        var fileName = Path.GetFileName(productImage.FileName);
                        var extension = Path.GetExtension(fileName);
                        var newFileName = Guid.NewGuid().ToString() + extension;
                        var path = Path.Combine(Server.MapPath("~/Content/Images/Products"), newFileName);
                        
                        // Create directory if it doesn't exist
                        Directory.CreateDirectory(Path.GetDirectoryName(path));
                        
                        // Save the file
                        productImage.SaveAs(path);
                        existingProduct.PhotoPath = "/Content/Images/Products/" + newFileName;
                    }

                    // Update product properties
                    existingProduct.Name = product.Name;
                    existingProduct.Description = product.Description;
                    existingProduct.Price = product.Price;
                    existingProduct.Stock = product.Stock;
                    existingProduct.Category = product.Category;
                    existingProduct.Brand = product.Brand;
                    existingProduct.IsActive = product.IsActive;
                    existingProduct.IsClickAndCollect = product.IsClickAndCollect;
                    
                    db.SaveChanges();

                    TempData["Message"] = "Product updated successfully!";
                    return RedirectToAction("Products", new { id = id });
                }
                catch (Exception ex)
                {
                    // Log the error and add a user-friendly message
                    System.Diagnostics.Debug.WriteLine($"Error updating product: {ex.Message}");
                    ModelState.AddModelError("", "An error occurred while updating the product. Please try again.");
                }
            }

            // If we got this far, something failed, redisplay form
            ViewBag.DukkanId = id;
            ViewBag.DukkanName = dukkan.Name;
            
            // Prepare categories dropdown data
            var categories = db.Products
                .Select(p => p.Category)
                .Where(c => !string.IsNullOrEmpty(c))
                .Distinct()
                .ToList();
                
            ViewBag.Categories = new SelectList(categories, product.Category);
            
            // Prepare brands dropdown data
            var brands = db.Products
                .Select(p => p.Brand)
                .Where(b => !string.IsNullOrEmpty(b))
                .Distinct()
                .ToList();
                
            ViewBag.Brands = new SelectList(brands, product.Brand);
            
            return View(product);
        }

        // GET: ExistingProducts
        [Authorize]
        [Route("ExistingProducts/{dukkanId}")]
        public ActionResult ExistingProducts(int dukkanId, string searchTerm = "", int page = 1)
        {
            // Verify current user owns this shop
            if (!IsCurrentUserShopOwner(dukkanId))
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var pageSize = 12; // Number of products per page
            
            // Get ALL products in the system (temporarily disable all filtering for debugging)
            var productsQuery = db.Products.AsQueryable();
            
            // Debug output
            System.Diagnostics.Debug.WriteLine($"Total product count in database: {productsQuery.Count()}");
            
            // Apply search filter if provided
            if (!string.IsNullOrEmpty(searchTerm))
            {
                productsQuery = productsQuery.Where(p => 
                    p.Name.Contains(searchTerm) || 
                    p.Description.Contains(searchTerm) || 
                    p.Brand.Contains(searchTerm) ||
                    p.Category.Contains(searchTerm));
            }
            
            // Get total count for pagination
            var totalItems = productsQuery.Count();
            
            // Debug output
            System.Diagnostics.Debug.WriteLine($"Found {totalItems} products for shop {dukkanId}");
            
            // Apply pagination
            var products = productsQuery
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            
            // Create view model with pagination info
            var viewModel = new ProductListViewModel
            {
                Products = products,
                PaginationInfo = new PaginationInfo
                {
                    CurrentPage = page,
                    ItemsPerPage = pageSize,
                    TotalItems = totalItems
                },
                SearchTerm = searchTerm,
                DukkanId = dukkanId
            };
            
            return View(viewModel);
        }
        
        // POST: AddExistingProduct
        [HttpPost]
        [Authorize]
        [Route("AddExistingProduct")]
        public ActionResult AddExistingProduct(int dukkanId, int productId, decimal price, int stock, bool isClickAndCollect = false)
        {
            System.Diagnostics.Debug.WriteLine($"AddExistingProduct called: Shop={dukkanId}, Product={productId}, Price={price}");
            
            // Verify current user owns this shop
            if (!IsCurrentUserShopOwner(dukkanId))
            {
                System.Diagnostics.Debug.WriteLine("Permission denied: User is not shop owner");
                return Json(new { success = false, message = "You do not have permission to add products to this shop." });
            }
            
            try
            {
                // Check if product exists
                var product = db.Products.Find(productId);
                if (product == null)
                {
                    System.Diagnostics.Debug.WriteLine("Product not found");
                    return Json(new { success = false, message = "Product not found." });
                }
                
                System.Diagnostics.Debug.WriteLine($"Found product: {product.Name} (ID: {product.Id})");
                
                // Check if this shop already has this product
                var existingShopProduct = db.ShopProducts.FirstOrDefault(sp => sp.DukkanId == dukkanId && sp.ProductId == productId);
                if (existingShopProduct != null)
                {
                    System.Diagnostics.Debug.WriteLine("Product already exists in shop");
                    return Json(new { success = false, message = "This product is already in your shop." });
                }
                
                // Create a new ShopProduct entry
                var shopProduct = new ShopProduct
                {
                    DukkanId = dukkanId,
                    ProductId = productId,
                    Price = price,
                    Stock = stock,
                    IsClickAndCollect = isClickAndCollect
                };
                
                System.Diagnostics.Debug.WriteLine("Adding ShopProduct to database");
                db.ShopProducts.Add(shopProduct);
                db.SaveChanges();
                System.Diagnostics.Debug.WriteLine($"ShopProduct added successfully with ID: {shopProduct.Id}");
                
                return Json(new { 
                    success = true, 
                    message = "Product added to your shop successfully.",
                    shopProductId = shopProduct.Id
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in AddExistingProduct: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }

        // GET: Dukkan/AddProductFromOthers/{id}
        [Authorize(Roles = "Admin,Seller")]
        [Route("AddProductFromOthers/{id}")]
        public ActionResult AddProductFromOthers(int id)
        {
            var username = User.Identity.Name;
            var user = db.Users.FirstOrDefault(u => u.Username == username);
            
            if (user == null)
            {
                return HttpNotFound("User not found");
            }
            
            // Get user's shops
            var userShops = db.Dukkans
                .Where(d => d.OwnerId == user.Id && d.IsActive)
                .ToList();
                
            if (userShops == null || !userShops.Any())
            {
                TempData["Message"] = "You don't have any active shops.";
                return RedirectToAction("Index", "Shop");
            }
            
            // Find popular products to add
            var popularProducts = db.Products
                .Include("Dukkan")
                .Where(p => (!p.DukkanId.HasValue || p.DukkanId.Value != id))
                .OrderByDescending(p => p.CreatedAt)
                .Take(50)
                .ToList();
                
            // Check and exclude products already in this shop via ShopProducts table
            var existingProductIds = db.ShopProducts
                .Where(sp => sp.DukkanId == id)
                .Select(sp => sp.ProductId)
                .ToList();
                
            // Remove products that are already in the shop
            popularProducts = popularProducts
                .Where(p => !existingProductIds.Contains(p.Id))
                .ToList();
                
            ViewBag.UserShops = userShops;
            ViewBag.CurrentShopId = id;
            
            return View(popularProducts);
        }

        // POST: Dukkan/RemoveShopProduct
        [HttpPost]
        [Authorize]
        [Route("RemoveShopProduct")]
        public ActionResult RemoveShopProduct(int shopProductId, int dukkanId)
        {
            // Debug output all parameters received
            System.Diagnostics.Debug.WriteLine($"=== RemoveShopProduct parameters ===");
            System.Diagnostics.Debug.WriteLine($"shopProductId: {shopProductId}");
            System.Diagnostics.Debug.WriteLine($"dukkanId: {dukkanId}");
            
            try
            {
                // Skip owner validation temporarily for testing
                var shopProduct = db.ShopProducts.Find(shopProductId);
                
                if (shopProduct == null)
                {
                    System.Diagnostics.Debug.WriteLine($"ShopProduct not found with ID: {shopProductId}");
                    return Json(new { success = false, message = $"Product not found with ID {shopProductId}" });
                }
                
                System.Diagnostics.Debug.WriteLine($"Found ShopProduct: ID={shopProduct.Id}, DukkanId={shopProduct.DukkanId}, ProductId={shopProduct.ProductId}");
                System.Diagnostics.Debug.WriteLine($"Removing ShopProduct: {shopProductId}");
                
                db.ShopProducts.Remove(shopProduct);
                db.SaveChanges();
                
                System.Diagnostics.Debug.WriteLine("ShopProduct removed successfully via async method");
                return Json(new { success = true, message = "Product removed successfully" });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error removing ShopProduct: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }
        
        // POST: EditShopProduct
        [HttpPost]
        [Authorize]
        [Route("EditShopProduct")]
        public ActionResult EditShopProduct(int shopProductId, int dukkanId, decimal price, int stock)
        {
            // Verify current user owns this shop
            if (!IsCurrentUserShopOwner(dukkanId))
            {
                return Json(new { success = false, message = "You do not have permission to manage this shop." });
            }
            
            try
            {
                // Find the ShopProduct
                var shopProduct = db.ShopProducts.Find(shopProductId);
                if (shopProduct == null)
                {
                    return Json(new { success = false, message = "Product not found" });
                }
                
                // Verify this ShopProduct belongs to the specified shop
                if (shopProduct.DukkanId != dukkanId)
                {
                    return Json(new { success = false, message = "This product does not belong to your shop" });
                }
                
                // Update the ShopProduct
                shopProduct.Price = price;
                shopProduct.Stock = stock;
                db.SaveChanges();
                
                return Json(new { success = true, message = "Product updated successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error updating product: " + ex.Message });
            }
        }

        // GET: Diagnostic to check ShopProducts
        [Authorize]
        [Route("DiagnoseShopProducts/{dukkanId}")]
        public ActionResult DiagnoseShopProducts(int dukkanId)
        {
            // Verify current user owns this shop or is admin
            if (!IsCurrentUserShopOwner(dukkanId))
            {
                return RedirectToAction("AccessDenied", "Account");
            }
            
            var allShopProducts = db.ShopProducts
                .Include("Product")
                .Include("Product.Dukkan")
                .Include("Dukkan")
                .Where(sp => sp.DukkanId == dukkanId)
                .ToList();
                
            var viewModel = new {
                ShopProducts = allShopProducts,
                ShopName = db.Dukkans.Find(dukkanId)?.Name ?? "Unknown Shop",
                ShopId = dukkanId
            };
            
            return View(viewModel);
        }

        // GET: Test page for removing shop products
        [Authorize]
        [Route("TestRemove")]
        public ActionResult TestRemove()
        {
            return View();
        }

        // GET: Dukkan/ManageOtherProducts/{id}
        [Authorize]
        [Route("ManageOtherProducts/{id}")]
        public ActionResult ManageOtherProducts(int id)
        {
            // Simple redirect to DiagnoseShopProducts to make it easier to find
            return RedirectToAction("DiagnoseShopProducts", new { dukkanId = id });
        }

        // GET: Dukkan/RemoveProducts/{id}
        [Authorize]
        [Route("RemoveProducts/{id}")]
        public ActionResult RemoveProducts(int id)
        {
            // Verify current user owns this shop or is admin
            if (!IsCurrentUserShopOwner(id))
            {
                return RedirectToAction("AccessDenied", "Account");
            }
            
            var allShopProducts = db.ShopProducts
                .Include("Product")
                .Include("Product.Dukkan")
                .Include("Dukkan")
                .Where(sp => sp.DukkanId == id)
                .ToList();
                
            var viewModel = new {
                ShopProducts = allShopProducts,
                ShopName = db.Dukkans.Find(id)?.Name ?? "Unknown Shop",
                ShopId = id
            };
            
            return View(viewModel);
        }

        // GET: Dukkan/OtherProducts/{id}
        [Authorize]
        [Route("OtherProducts/{id}")]
        public ActionResult OtherProducts(int id)
        {
            // Verify current user owns this shop or is admin
            if (!IsCurrentUserShopOwner(id))
            {
                return RedirectToAction("AccessDenied", "Account");
            }
            
            var shopProducts = db.ShopProducts
                .Include("Product")
                .Include("Product.Dukkan")
                .Where(sp => sp.DukkanId == id)
                .ToList();
                
            ViewBag.DukkanId = id;
            ViewBag.DukkanName = db.Dukkans.Find(id)?.Name ?? "Unknown Shop";
            
            return View(shopProducts);
        }
        
        // POST: Dukkan/UpdateShopProduct
        [HttpPost]
        [Authorize]
        [Route("UpdateShopProduct")]
        public ActionResult UpdateShopProduct(int shopProductId, decimal price, int stock, int dukkanId)
        {
            System.Diagnostics.Debug.WriteLine($"UpdateShopProduct called: ShopProductId={shopProductId}, Price={price}, Stock={stock}, DukkanId={dukkanId}");
            
            // Verify current user owns this shop or is admin
            if (!IsCurrentUserShopOwner(dukkanId))
            {
                return Json(new { success = false, message = "Access denied - You don't have permission to manage this shop" });
            }
            
            try
            {
                var shopProduct = db.ShopProducts.Find(shopProductId);
                
                if (shopProduct == null)
                {
                    System.Diagnostics.Debug.WriteLine($"ShopProduct not found with ID: {shopProductId}");
                    return Json(new { success = false, message = $"Product not found with ID {shopProductId}" });
                }
                
                if (shopProduct.DukkanId != dukkanId)
                {
                    System.Diagnostics.Debug.WriteLine($"ShopProduct DukkanId {shopProduct.DukkanId} doesn't match requested DukkanId {dukkanId}");
                    return Json(new { success = false, message = $"This product doesn't belong to your store" });
                }
                
                // Update the product
                shopProduct.Price = price;
                shopProduct.Stock = stock;
                
                db.SaveChanges();
                
                System.Diagnostics.Debug.WriteLine($"ShopProduct updated successfully: {shopProductId}");
                return Json(new { success = true, message = "Product updated successfully" });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating ShopProduct: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        // GET: Dukkan/other/{id} - Simpler URL for easier access
        [Authorize]
        [Route("other/{id}")]
        public ActionResult OtherProductsShortcut(int id)
        {
            return RedirectToAction("OtherProducts", new { id = id });
        }

        // GET: Dukkan/DirectPage/{id} - Direct access with no authentication required
        [AllowAnonymous]
        [Route("DirectPage/{id}")]
        public ActionResult DirectPage(int id)
        {
            var dukkan = db.Dukkans.Find(id);
            
            if (dukkan == null)
            {
                // If store not found, still load the page but show an error
                ViewBag.Error = "Store not found with ID: " + id;
            }
            
            ViewBag.DukkanId = id;
            ViewBag.DukkanName = dukkan?.Name ?? "Unknown Store";
            
            return View();
        }

        // GET: Dukkan/CheckShopProducts/{id} - Check if database can access ShopProducts
        [AllowAnonymous]
        [Route("CheckShopProducts/{id}")]
        public ActionResult CheckShopProducts(int id)
        {
            try
            {
                var storeExists = db.Dukkans.Any(d => d.Id == id);
                
                // Try to get shop products for this store
                var shopProducts = db.ShopProducts
                    .Where(sp => sp.DukkanId == id)
                    .ToList();
                
                // Create a simple summary to return
                var summary = new
                {
                    success = true,
                    storeId = id,
                    storeExists = storeExists,
                    productCount = shopProducts.Count,
                    products = shopProducts.Select(sp => new 
                    {
                        id = sp.Id,
                        productId = sp.ProductId,
                        price = sp.Price,
                        stock = sp.Stock
                    }).Take(5).ToList() // Only take first 5 to keep response small
                };
                
                return Json(summary, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new 
                {
                    success = false,
                    error = ex.Message,
                    stackTrace = ex.StackTrace
                }, JsonRequestBehavior.AllowGet);
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