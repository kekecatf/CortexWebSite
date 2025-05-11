using System;
using System.Linq;
using System.Web;
using Larana.Data;
using Larana.Models;

namespace Larana.Services
{
    public class PopularityService
    {
        private readonly ApplicationDbContext _context;
        
        public PopularityService(ApplicationDbContext context)
        {
            _context = context;
        }
        
        /// <summary>
        /// Increment product view count
        /// </summary>
        public void IncrementProductView(int productId)
        {
            var product = _context.Products.Find(productId);
            if (product != null)
            {
                product.ViewCount += 1;
                _context.SaveChanges();
            }
        }
        
        /// <summary>
        /// Increment shop view count
        /// </summary>
        public void IncrementShopView(int shopId)
        {
            var shop = _context.Dukkans.Find(shopId);
            if (shop != null)
            {
                shop.ViewCount += 1;
                _context.SaveChanges();
            }
        }
        
        /// <summary>
        /// Update order count when a new order is placed
        /// </summary>
        public void UpdateOrderCounts(int orderId)
        {
            var order = _context.Orders
                .Include("OrderDetails")
                .Include("OrderDetails.Product")
                .FirstOrDefault(o => o.Id == orderId);
                
            if (order == null) return;
            
            // Get unique products and their shops
            var productIds = order.OrderDetails.Select(od => od.ProductId).Distinct().ToList();
            var shopIds = order.OrderDetails.Select(od => od.Product.DukkanId).Where(id => id.HasValue).Select(id => id.Value).Distinct().ToList();
            
            // Update product order counts
            foreach (var productId in productIds)
            {
                var product = _context.Products.Find(productId);
                if (product != null)
                {
                    product.OrderCount += 1;
                    // Also update sales count
                    var quantity = order.OrderDetails.Where(od => od.ProductId == productId).Sum(od => od.Quantity);
                    product.Sales += quantity;
                }
            }
            
            // Update shop order counts
            foreach (var shopId in shopIds)
            {
                var shop = _context.Dukkans.Find(shopId);
                if (shop != null)
                {
                    shop.OrderCount += 1;
                }
            }
            
            _context.SaveChanges();
        }
        
        /// <summary>
        /// Recalculate shop ratings based on product ratings and orders
        /// </summary>
        public void RecalculateShopRatings()
        {
            var shops = _context.Dukkans.ToList();
            
            foreach (var shop in shops)
            {
                // Get all products for this shop
                var products = _context.Products.Where(p => p.DukkanId == shop.Id).ToList();
                
                if (products.Any())
                {
                    // Calculate average rating from products and orders
                    decimal baseRating = 3.0m; // Default base rating
                    
                    // Factor in order count (more orders = higher potential rating)
                    decimal orderFactor = Math.Min(shop.OrderCount / 100.0m, 1.0m); // Max +1 point for orders
                    
                    // Factor in view count (more views with orders = higher engagement)
                    decimal viewRatio = shop.ViewCount > 0 ? (decimal)shop.OrderCount / shop.ViewCount : 0;
                    decimal viewFactor = Math.Min(viewRatio * 2, 1.0m); // Max +1 point for good view-to-order ratio
                    
                    // Calculate final rating (capped at 5.0)
                    shop.Rating = Math.Min(baseRating + orderFactor + viewFactor, 5.0m);
                    
                    // Round to nearest 0.5
                    shop.Rating = Math.Round(shop.Rating * 2, MidpointRounding.AwayFromZero) / 2;
                }
            }
            
            _context.SaveChanges();
        }
        
        /// <summary>
        /// Calculate product popularity score (used for sorting)
        /// </summary>
        public double CalculateProductPopularityScore(Product product)
        {
            if (product == null) return 0;
            
            // Base score from sales
            double score = product.Sales * 10;
            
            // Add points for views
            score += product.ViewCount * 0.1;
            
            // Add points for orders
            score += product.OrderCount * 5;
            
            // Recency factor (newer products get a boost, gradually diminishing)
            var daysSinceCreation = (DateTime.Now - product.CreatedAt).TotalDays;
            double recencyFactor = Math.Max(1.0, 30 - daysSinceCreation) / 30;
            
            // Apply recency boost (max 2x for brand new, diminishing to 1x after 30 days)
            score *= (1 + recencyFactor);
            
            return score;
        }
    }
} 