using System;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;
using Larana.Data;
using Larana.Models;

namespace Larana.Controllers
{
    [Authorize]
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductController()
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

        // GET: Product/Create
        [Authorize(Roles = "Admin,Seller")]
        [Route("Product/Create/{id:int}")]
        public ActionResult Create(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == -1)
            {
                return RedirectToAction("Login", "Account");
            }

            var dukkan = _context.Dukkans.FirstOrDefault(d => d.Id == id);
            if (dukkan == null)
            {
                return HttpNotFound("Shop not found");
            }

            // Verify user is owner of shop or admin
            if (dukkan.OwnerId != userId && !User.IsInRole("Admin"))
            {
                return new HttpStatusCodeResult(403, "You don't have permission to add products to this shop");
            }

            ViewBag.DukkanId = id;
            ViewBag.DukkanName = dukkan.Name;
            
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
                DukkanId = id,
                Stock = 0,
                IsClickAndCollect = false
            };

            return View(product);
        }

        // Also support traditional routing for backward compatibility
        [Authorize(Roles = "Admin,Seller")]
        public ActionResult Create(int? id)
        {
            if (!id.HasValue)
            {
                return HttpNotFound("Shop ID is required");
            }
            return Create(id.Value);
        }

        // POST: Product/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Seller")]
        [Route("Product/Create/{id:int}")]
        public ActionResult Create(Product product, HttpPostedFileBase productImage, int id)
        {
            var userId = GetCurrentUserId();
            if (userId == -1)
            {
                return RedirectToAction("Login", "Account");
            }

            // Ensure the DukkanId is set from the route value
            if (product.DukkanId == 0 || product.DukkanId == null)
            {
                product.DukkanId = id;
            }

            var dukkan = _context.Dukkans.FirstOrDefault(d => d.Id == product.DukkanId);
            if (dukkan == null)
            {
                return HttpNotFound("Shop not found");
            }

            // Verify user is owner of shop or admin
            if (dukkan.OwnerId != userId && !User.IsInRole("Admin"))
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
                    return RedirectToAction("Products", "Dukkan", new { id = product.DukkanId });
                }
                catch (Exception ex)
                {
                    // Log the error and add a user-friendly message
                    System.Diagnostics.Debug.WriteLine($"Error adding product: {ex.Message}");
                    ModelState.AddModelError("", "An error occurred while adding the product. Please try again.");
                }
            }
            else
            {
                // Log validation errors for debugging
                foreach (var state in ModelState)
                {
                    foreach (var error in state.Value.Errors)
                    {
                        System.Diagnostics.Debug.WriteLine($"Validation error for {state.Key}: {error.ErrorMessage}");
                    }
                }
            }

            // If we got this far, something failed, redisplay form
            ViewBag.DukkanId = product.DukkanId;
            ViewBag.DukkanName = dukkan.Name;
            
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

        // Also support traditional routing for backward compatibility
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Seller")]
        public ActionResult Create(Product product, HttpPostedFileBase productImage)
        {
            return Create(product, productImage, product.DukkanId ?? 0);
        }

        // GET: Product/Edit/5
        [Authorize(Roles = "Admin,Seller")]
        [Route("Product/Edit/{id:int}")]
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

            var dukkan = _context.Dukkans.FirstOrDefault(d => d.Id == product.DukkanId);
            if (dukkan == null)
            {
                return HttpNotFound("Shop not found");
            }

            // Verify user is owner of shop or admin
            if (dukkan.OwnerId != userId && !User.IsInRole("Admin"))
            {
                return new HttpStatusCodeResult(403, "You don't have permission to edit products in this shop");
            }

            ViewBag.DukkanId = product.DukkanId;
            ViewBag.DukkanName = dukkan.Name;
            
            // Prepare categories dropdown data
            ViewBag.Categories = new SelectList(
                _context.Products
                    .Select(p => p.Category)
                    .Where(c => !string.IsNullOrEmpty(c))
                    .Distinct()
                    .ToList(),
                product.Category
            );
            
            // Prepare brands dropdown data
            ViewBag.Brands = new SelectList(
                _context.Products
                    .Select(p => p.Brand)
                    .Where(b => !string.IsNullOrEmpty(b))
                    .Distinct()
                    .ToList(),
                product.Brand
            );

            return View(product);
        }

        // POST: Product/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Seller")]
        [Route("Product/Edit/{id:int}")]
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

            var dukkan = _context.Dukkans.FirstOrDefault(d => d.Id == existingProduct.DukkanId);
            if (dukkan == null)
            {
                return HttpNotFound("Shop not found");
            }

            // Verify user is owner of shop or admin
            if (dukkan.OwnerId != userId && !User.IsInRole("Admin"))
            {
                return new HttpStatusCodeResult(403, "You don't have permission to edit products in this shop");
            }

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
                return RedirectToAction("Products", "Dukkan", new { id = existingProduct.DukkanId });
            }

            // If we got this far, something failed, redisplay form
            ViewBag.DukkanId = existingProduct.DukkanId;
            ViewBag.DukkanName = dukkan.Name;
            
            // Prepare categories dropdown data
            ViewBag.Categories = new SelectList(
                _context.Products
                    .Select(p => p.Category)
                    .Where(c => !string.IsNullOrEmpty(c))
                    .Distinct()
                    .ToList(),
                product.Category
            );
            
            // Prepare brands dropdown data
            ViewBag.Brands = new SelectList(
                _context.Products
                    .Select(p => p.Brand)
                    .Where(b => !string.IsNullOrEmpty(b))
                    .Distinct()
                    .ToList(),
                product.Brand
            );
            
            return View(product);
        }

        // GET: Product/Delete/5
        [Authorize(Roles = "Admin,Seller")]
        [Route("Product/Delete/{id:int}")]
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

            var dukkan = _context.Dukkans.FirstOrDefault(d => d.Id == product.DukkanId);
            if (dukkan == null)
            {
                return HttpNotFound("Shop not found");
            }

            // Verify user is owner of shop or admin
            if (dukkan.OwnerId != userId && !User.IsInRole("Admin"))
            {
                return new HttpStatusCodeResult(403, "You don't have permission to delete products from this shop");
            }
            
            // Delete the product
            _context.Products.Remove(product);
            _context.SaveChanges();
            
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

            TempData["Message"] = "Product deleted successfully!";
            return RedirectToAction("Products", "Dukkan", new { id = product.DukkanId });
        }
        
        [AllowAnonymous]
        public ActionResult Details(int id)
        {
            // Get the main product
            var product = _context.Products.FirstOrDefault(p => p.Id == id);
            if (product == null)
            {
                return HttpNotFound("Product not found");
            }

            // Get all stores that sell this product
            var storeProducts = _context.ShopProducts
                .Where(sp => sp.ProductId == id)
                .Include(sp => sp.Dukkan)
                .ToList();

            // Create a viewmodel with the product and store options
            var viewModel = new ProductDetailsViewModel
            {
                Product = product,
                StoreOptions = storeProducts.Select(sp => new StoreProductOption
                {
                    StoreId = sp.DukkanId,
                    StoreName = sp.Dukkan.Name,
                    Price = sp.Price,
                    Stock = sp.Stock,
                    IsClickAndCollect = sp.IsClickAndCollect,
                    ProductId = sp.ProductId,
                    ShopProductId = sp.Id
                }).ToList()
            };

            // Add the original store to options if not already included
            if (product.DukkanId.HasValue && !viewModel.StoreOptions.Any(so => so.StoreId == product.DukkanId.Value))
            {
                var originalStore = _context.Dukkans.FirstOrDefault(d => d.Id == product.DukkanId.Value);
                if (originalStore != null)
                {
                    viewModel.StoreOptions.Add(new StoreProductOption
                    {
                        StoreId = originalStore.Id,
                        StoreName = originalStore.Name,
                        Price = product.Price,
                        Stock = product.Stock,
                        IsClickAndCollect = product.IsClickAndCollect,
                        ProductId = product.Id,
                        ShopProductId = null // This indicates it's from the original product
                    });
                }
            }

            return View(viewModel);
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