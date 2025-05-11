using System;
using System.Data.Entity;
using System.Linq;
using Larana.Models;

namespace Larana.Data
{
    public class DatabaseInitializer : DropCreateDatabaseIfModelChanges<ApplicationDbContext>
    {
        protected override void Seed(ApplicationDbContext context)
        {
            // Ensure Admin role exists
            if (!context.Roles.Any(r => r.Name == "Admin"))
            {
                context.Roles.Add(new Role { Name = "Admin" });
                context.SaveChanges();
            }

            // Ensure Admin user exists
            if (!context.Users.Any(u => u.Username == "admin"))
            {
                var adminRole = context.Roles.First(r => r.Name == "Admin");
                context.Users.Add(new User
                {
                    Username = "admin",
                    Password = "admin", // In production, this should be hashed
                    Email = "admin@larana.com",
                    RoleId = adminRole.Id
                });
                context.SaveChanges();
            }

            base.Seed(context);
        }
    }
} 