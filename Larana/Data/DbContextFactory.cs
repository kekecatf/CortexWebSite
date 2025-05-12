using System;
using System.Web;
using Larana.Models;
using Larana.Data;

namespace Larana.Data
{
    /// <summary>
    /// Factory sınıfı ile DbContext nesnelerinin oluşturulması merkezi bir noktadan yönetilecek
    /// </summary>
    public static class DbContextFactory
    {
        /// <summary>
        /// Ana uygulama context'ini döndürür
        /// </summary>
        public static ApplicationDbContext CreateApplicationDbContext()
        {
            return new ApplicationDbContext();
        }
        
        /// <summary>
        /// Geriye uyumluluk için DbAccount context'ini döndürür
        /// </summary>
        public static Larana.Data.DbAccount CreateDbAccount()
        {
            return new Larana.Data.DbAccount();
        }
        
        /// <summary>
        /// Geriye uyumluluk için OrdersDbContext context'ini döndürür
        /// </summary>
        public static OrdersDbContext CreateOrdersDbContext()
        {
            return new OrdersDbContext();
        }
    }
}