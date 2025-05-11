using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Larana.Models;
using Larana.Data;

namespace Larana.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private DbAccount db = new DbAccount();
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

        public ActionResult Orders()
        {
            var orders = ordersDb.Orders
                .Select(o => new OrderViewModel
                {
                    Id = o.Id,
                    UserId = o.UserId.ToString(),
                    Address = o.Address,
                    PhoneNumber = o.PhoneNumber,
                    RecipientName = o.RecipientName,
                    ShippingCompany = o.ShippingCompany,
                    OrderDate = o.OrderDate,
                    TotalAmount = o.TotalAmount,
                    Status = o.Status.ToString(),
                    OrderDetails = o.OrderDetails.Select(od => new OrderDetailViewModel
                    {
                        ProductId = od.ProductId,
                        Quantity = od.Quantity,
                        UnitPrice = od.UnitPrice
                    }).ToList()
                }).ToList();

            var productIds = orders.SelectMany(o => o.OrderDetails).Select(d => d.ProductId).Distinct().ToList();
            var productDetails = productDb.Products
                .Where(p => productIds.Contains(p.Id))
                .ToDictionary(p => p.Id, p => new { p.Name, p.PhotoPath });

            foreach (var order in orders)
            {
                foreach (var detail in order.OrderDetails)
                {
                    if (productDetails.TryGetValue(detail.ProductId, out var product))
                    {
                        detail.ProductName = product.Name;
                        detail.PhotoPath = product.PhotoPath;
                    }
                    else
                    {
                        detail.ProductName = "Unknown Product";
                        detail.PhotoPath = "/images/default-product.png";
                    }
                }
            }

            return View(orders);
        }

        public ActionResult EditProduct(int id)
        {
            var product = productDb.Products.FirstOrDefault(p => p.Id == id);
            if (product == null)
            {
                TempData["ErrorMessage"] = "Product not found.";
                return RedirectToAction("Index");
            }
            
            // Get all unique brands and categories from existing products
            var brands = productDb.Products
                .Where(p => !string.IsNullOrEmpty(p.Brand))
                .Select(p => p.Brand)
                .Distinct()
                .OrderBy(b => b)
                .ToList();
                
            var categories = productDb.Products
                .Where(p => !string.IsNullOrEmpty(p.Category))
                .Select(p => p.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToList();
                
            ViewBag.BrandsList = string.Join(",", brands);
            ViewBag.CategoriesList = string.Join(",", categories);
                
            return View(product);
        }

        [HttpPost]
        public ActionResult EditProduct(Product product, string actionType)
        {
            var existingProduct = productDb.Products.FirstOrDefault(p => p.Id == product.Id);
            if (existingProduct == null)
            {
                TempData["ErrorMessage"] = "Product not found.";
                return RedirectToAction("Index");
            }

            try
            {
                switch (actionType)
                {
                    case "UpdateDetails":
                        // Remove validation for fields not being updated
                        ModelState.Remove("Stock");
                        ModelState.Remove("Price");
                        ModelState.Remove("PhotoPath");
                        
                        if (ModelState.IsValid)
                        {
                            existingProduct.Name = product.Name;
                            existingProduct.Brand = product.Brand;
                            existingProduct.Category = product.Category;
                            productDb.SaveChanges();
                            TempData["SuccessMessage"] = "Product details updated successfully!";
                        }
                        else
                        {
                            var errors = ModelState.Values.SelectMany(v => v.Errors)
                                                  .Select(e => e.ErrorMessage)
                                                  .ToList();
                            TempData["ErrorMessage"] = "Validation Errors: " + string.Join(", ", errors);
                        }
                        break;
                        
                    case "UpdateStockPrice":
                        // Remove non-required fields from validation
                        ModelState.Remove("Name");
                        ModelState.Remove("Brand");
                        ModelState.Remove("Category");
                        ModelState.Remove("Description");
                        ModelState.Remove("PhotoPath");

                        if (ModelState.IsValid)
                        {
                            existingProduct.Stock = product.Stock;
                            existingProduct.Price = product.Price;
                            existingProduct.IsClickAndCollect = product.IsClickAndCollect;
                            productDb.SaveChanges();
                            TempData["SuccessMessage"] = "Product updated successfully!";
                        }
                        else
                        {
                            var errors = ModelState.Values.SelectMany(v => v.Errors)
                                                  .Select(e => e.ErrorMessage)
                                                  .ToList();
                            TempData["ErrorMessage"] = "Validation Errors: " + string.Join(", ", errors);
                        }
                        break;

                    case "DeleteProduct":
                        productDb.Products.Remove(existingProduct);
                        productDb.SaveChanges();
                        TempData["SuccessMessage"] = "Product deleted successfully!";
                        return RedirectToAction("Index");

                    default:
                        TempData["ErrorMessage"] = "Invalid action type.";
                        break;
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An unexpected error occurred: {ex.Message}";
            }

            return RedirectToAction("EditProduct", new { id = product.Id });
        }

        [HttpPost]
        public ActionResult UpdateOrderStatus(int orderId, string newStatus)
        {
            var order = ordersDb.Orders.FirstOrDefault(o => o.Id == orderId);
            if (order != null)
            {
                if (Enum.TryParse<OrderStatus>(newStatus, out var status))
                {
                    order.Status = status;
                    ordersDb.SaveChanges();
                }
            }

            return RedirectToAction("Orders");
        }

        public ActionResult AddProduct()
        {
            // Get all unique brands and categories from existing products
            var brands = productDb.Products
                .Where(p => !string.IsNullOrEmpty(p.Brand))
                .Select(p => p.Brand)
                .Distinct()
                .OrderBy(b => b)
                .ToList();
                
            var categories = productDb.Products
                .Where(p => !string.IsNullOrEmpty(p.Category))
                .Select(p => p.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToList();
                
            ViewBag.BrandsList = string.Join(",", brands);
            ViewBag.CategoriesList = string.Join(",", categories);
                
            return View(new Product());
        }

        [HttpPost]
        public ActionResult AddProduct(Product product)
        {
            if (ModelState.IsValid)
            {
                product.PhotoPath = "img/products/" + product.PhotoPath;

                productDb.Products.Add(product);
                productDb.SaveChanges();

                TempData["SuccessMessage"] = "Product added successfully!";
                return RedirectToAction("Index");
            }

            // If we got this far, redisplay form
            var brands = productDb.Products
                .Where(p => !string.IsNullOrEmpty(p.Brand))
                .Select(p => p.Brand)
                .Distinct()
                .OrderBy(b => b)
                .ToList();
                
            var categories = productDb.Products
                .Where(p => !string.IsNullOrEmpty(p.Category))
                .Select(p => p.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToList();
                
            ViewBag.BrandsList = string.Join(",", brands);
            ViewBag.CategoriesList = string.Join(",", categories);

            return View(product);
        }

        public ActionResult ManageUsers()
        {
            var users = db.Users.ToList();
            return View(users);
        }

        [HttpPost]
        public ActionResult UpdateUser(int id, string username, string email, string userType, bool isActive)
        {
            var user = db.Users.Find(id);
            if (user != null)
            {
                user.Username = username;
                user.Email = email;
                
                if (Enum.TryParse<UserType>(userType, out var type))
                {
                    user.UserType = type;
                    
                    // Update role based on user type
                    if (type == UserType.Admin)
                    {
                        user.Role = UserRole.Admin;
                        user.Roles = "Admin";
                    }
                    else if (type == UserType.Seller)
                    {
                        user.Role = UserRole.Seller;
                        user.Roles = "Seller";
                    }
                    else
                    {
                        user.Role = UserRole.Customer;
                        user.Roles = "Customer";
                    }
                }
                
                user.IsActive = isActive;
                db.SaveChanges();
                
                TempData["SuccessMessage"] = "User updated successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "User not found.";
            }
            
            return RedirectToAction("ManageUsers");
        }
        
        [HttpPost]
        public ActionResult DeleteUser(int id)
        {
            var user = db.Users.Find(id);
            if (user != null)
            {
                try
                {
                    db.Users.Remove(user);
                    db.SaveChanges();
                    TempData["SuccessMessage"] = "User deleted successfully.";
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Unable to delete user: {ex.Message}";
                }
            }
            else
            {
                TempData["ErrorMessage"] = "User not found.";
            }
            
            return RedirectToAction("ManageUsers");
        }

        [HttpPost]
        public ActionResult AddNewBrand(string brandName, string returnUrl)
        {
            if (!string.IsNullOrEmpty(brandName))
            {
                TempData["NewBrand"] = brandName;
                TempData["SuccessMessage"] = $"Brand '{brandName}' added successfully!";
            }
            
            return Redirect(returnUrl);
        }
        
        [HttpPost]
        public ActionResult AddNewCategory(string categoryName, string returnUrl)
        {
            if (!string.IsNullOrEmpty(categoryName))
            {
                TempData["NewCategory"] = categoryName;
                TempData["SuccessMessage"] = $"Category '{categoryName}' added successfully!";
            }
            
            return Redirect(returnUrl);
        }

        public ActionResult CreateBrand()
        {
            return View();
        }
        
        [HttpPost]
        public ActionResult CreateBrand(string brandName)
        {
            if (!string.IsNullOrEmpty(brandName))
            {
                // Create a dummy product with this brand to establish it in the database
                var product = new Product
                {
                    Name = "Brand Placeholder - " + brandName,
                    Brand = brandName,
                    Category = "Placeholder",
                    PhotoPath = "img/products/placeholder.jpg",
                    Price = 0.01m,
                    Stock = 0,
                    IsActive = false
                };
                
                productDb.Products.Add(product);
                productDb.SaveChanges();
                
                TempData["SuccessMessage"] = $"Brand '{brandName}' has been added successfully!";
                return RedirectToAction("Index");
            }
            
            TempData["ErrorMessage"] = "Please provide a brand name.";
            return View();
        }
        
        public ActionResult CreateCategory()
        {
            return View();
        }
        
        [HttpPost]
        public ActionResult CreateCategory(string categoryName)
        {
            if (!string.IsNullOrEmpty(categoryName))
            {
                // Create a dummy product with this category to establish it in the database
                var product = new Product
                {
                    Name = "Category Placeholder - " + categoryName,
                    Brand = "Placeholder",
                    Category = categoryName,
                    PhotoPath = "img/products/placeholder.jpg",
                    Price = 0.01m,
                    Stock = 0,
                    IsActive = false
                };
                
                productDb.Products.Add(product);
                productDb.SaveChanges();
                
                TempData["SuccessMessage"] = $"Category '{categoryName}' has been added successfully!";
                return RedirectToAction("Index");
            }
            
            TempData["ErrorMessage"] = "Please provide a category name.";
            return View();
        }

        public class OrderDetailViewModel
        {
            public int ProductId { get; set; }
            public string ProductName { get; set; }
            public string PhotoPath { get; set; }
            public int Quantity { get; set; }
            public decimal UnitPrice { get; set; }
        }

        public class OrderViewModel
        {
            public int Id { get; set; }
            public string UserId { get; set; }
            public string RecipientName { get; set; }
            public string ShippingCompany { get; set; }
            public string Address { get; set; }
            public string PhoneNumber { get; set; }
            public DateTime OrderDate { get; set; }
            public decimal? TotalAmount { get; set; }
            public string Status { get; set; }
            public bool IsClickAndCollect { get; set; }
            public List<OrderDetailViewModel> OrderDetails { get; set; }
        }
    }
}
