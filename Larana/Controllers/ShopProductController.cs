using System;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Larana.Data;
using Larana.Models;

namespace Larana.Controllers
{
    [Authorize(Roles = "Seller")]
    [RoutePrefix("ShopProduct")]
    public class ShopProductController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ShopProductController()
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
                // Return -1 to indicate user not found
                return -1;
            }
            
            return user.Id;
        }

        // GET: ShopProduct/List/1 (1 is shopId)
        [HttpGet]
        [Route("List/{shopId:int}")]
        public ActionResult List(int shopId)
        {
            var userId = GetCurrentUserId();
            if (userId == -1)
            {
                return RedirectToAction("Login", "Account");
            }

            var shop = _context.Dukkans.FirstOrDefault(d => d.Id == shopId);
            if (shop == null)
            {
                return HttpNotFound("Shop not found");
            }

            // Verify user is owner of shop
            if (shop.OwnerId != userId)
            {
                return new HttpStatusCodeResult(403, "You don't have permission to view products in this shop");
            }

            var products = _context.Products.Where(p => p.DukkanId == shopId).ToList();
            ViewBag.ShopId = shopId;
            ViewBag.ShopName = shop.Name;
            
            return View(products);
        }

        // GET: ShopProduct/Add/1 (1 is shopId)
        [HttpGet]
        [Route("Add/{shopId:int}")]
        public ActionResult Add(int shopId)
        {
            var userId = GetCurrentUserId();
            if (userId == -1)
            {
                return RedirectToAction("Login", "Account");
            }

            var shop = _context.Dukkans.FirstOrDefault(d => d.Id == shopId);
            if (shop == null)
            {
                return HttpNotFound("Shop not found");
            }

            // Verify user is owner of shop
            if (shop.OwnerId != userId)
            {
                return new HttpStatusCodeResult(403, "You don't have permission to add products to this shop");
            }

            ViewBag.ShopId = shopId;
            ViewBag.ShopName = shop.Name;
            
            // Prepare categories dropdown data
            var categories = _context.Products
                .Select(p => p.Category)
                .Where(c => !string.IsNullOrEmpty(c))
                .Distinct()
                .ToList();
                
            ViewBag.Categories = new SelectList(categories);
            
            // Prepare brands dropdown data
            var brands = _context.Products
                .Select(p => p.Brand)
                .Where(b => !string.IsNullOrEmpty(b))
                .Distinct()
                .ToList();
                
            ViewBag.Brands = new SelectList(brands);
            
            // Initialize a new product with default values
            var product = new Product
            {
                DukkanId = shopId,
                Stock = 0,
                IsClickAndCollect = false
            };

            return View(product);
        }

        // POST: ShopProduct/Add/1 (1 is shopId)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Add/{shopId:int}")]
        public ActionResult Add(Product product, HttpPostedFileBase productImage, int shopId)
        {
            var userId = GetCurrentUserId();
            if (userId == -1)
            {
                return RedirectToAction("Login", "Account");
            }

            // Make sure the product is associated with the correct shop
            product.DukkanId = shopId;

            var shop = _context.Dukkans.FirstOrDefault(d => d.Id == shopId);
            if (shop == null)
            {
                return HttpNotFound("Shop not found");
            }

            // Verify user is owner of shop
            if (shop.OwnerId != userId)
            {
                return new HttpStatusCodeResult(403, "You don't have permission to add products to this shop");
            }
            
            // Ensure required fields are set
            if (string.IsNullOrWhiteSpace(product.Name))
            {
                ModelState.AddModelError("Name", "Product name is required.");
            }
            
            if (string.IsNullOrWhiteSpace(product.Brand))
            {
                ModelState.AddModelError("Brand", "Brand is required.");
            }
            
            if (product.Price <= 0)
            {
                ModelState.AddModelError("Price", "Price must be greater than zero.");
            }

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

                    _context.Products.Add(product);
                    _context.SaveChanges();

                    TempData["Message"] = "Product added successfully!";
                    return RedirectToAction("List", new { shopId = shopId });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "An error occurred while adding the product. Please try again.");
                    System.Diagnostics.Debug.WriteLine($"Error adding product: {ex.Message}");
                }
            }

            // If we got this far, something failed, redisplay form
            ViewBag.ShopId = shopId;
            ViewBag.ShopName = shop.Name;
            
            // Prepare categories dropdown data
            var categories = _context.Products
                .Select(p => p.Category)
                .Where(c => !string.IsNullOrEmpty(c))
                .Distinct()
                .ToList();
                
            ViewBag.Categories = new SelectList(categories);
            
            // Prepare brands dropdown data
            var brands = _context.Products
                .Select(p => p.Brand)
                .Where(b => !string.IsNullOrEmpty(b))
                .Distinct()
                .ToList();
                
            ViewBag.Brands = new SelectList(brands);
            
            return View(product);
        }

        // GET: ShopProduct/Edit/5 (5 is productId)
        [HttpGet]
        [Route("Edit/{id:int}")]
        public ActionResult Edit(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == -1)
            {
                return RedirectToAction("Login", "Account");
            }

            var product = _context.Products.FirstOrDefault(p => p.Id == id);
            if (product == null)
            {
                return HttpNotFound("Product not found");
            }

            var shop = _context.Dukkans.FirstOrDefault(d => d.Id == product.DukkanId);
            if (shop == null)
            {
                return HttpNotFound("Shop not found");
            }

            // Verify user is owner of shop
            if (shop.OwnerId != userId)
            {
                return new HttpStatusCodeResult(403, "You don't have permission to edit this product");
            }

            ViewBag.ShopId = product.DukkanId;
            ViewBag.ShopName = shop.Name;
            
            // Prepare categories dropdown data
            var categories = _context.Products
                .Select(p => p.Category)
                .Where(c => !string.IsNullOrEmpty(c))
                .Distinct()
                .ToList();
                
            ViewBag.Categories = new SelectList(categories, product.Category);
            
            // Prepare brands dropdown data
            var brands = _context.Products
                .Select(p => p.Brand)
                .Where(b => !string.IsNullOrEmpty(b))
                .Distinct()
                .ToList();
                
            ViewBag.Brands = new SelectList(brands, product.Brand);
            
            return View(product);
        }

        // POST: ShopProduct/Edit/5 (5 is productId)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Edit/{id:int}")]
        public ActionResult Edit(Product product, HttpPostedFileBase productImage)
        {
            var userId = GetCurrentUserId();
            if (userId == -1)
            {
                return RedirectToAction("Login", "Account");
            }

            var existingProduct = _context.Products.FirstOrDefault(p => p.Id == product.Id);
            if (existingProduct == null)
            {
                return HttpNotFound("Product not found");
            }

            var shop = _context.Dukkans.FirstOrDefault(d => d.Id == existingProduct.DukkanId);
            if (shop == null)
            {
                return HttpNotFound("Shop not found");
            }

            // Verify user is owner of shop
            if (shop.OwnerId != userId)
            {
                return new HttpStatusCodeResult(403, "You don't have permission to edit this product");
            }

            // Ensure the product keeps its shop association
            product.DukkanId = existingProduct.DukkanId;

            if (ModelState.IsValid)
            {
                // Handle image upload
                if (productImage != null && productImage.ContentLength > 0)
                {
                    // Delete old image if exists
                    if (!string.IsNullOrEmpty(existingProduct.PhotoPath))
                    {
                        var oldPath = Server.MapPath("~" + existingProduct.PhotoPath);
                        if (System.IO.File.Exists(oldPath))
                        {
                            try
                            {
                                System.IO.File.Delete(oldPath);
                            }
                            catch (Exception ex)
                            {
                                // Log error but continue
                                System.Diagnostics.Debug.WriteLine($"Error deleting old image: {ex.Message}");
                            }
                        }
                    }

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
                existingProduct.Brand = product.Brand;
                existingProduct.Price = product.Price;
                existingProduct.Category = product.Category;
                existingProduct.Stock = product.Stock;
                existingProduct.IsClickAndCollect = product.IsClickAndCollect;

                _context.SaveChanges();

                TempData["Message"] = "Product updated successfully!";
                return RedirectToAction("List", new { shopId = existingProduct.DukkanId });
            }

            // If we got this far, something failed, redisplay form
            ViewBag.ShopId = existingProduct.DukkanId;
            ViewBag.ShopName = shop.Name;
            
            // Prepare categories dropdown data
            var categories = _context.Products
                .Select(p => p.Category)
                .Where(c => !string.IsNullOrEmpty(c))
                .Distinct()
                .ToList();
                
            ViewBag.Categories = new SelectList(categories, product.Category);
            
            // Prepare brands dropdown data
            var brands = _context.Products
                .Select(p => p.Brand)
                .Where(b => !string.IsNullOrEmpty(b))
                .Distinct()
                .ToList();
                
            ViewBag.Brands = new SelectList(brands, product.Brand);
            
            return View(product);
        }

        // POST: ShopProduct/Delete/5 (5 is productId)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Delete/{id:int}")]
        public ActionResult Delete(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == -1)
            {
                return RedirectToAction("Login", "Account");
            }

            var product = _context.Products.FirstOrDefault(p => p.Id == id);
            if (product == null)
            {
                return HttpNotFound("Product not found");
            }

            var shop = _context.Dukkans.FirstOrDefault(d => d.Id == product.DukkanId);
            if (shop == null)
            {
                return HttpNotFound("Shop not found");
            }

            // Verify user is owner of shop
            if (shop.OwnerId != userId)
            {
                return new HttpStatusCodeResult(403, "You don't have permission to delete this product");
            }

            int shopId = product.DukkanId.Value;
            
            // Delete the product image if exists
            if (!string.IsNullOrEmpty(product.PhotoPath))
            {
                try
                {
                    var imagePath = Server.MapPath("~" + product.PhotoPath);
                    if (System.IO.File.Exists(imagePath))
                    {
                        System.IO.File.Delete(imagePath);
                    }
                }
                catch (Exception ex)
                {
                    // Log error but continue
                    System.Diagnostics.Debug.WriteLine($"Error deleting product image: {ex.Message}");
                }
            }
            
            // Remove the product
            _context.Products.Remove(product);
            _context.SaveChanges();

            TempData["Message"] = "Product deleted successfully!";
            return RedirectToAction("List", new { shopId = shopId });
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