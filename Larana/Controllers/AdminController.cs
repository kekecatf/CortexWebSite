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
            System.Diagnostics.Debug.WriteLine($"UpdateUser çağrıldı. ID={id}");

            try
            {
                // Validate the ID parameter
                if (id <= 0)
                {
                    if (Request.IsAjaxRequest())
                    {
                        return Json(new { success = false, message = "Geçersiz kullanıcı ID'si." });
                    }
                    
                    TempData["ErrorMessage"] = "Geçersiz kullanıcı ID.";
                    return RedirectToAction("ManageUsers");
                }

                // 1. Güncellenmek istenen kullanıcıyı bul
                var userToUpdate = db.Users.Find(id);
                if (userToUpdate == null)
                {
                    if (Request.IsAjaxRequest())
                    {
                        return Json(new { success = false, message = "Güncellenecek kullanıcı bulunamadı." });
                    }
                    
                    TempData["ErrorMessage"] = "Güncellenecek kullanıcı bulunamadı.";
                    return RedirectToAction("ManageUsers");
                }

                // 2. İşlemi yapan admin kullanıcısını bul (basit yöntemle)
                User currentAdmin = null;
                string adminUsername = User.Identity?.Name;

                if (string.IsNullOrEmpty(adminUsername))
                {
                    if (Request.IsAjaxRequest())
                    {
                        return Json(new { success = false, message = "Oturum bilgisi alınamadı." });
                    }
                    
                    TempData["ErrorMessage"] = "Oturum bilgisi alınamadı.";
                    return RedirectToAction("ManageUsers");
                }

                System.Diagnostics.Debug.WriteLine($"Authenticated user: {adminUsername}");

                // Kullanıcıyı veritabanında bul
                try
                {
                    currentAdmin = db.Users.FirstOrDefault(u => u.Username == adminUsername);

                    if (currentAdmin == null)
                    {
                        if (Request.IsAjaxRequest())
                        {
                            return Json(new { success = false, message = "Yönetici bilgisi bulunamadı." });
                        }
                        
                        System.Diagnostics.Debug.WriteLine("Admin user not found in database");
                        TempData["ErrorMessage"] = "Yönetici bilgisi bulunamadı.";
                        return RedirectToAction("ManageUsers");
                    }
                }
                catch (Exception ex)
                {
                    // Log exception
                    System.Diagnostics.Debug.WriteLine($"Error finding current admin: {ex.Message}");
                    
                    if (Request.IsAjaxRequest())
                    {
                        return Json(new { success = false, message = "Yönetici bilgisi alınırken hata oluştu: " + ex.Message });
                    }
                    
                    TempData["ErrorMessage"] = "Yönetici bilgisi alınırken hata oluştu.";
                    return RedirectToAction("ManageUsers");
                }

                // 3. Kendini güncelliyor mu kontrol et?
                bool isSelfUpdate = currentAdmin.Id == id;
                if (isSelfUpdate)
                {
                    System.Diagnostics.Debug.WriteLine("Self-update detected!");
                }

                // 4. Gerekli güncellemeleri yap
                // Temel bilgiler her zaman güncellenebilir
                if (!string.IsNullOrEmpty(username))
                {
                    userToUpdate.Username = username;
                }

                if (!string.IsNullOrEmpty(email))
                {
                    userToUpdate.Email = email;
                }

                // 5. Kendi hesabını güncelliyorsa rolü ve durumu değiştirmeye izin verme
                if (!isSelfUpdate)
                {
                    System.Diagnostics.Debug.WriteLine("Updating role/status of another user");

                    // UserType işlemi
                    if (!string.IsNullOrEmpty(userType))
                    {
                        try
                        {
                            // string -> enum dönüşümü - format hatası olasılığı var
                            UserType type;
                            if (Enum.TryParse(userType, true, out type))
                            {
                                userToUpdate.UserType = type;

                                // Buna bağlı olarak Role ve Roles alanlarını da güncelle
                                switch (type)
                                {
                                    case UserType.Admin:
                                        userToUpdate.Role = UserRole.Admin;
                                        userToUpdate.Roles = "Admin";
                                        break;
                                    case UserType.Seller:
                                        userToUpdate.Role = UserRole.Seller;
                                        userToUpdate.Roles = "Seller";
                                        break;
                                    default:
                                    case UserType.Customer:
                                        userToUpdate.Role = UserRole.Customer;
                                        userToUpdate.Roles = "Customer";
                                        break;
                                }
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"Failed to parse user type: {userType}");
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error updating user type: {ex.Message}");
                            // Hata oluştu ama işleme devam et
                        }
                    }

                    // Aktiflik durumu
                    userToUpdate.IsActive = isActive;
                }

                // 6. Değişiklikleri kaydet ve sonlandır
                db.SaveChanges();

                if (Request.IsAjaxRequest())
                {
                    var result = new { 
                        success = true, 
                        message = isSelfUpdate 
                            ? "Profil başarıyla güncellendi." 
                            : "Kullanıcı başarıyla güncellendi.",
                        userData = new {
                            id = userToUpdate.Id,
                            username = userToUpdate.Username,
                            email = userToUpdate.Email,
                            userType = userToUpdate.UserType.ToString(),
                            isActive = userToUpdate.IsActive
                        }
                    };
                    
                    return Json(result);
                }
                
                if (isSelfUpdate)
                {
                    TempData["SuccessMessage"] = "Profil başarıyla güncellendi. Not: Yöneticiler kendi rollerini veya durumlarını değiştiremezler.";
                }
                else
                {
                    TempData["SuccessMessage"] = "Kullanıcı başarıyla güncellendi.";
                }
            }
            catch (Exception ex)
            {
                // Genel hata durumu
                System.Diagnostics.Debug.WriteLine($"Error updating user: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                
                if (Request.IsAjaxRequest())
                {
                    return Json(new { success = false, message = "Kullanıcı güncellenirken bir hata oluştu: " + ex.Message });
                }
                
                TempData["ErrorMessage"] = "Kullanıcı güncellenirken bir hata oluştu: " + ex.Message;
            }

            return RedirectToAction("ManageUsers");
        }
        
        [HttpPost]
        public ActionResult DeleteUser(int id)
        {
            System.Diagnostics.Debug.WriteLine($"DeleteUser çağrıldı. ID={id}");
            
            try
            {
                // Parametre kontrolü
                if (id <= 0)
                {
                    if (Request.IsAjaxRequest())
                    {
                        return Json(new { success = false, message = "Geçersiz kullanıcı ID'si (0 veya negatif değer)." });
                    }
                    
                    TempData["ErrorMessage"] = "Geçersiz kullanıcı ID'si (0 veya negatif değer).";
                    return RedirectToAction("ManageUsers");
                }
                
                // 1. Silinmek istenen kullanıcıyı bul 
                var userToDelete = db.Users.Find(id);
                if (userToDelete == null)
                {
                    if (Request.IsAjaxRequest())
                    {
                        return Json(new { success = false, message = "Silinecek kullanıcı bulunamadı." });
                    }
                    
                    TempData["ErrorMessage"] = "Silinecek kullanıcı bulunamadı.";
                    return RedirectToAction("ManageUsers");
                }
                
                // 2. İşlemi yapan admin kullanıcısını bul (basit yöntemle)
                User currentAdmin = null;
                string adminUsername = User.Identity?.Name;
                
                if (string.IsNullOrEmpty(adminUsername))
                {
                    if (Request.IsAjaxRequest())
                    {
                        return Json(new { success = false, message = "Oturum bilgisi alınamadı." });
                    }
                    
                    TempData["ErrorMessage"] = "Oturum bilgisi alınamadı.";
                    return RedirectToAction("ManageUsers");
                }
                
                System.Diagnostics.Debug.WriteLine($"Authenticated user: {adminUsername}");
                
                // Kullanıcıyı veritabanında bul
                try
                {
                    currentAdmin = db.Users.FirstOrDefault(u => u.Username == adminUsername);
                    
                    if (currentAdmin == null)
                    {
                        if (Request.IsAjaxRequest())
                        {
                            return Json(new { success = false, message = "Yönetici bilgisi bulunamadı." });
                        }
                        
                        System.Diagnostics.Debug.WriteLine("Admin user not found in database");
                        TempData["ErrorMessage"] = "Yönetici bilgisi bulunamadı.";
                        return RedirectToAction("ManageUsers");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error finding current admin: {ex.Message}");
                    
                    if (Request.IsAjaxRequest())
                    {
                        return Json(new { success = false, message = "Yönetici bilgisi alınırken hata oluştu: " + ex.Message });
                    }
                    
                    TempData["ErrorMessage"] = "Yönetici bilgisi alınırken hata oluştu.";
                    return RedirectToAction("ManageUsers");
                }
                
                // 3. Kendini silmeye çalışıyor mu kontrol et?
                if (currentAdmin.Id == id)
                {
                    System.Diagnostics.Debug.WriteLine("Self-delete attempt detected and prevented");
                    
                    if (Request.IsAjaxRequest())
                    {
                        return Json(new { success = false, message = "Yöneticiler kendi hesaplarını silemezler." });
                    }
                    
                    TempData["ErrorMessage"] = "Yöneticiler kendi hesaplarını silemezler.";
                    return RedirectToAction("ManageUsers");
                }
                
                // 4. Silme işleminden önce ilişkili kayıtları kontrol et
                try
                {
                    // Kullanıcının siparişlerini kontrol et
                    var userOrders = ordersDb.Orders.Where(o => o.UserId == id).ToList();
                    if (userOrders.Any())
                    {
                        System.Diagnostics.Debug.WriteLine($"Kullanıcının {userOrders.Count} siparişi var!");
                        
                        // Sipariş referansı olan sepetler olduğu için, siparişleri silmeden kullanıcıyı silemeyiz
                        if (Request.IsAjaxRequest())
                        {
                            return Json(new { success = false, message = $"Bu kullanıcı {userOrders.Count} siparişe sahip. Kullanıcıyı silmeden önce siparişleri silmeli veya başka kullanıcıya transfer etmelisiniz." });
                        }
                        
                        TempData["ErrorMessage"] = $"Bu kullanıcı {userOrders.Count} siparişe sahip. Kullanıcıyı silmeden önce siparişleri silmeli veya başka kullanıcıya transfer etmelisiniz.";
                        return RedirectToAction("ManageUsers");
                    }
                    
                    // Kullanıcının sepetlerini al ve sil (siparişlerden sonra silmeliyiz)
                    var userCarts = productDb.Carts.Where(c => c.UserId == id).ToList();
                    if (userCarts.Any())
                    {
                        System.Diagnostics.Debug.WriteLine($"Kullanıcının {userCarts.Count} sepeti var, bunlar siliniyor...");
                        // Sepetleri sil
                        foreach (var cart in userCarts)
                        {
                            productDb.Carts.Remove(cart);
                        }
                        productDb.SaveChanges();
                    }
                    
                    // Kullanıcının dükkanlarını al
                    var userDukkans = productDb.Dukkans.Where(d => d.OwnerId == id).ToList();
                    if (userDukkans.Any())
                    {
                        System.Diagnostics.Debug.WriteLine($"Kullanıcının {userDukkans.Count} dükkanı var, bunlar silinemiyor!");
                        TempData["ErrorMessage"] = $"Bu kullanıcı {userDukkans.Count} dükkana sahip. Önce dükkanları silmeli veya başka kullanıcıya transfer etmelisiniz.";
                        return RedirectToAction("ManageUsers");
                    }
                    
                    // Kullanıcının adına kayıtlı rating/değerlendirmeler varsa kontrol et
                    var userRatings = productDb.Ratings.Where(r => r.UserId == id).ToList();
                    if (userRatings.Any())
                    {
                        System.Diagnostics.Debug.WriteLine($"Kullanıcının {userRatings.Count} değerlendirmesi var, bunlar siliniyor...");
                        // Değerlendirmeleri sil
                        foreach (var rating in userRatings)
                        {
                            productDb.Ratings.Remove(rating);
                        }
                        productDb.SaveChanges();
                    }
                    
                    // 5. Silme işlemini gerçekleştir
                    db.Users.Remove(userToDelete);
                    db.SaveChanges();
                    
                    if (Request.IsAjaxRequest())
                    {
                        return Json(new { success = true, message = "Kullanıcı başarıyla silindi." });
                    }
                    
                    TempData["SuccessMessage"] = "Kullanıcı başarıyla silindi.";
                }
                catch (System.Data.Entity.Infrastructure.DbUpdateException ex)
                {
                    // Entity Framework DbUpdateException'ı özel olarak yakala
                    var innerException = ex.InnerException;
                    string errorDetails = "İç hata: ";
                    
                    while (innerException != null)
                    {
                        errorDetails += innerException.Message + " "; 
                        innerException = innerException.InnerException;
                    }
                    
                    System.Diagnostics.Debug.WriteLine("Veritabanı güncelleme hatası: " + ex.Message);
                    System.Diagnostics.Debug.WriteLine("İç hata detayları: " + errorDetails);
                    
                    if (Request.IsAjaxRequest())
                    {
                        return Json(new { success = false, message = "Veritabanı güncelleme hatası: " + errorDetails });
                    }
                    
                    TempData["ErrorMessage"] = "Kullanıcı silinirken bir hata oluştu: " + errorDetails;
                }
            }
            catch (Exception ex)
            {
                // Genel hata durumu
                System.Diagnostics.Debug.WriteLine($"Error deleting user: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner exception: {ex.InnerException.Message}");
                    System.Diagnostics.Debug.WriteLine($"Inner stack trace: {ex.InnerException.StackTrace}");
                }
                
                if (Request.IsAjaxRequest())
                {
                    return Json(new { success = false, message = "Kullanıcı silinirken bir hata oluştu: " + GetAllInnerExceptionMessages(ex) });
                }
                
                TempData["ErrorMessage"] = "Kullanıcı silinirken bir hata oluştu: " + GetAllInnerExceptionMessages(ex);
            }
            
            return RedirectToAction("ManageUsers");
        }
        
        private string GetAllInnerExceptionMessages(Exception ex)
        {
            var message = ex.Message;
            var innerEx = ex.InnerException;
            
            while (innerEx != null)
            {
                message += " İç hata: " + innerEx.Message;
                innerEx = innerEx.InnerException;
            }
            
            return message;
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

