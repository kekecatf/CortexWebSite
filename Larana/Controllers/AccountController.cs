using System;
using System.Linq;
using System.Web.Mvc;
using System.Web.Security;
using Larana.Models;
using Larana.Data;
using System.Collections.Generic;

namespace Larana.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        [HttpGet]
        [AllowAnonymous]
        public ActionResult Login()
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("MyAccount", "Account");
            }
            return View();
        }

        [HttpGet]
        [Authorize]
        public ActionResult BecomeASeller()
        {
            var username = User.Identity.Name;
            var user = db.Users.FirstOrDefault(u => u.Username == username);

            if (user == null)
            {
                return RedirectToAction("Login");
            }

            // Check if user is already a seller
            if (user.UserType == UserType.Seller)
            {
                TempData["Message"] = "You are already registered as a seller!";
                return RedirectToAction("MyAccount");
            }

            return View();
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult BecomeASeller(string shopName, string shopDescription, bool agreeTerms = false)
        {
            if (string.IsNullOrWhiteSpace(shopName))
            {
                ModelState.AddModelError("", "Shop name is required.");
                return View();
            }
            
            if (!agreeTerms)
            {
                ModelState.AddModelError("", "You must agree to the seller terms and conditions.");
                return View();
            }

            var username = User.Identity.Name;
            var user = db.Users.FirstOrDefault(u => u.Username == username);

            if (user == null)
            {
                return RedirectToAction("Login");
            }

            // Check if user is already a seller
            if (user.UserType == UserType.Seller)
            {
                TempData["Message"] = "You are already registered as a seller!";
                return RedirectToAction("MyAccount");
            }

            try
            {
                // Upgrade user to seller
                user.UserType = UserType.Seller;
                user.Role = UserRole.Seller;
                user.Roles = "Seller";

                // Create a new shop for the seller
                var shop = new Dukkan
                {
                    Name = shopName,
                    Description = !string.IsNullOrWhiteSpace(shopDescription) ? shopDescription : $"{shopName} - Official Store",
                    OwnerId = user.Id,
                    CreatedAt = DateTime.Now,
                    IsActive = true
                };

                db.Dukkans.Add(shop);
                db.SaveChanges();

                // Update authentication ticket to include new role
                FormsAuthenticationTicket ticket = new FormsAuthenticationTicket(
                    1,
                    user.Username,
                    DateTime.Now,
                    DateTime.Now.AddMinutes(30),
                    false,
                    user.Roles,
                    FormsAuthentication.FormsCookiePath
                );

                string encryptedTicket = FormsAuthentication.Encrypt(ticket);
                var cookie = new System.Web.HttpCookie(FormsAuthentication.FormsCookieName, encryptedTicket);
                Response.Cookies.Add(cookie);

                TempData["Message"] = "Congratulations! Your account has been upgraded to a seller account and your shop has been created.";
                return RedirectToAction("Details", "Dukkan", new { id = shop.Id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred. Please try again.");
                // Log the error
                System.Diagnostics.Debug.WriteLine($"BecomeASeller error: {ex.Message}");
                return View();
            }
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Login(string username, string password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ViewBag.Message = "Please enter both username and password.";
                return View();
            }

            var user = db.Users.FirstOrDefault(u => u.Username == username && u.IsActive);

            if (user != null && user.ValidatePassword(password))
            {
                // Update last login date
                user.LastLoginDate = DateTime.Now;
                db.SaveChanges();

                FormsAuthenticationTicket ticket = new FormsAuthenticationTicket(
                    1,
                    user.Username,
                    DateTime.Now,
                    DateTime.Now.AddHours(24), // Increase timeout
                    true, // isPersistent - cookie kalıcı olsun
                    user.Roles,
                    "/"  // Cookie yolu kök dizini olarak ayarla
                );

                string encryptedTicket = FormsAuthentication.Encrypt(ticket);
                var cookie = new System.Web.HttpCookie(FormsAuthentication.FormsCookieName, encryptedTicket);
                cookie.HttpOnly = true; // XSS koruması
                cookie.Path = "/"; // Kök dizinden itibaren geçerli
                cookie.Expires = DateTime.Now.AddHours(24); // Cookie süresini ayarla
                Response.Cookies.Add(cookie);

                if (user.UserType == UserType.Admin)
                {
                    return RedirectToAction("Index", "Admin");
                }
                else if (user.UserType == UserType.Seller)
                {
                    var store = db.Dukkans.FirstOrDefault(d => d.OwnerId == user.Id);
                    if (store != null)
                    {
                        return RedirectToAction("Details", "Dukkan", new { id = store.Id });
                    }
                }

                return RedirectToAction("MyAccount", "Account");
            }
            else
            {
                ViewBag.Message = "Invalid Username or Password";
                return View();
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public ActionResult Register()
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("MyAccount", "Account");
            }
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Register(User model, bool IsSeller = false, string ShopName = null, string ShopDescription = null, bool AgreeTerms = false)
        {
            if (ModelState.IsValid)
            {
                // Check if username or email already exists
                if (db.Users.Any(u => u.Username == model.Username))
                {
                    ModelState.AddModelError("Username", "This username is already taken.");
                    return View(model);
                }

                if (db.Users.Any(u => u.Email == model.Email))
                {
                    ModelState.AddModelError("Email", "This email is already registered.");
                    return View(model);
                }
                
                // Verify terms agreement for sellers
                if (IsSeller && !AgreeTerms)
                {
                    ModelState.AddModelError("", "You must agree to the seller terms and conditions to register as a seller.");
                    return View(model);
                }

                try
                {
                    var user = new User
                    {
                        Username = model.Username,
                        Email = model.Email,
                        Address = model.Address,
                        PhoneNumber = model.PhoneNumber,
                        CreatedAt = DateTime.Now,
                        IsActive = true,
                        UserType = IsSeller ? UserType.Seller : UserType.Customer,
                        Role = IsSeller ? UserRole.Seller : UserRole.Customer,
                        Roles = IsSeller ? "Seller" : "Customer"
                    };

                    user.SetPassword(model.Password);
                    db.Users.Add(user);
                    db.SaveChanges();

                    // If registering as a seller, create a shop
                    if (IsSeller)
                    {
                        if (string.IsNullOrWhiteSpace(ShopName))
                        {
                            ModelState.AddModelError("", "Shop name is required for seller accounts.");
                            db.Users.Remove(user);
                            db.SaveChanges();
                            return View(model);
                        }

                        var shop = new Dukkan
                        {
                            Name = ShopName,
                            Description = ShopDescription ?? $"{ShopName} - Official Store",
                            OwnerId = user.Id,
                            CreatedAt = DateTime.Now,
                            IsActive = true
                        };

                        db.Dukkans.Add(shop);
                        db.SaveChanges();
                    }

                    return RedirectToAction("Login");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "An error occurred during registration. Please try again.");
                    // Log the error for debugging
                    System.Diagnostics.Debug.WriteLine($"Registration error: {ex.Message}");
                }
            }

            return View(model);
        }

        [HttpGet]
        public ActionResult Edit()
        {
            var username = User.Identity.Name;
            var user = db.Users.FirstOrDefault(u => u.Username == username);

            if (user == null)
            {
                // User is authenticated but not found in the database
                System.Diagnostics.Debug.WriteLine($"Authentication issue: User '{username}' is authenticated but not found in database");
                FormsAuthentication.SignOut(); // Sign out the user
                TempData["Message"] = "Your session has expired. Please log in again.";
                return RedirectToAction("Login");
            }

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(User updatedUser, string currentPassword, string newPassword)
        {
            var username = User.Identity.Name;
            var user = db.Users.FirstOrDefault(u => u.Username == username);

            if (user == null)
            {
                ViewBag.Message = "User not found.";
                return View(updatedUser);
            }

            try
            {
                if (!string.IsNullOrWhiteSpace(currentPassword) && !user.ValidatePassword(currentPassword))
                {
                    ModelState.AddModelError("", "Current password is incorrect.");
                    return View(updatedUser);
                }

                // Update user information
                user.Email = updatedUser.Email;
                user.Address = updatedUser.Address;
                user.PhoneNumber = updatedUser.PhoneNumber;

                if (!string.IsNullOrWhiteSpace(updatedUser.Username) && updatedUser.Username != user.Username)
                {
                    if (db.Users.Any(u => u.Username == updatedUser.Username))
                    {
                        ModelState.AddModelError("Username", "This username is already taken.");
                        return View(updatedUser);
                    }
                    user.Username = updatedUser.Username;
                    FormsAuthentication.SetAuthCookie(user.Username, false);
                }

                if (!string.IsNullOrWhiteSpace(newPassword))
                {
                    user.SetPassword(newPassword);
                }

                db.SaveChanges();
                ViewBag.Message = "Your account information has been successfully updated.";
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred while updating your information. Please try again.");
                // Log the exception
                System.Diagnostics.Debug.WriteLine($"Profile update error: {ex.Message}");
            }

            return View(updatedUser);
        }

        public ActionResult MyAccount()
        {
            var username = User.Identity.Name;
            var user = db.Users.FirstOrDefault(u => u.Username == username);

            if (user == null)
            {
                // User is authenticated but not found in the database
                // This can happen if the user record was deleted but the auth cookie remains
                System.Diagnostics.Debug.WriteLine($"Authentication issue: User '{username}' is authenticated but not found in database");
                FormsAuthentication.SignOut(); // Sign out the user
                TempData["Message"] = "Your session has expired. Please log in again.";
                return RedirectToAction("Login");
            }

            if (user.UserType == UserType.Admin)
            {
                return RedirectToAction("Index", "Admin");
            }
            else if (user.UserType == UserType.Seller)
            {
                var store = db.Dukkans.FirstOrDefault(d => d.OwnerId == user.Id);
                if (store != null)
                {
                    return RedirectToAction("Details", "Dukkan", new { id = store.Id });
                }
            }

            return View(user);
        }

        public ActionResult ViewOrders()
        {
            var username = User.Identity.Name;
            var user = db.Users.FirstOrDefault(u => u.Username == username);

            if (user == null)
            {
                // User is authenticated but not found in the database
                System.Diagnostics.Debug.WriteLine($"Authentication issue: User '{username}' is authenticated but not found in database");
                FormsAuthentication.SignOut(); // Sign out the user
                TempData["Message"] = "Your session has expired. Please log in again.";
                return RedirectToAction("Login");
            }

            var orders = db.Orders
                .Where(o => o.UserId == user.Id)
                .OrderByDescending(o => o.OrderDate)
                .ToList();

            return View(orders);
        }

        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            return RedirectToAction("Login");
        }
    }
}

public class OrderDetailViewModel
{
    public int ProductId { get; set; }
    public string ProductName { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
public class OrderViewModel
{
    public int Id { get; set; }
    public DateTime OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; }
    public string RecipientName { get; set; }
    public string ShippingCompany { get; set; }
    public string Address { get; set; }
    public string PhoneNumber { get; set; }
    public List<OrderDetailViewModel> OrderDetails { get; set; }
}


