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
                    UserId = o.UserId,
                    Address = o.Address,
                    PhoneNumber = o.PhoneNumber,
                    RecipientName = o.RecipientName,
                    ShippingCompany = o.ShippingCompany,
                    OrderDate = o.OrderDate,
                    TotalAmount = o.TotalAmount,
                    Status = o.Status,
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
            return View(product);
        }

        [HttpPost]
        public ActionResult EditProduct(Product product, string actionType)
        {
            var existingProduct = productDb.Products.FirstOrDefault(p => p.Id == product.Id);
            if (existingProduct == null)
            {
                TempData["ErrorMessage"] = "Product not found.";
                return RedirectToAction("EditProduct", new { id = product.Id });
            }

            try
            {
                if (actionType == "UpdateStockPrice")
                {
                    if (ModelState.IsValid)
                    {
                        existingProduct.Stock = product.Stock;

                        string correctedPrice = product.Price.ToString().Replace(',', '.');
                        if (decimal.TryParse(correctedPrice, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal parsedPrice))
                        {
                            existingProduct.Price = parsedPrice;
                        }
                        else
                        {
                            TempData["ErrorMessage"] = "Invalid price format.";
                            return RedirectToAction("EditProduct", new { id = product.Id });
                        }

                        productDb.SaveChanges();
                        TempData["SuccessMessage"] = "Stock and price updated successfully!";
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Validation failed for Stock or Price.";
                    }
                }
                else if (actionType == "DeleteProduct")
                {
                    productDb.Products.Remove(existingProduct);
                    productDb.SaveChanges();
                    TempData["SuccessMessage"] = "Product deleted successfully!";
                    return RedirectToAction("Index");
                }
                else
                {
                    TempData["ErrorMessage"] = "Invalid action type.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
            }

            return RedirectToAction("EditProduct", new { id = product.Id });
        }



        [HttpPost]
        public ActionResult UpdateOrderStatus(int orderId, string newStatus)
        {
            var order = ordersDb.Orders.FirstOrDefault(o => o.Id == orderId);
            if (order != null)
            {
                order.Status = newStatus;
                ordersDb.SaveChanges();
            }

            return RedirectToAction("Orders");
        }

        public ActionResult AddProduct()
        {
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

                return RedirectToAction("Index");
            }

            return View(product);
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
            public decimal TotalAmount { get; set; }
            public string Status { get; set; }
            public List<OrderDetailViewModel> OrderDetails { get; set; }
        }



    }
}
