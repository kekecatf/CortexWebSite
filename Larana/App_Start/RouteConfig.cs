using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace Larana
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapMvcAttributeRoutes();

            // Add specific route for Product/Create with ID parameter
            routes.MapRoute(
                name: "ProductCreate",
                url: "Product/Create/{id}",
                defaults: new { controller = "Dukkan", action = "ProductRedirect" },
                constraints: new { id = @"\d+" }  // Ensure id is a number
            );

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}