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

        // OrderViewModel sınıfını tanımlayalım
        public class OrderViewModel
        {
            public int Id { get; set; }
            public string OrderId { get; set; } // Gösterim için kullanılacak sipariş no
            public DateTime OrderDate { get; set; }
            public string RecipientName { get; set; }
            public string Email { get; set; }
            public string StatusText { get; set; }
            public decimal? TotalAmount { get; set; }
        }

        public ActionResult Index()
        {
            // Kullanıcı giriş yapmış mı kontrol et
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }
            
            // Admin rolünde mi kontrol et
            if (!User.IsInRole("Admin"))
            {
                return RedirectToAction("MyAccount", "Account");
            }

            try
            {
                ViewBag.TotalUsers = db.Users.Count();
                ViewBag.TotalProducts = productDb.Products.Count();
                ViewBag.TotalOrders = ordersDb.Orders.Count();
                ViewBag.TotalRevenue = ordersDb.Orders.Any() ? 
                    ordersDb.Orders.Sum(o => o.TotalAmount ?? 0) : 0;
                ViewBag.TotalShops = productDb.Dukkans.Count();

                return View();
            }
            catch (Exception ex)
            {
                // Hata durumunda logla
                System.Diagnostics.Debug.WriteLine($"Admin Index Hatası: {ex.Message}");
                
                // Kullanıcıya bir hata mesajı göster
                TempData["ErrorMessage"] = "Bir hata oluştu. Lütfen tekrar deneyin.";
                return RedirectToAction("MyAccount", "Account");
            }
        }

        public ActionResult ManageUsers()
        {
            try
            {
                // İzin kontrolü ekleyelim
                if (!User.IsInRole("Admin"))
                {
                    return RedirectToAction("AccessDenied", "Account");
                }
                
                var users = db.Users.ToList();
                return View(users);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ManageUsers Hatası: {ex.Message}");
                TempData["ErrorMessage"] = "Kullanıcılar listelenirken bir hata oluştu.";
                return RedirectToAction("Index");
            }
        }
        
        public ActionResult CreateDukkan()
        {
            return View();
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateDukkan(Dukkan dukkan)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    productDb.Dukkans.Add(dukkan);
                    productDb.SaveChanges();
                    
                    TempData["SuccessMessage"] = "Mağaza başarıyla oluşturuldu.";
                    return RedirectToAction("Index");
                }
                
                return View(dukkan);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CreateDukkan Hatası: {ex.Message}");
                TempData["ErrorMessage"] = "Mağaza oluşturulurken bir hata oluştu.";
                return View(dukkan);
            }
        }
        
        public ActionResult Orders()
        {
            try
            {
                var orders = ordersDb.Orders
                    .OrderByDescending(o => o.OrderDate)
                    .ToList();
                
                // OrderViewModel listesine dönüştür
                var orderViewModels = orders.Select(o => new OrderViewModel
                {
                    Id = o.Id,
                    OrderId = "ORD-" + o.Id.ToString("D6"), // Sipariş no formatı
                    OrderDate = o.OrderDate,
                    RecipientName = o.RecipientName ?? "Belirtilmemiş",
                    Email = o.User != null ? o.User.Email : "Belirtilmemiş",
                    StatusText = o.Status.ToString(),
                    TotalAmount = o.TotalAmount
                }).ToList();
                
                return View(orderViewModels);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Orders Hatası: {ex.Message}");
                TempData["ErrorMessage"] = "Siparişler listelenirken bir hata oluştu.";
                return RedirectToAction("Index");
            }
        }
        
        public ActionResult CreateBrand()
        {
            return View();
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateBrand(FormCollection form)
        {
            try
            {
                string brandName = form["BrandName"];
                
                if (string.IsNullOrWhiteSpace(brandName))
                {
                    ModelState.AddModelError("BrandName", "Marka adı gereklidir.");
                    return View();
                }
                
                // Yeni markayı ekle (örnek implementasyon - gerçek veritabanı entegrasyonu gerekebilir)
                TempData["SuccessMessage"] = $"{brandName} markası başarıyla oluşturuldu.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CreateBrand Hatası: {ex.Message}");
                TempData["ErrorMessage"] = "Marka oluşturulurken bir hata oluştu.";
                return View();
            }
        }
        
        public ActionResult CreateCategory()
        {
            return View();
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateCategory(FormCollection form)
        {
            try
            {
                string categoryName = form["CategoryName"];
                
                if (string.IsNullOrWhiteSpace(categoryName))
                {
                    ModelState.AddModelError("CategoryName", "Kategori adı gereklidir.");
                    return View();
                }
                
                // Yeni kategoriyi ekle (örnek implementasyon - gerçek veritabanı entegrasyonu gerekebilir)
                TempData["SuccessMessage"] = $"{categoryName} kategorisi başarıyla oluşturuldu.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CreateCategory Hatası: {ex.Message}");
                TempData["ErrorMessage"] = "Kategori oluşturulurken bir hata oluştu.";
                return View();
            }
        }
        
        public ActionResult AddProduct()
        {
            // Kategorileri ve markaları al
            ViewBag.Categories = new SelectList(productDb.Products.Select(p => p.Category).Distinct(), "Category");
            ViewBag.Brands = new SelectList(productDb.Products.Select(p => p.Brand).Distinct(), "Brand");
            
            return View();
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddProduct(Product product, HttpPostedFileBase productImage)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    if (productImage != null && productImage.ContentLength > 0)
                    {
                        // Dosya adını oluştur
                        var fileName = Guid.NewGuid().ToString() + System.IO.Path.GetExtension(productImage.FileName);
                        var path = System.IO.Path.Combine(Server.MapPath("~/Content/ProductImages"), fileName);
                        
                        // Dosyayı kaydet
                        productImage.SaveAs(path);
                        product.PhotoPath = "Content/ProductImages/" + fileName;
                    }
                    
                    // Ürünü veritabanına kaydet
                    productDb.Products.Add(product);
                    productDb.SaveChanges();
                    
                    TempData["SuccessMessage"] = "Ürün başarıyla eklendi.";
                    return RedirectToAction("Index");
                }
                
                // Validasyon hatası varsa - form alanlarını tekrar doldurmak için
                ViewBag.Categories = new SelectList(productDb.Products.Select(p => p.Category).Distinct(), "Category");
                ViewBag.Brands = new SelectList(productDb.Products.Select(p => p.Brand).Distinct(), "Brand");
                
                return View(product);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AddProduct Hatası: {ex.Message}");
                TempData["ErrorMessage"] = "Ürün eklenirken bir hata oluştu.";
                
                ViewBag.Categories = new SelectList(productDb.Products.Select(p => p.Category).Distinct(), "Category");
                ViewBag.Brands = new SelectList(productDb.Products.Select(p => p.Brand).Distinct(), "Brand");
                
                return View(product);
            }
        }
        
        public ActionResult EditProduct(int id)
        {
            try
            {
                var product = productDb.Products.Find(id);
                
                if (product == null)
                {
                    TempData["ErrorMessage"] = "Ürün bulunamadı.";
                    return RedirectToAction("Index");
                }
                
                ViewBag.Categories = new SelectList(productDb.Products.Select(p => p.Category).Distinct(), "Category", "Category", product.Category);
                ViewBag.Brands = new SelectList(productDb.Products.Select(p => p.Brand).Distinct(), "Brand", "Brand", product.Brand);
                
                return View(product);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EditProduct Hatası: {ex.Message}");
                TempData["ErrorMessage"] = "Ürün düzenleme sayfası yüklenirken bir hata oluştu.";
                return RedirectToAction("Index");
            }
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditProduct(Product product, HttpPostedFileBase productImage)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var existingProduct = productDb.Products.Find(product.Id);
                    
                    if (existingProduct == null)
                    {
                        TempData["ErrorMessage"] = "Ürün bulunamadı.";
                        return RedirectToAction("Index");
                    }
                    
                    // Ürün bilgilerini güncelle
                    existingProduct.Name = product.Name;
                    existingProduct.Description = product.Description;
                    existingProduct.Price = product.Price;
                    existingProduct.Stock = product.Stock;
                    existingProduct.Category = product.Category;
                    existingProduct.Brand = product.Brand;
                    existingProduct.IsActive = product.IsActive;
                    
                    // Yeni bir resim varsa yükle
                    if (productImage != null && productImage.ContentLength > 0)
                    {
                        var fileName = Guid.NewGuid().ToString() + System.IO.Path.GetExtension(productImage.FileName);
                        var path = System.IO.Path.Combine(Server.MapPath("~/Content/ProductImages"), fileName);
                        
                        productImage.SaveAs(path);
                        existingProduct.PhotoPath = "Content/ProductImages/" + fileName;
                    }
                    
                    // Değişiklikleri kaydet
                    productDb.SaveChanges();
                    
                    TempData["SuccessMessage"] = "Ürün başarıyla güncellendi.";
                    return RedirectToAction("Index");
                }
                
                ViewBag.Categories = new SelectList(productDb.Products.Select(p => p.Category).Distinct(), "Category", "Category", product.Category);
                ViewBag.Brands = new SelectList(productDb.Products.Select(p => p.Brand).Distinct(), "Brand", "Brand", product.Brand);
                
                return View(product);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EditProduct Hatası: {ex.Message}");
                TempData["ErrorMessage"] = "Ürün güncellenirken bir hata oluştu.";
                
                ViewBag.Categories = new SelectList(productDb.Products.Select(p => p.Category).Distinct(), "Category", "Category", product.Category);
                ViewBag.Brands = new SelectList(productDb.Products.Select(p => p.Brand).Distinct(), "Brand", "Brand", product.Brand);
                
                return View(product);
            }
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

