using System.Data.Entity;

namespace Larana.Data
{
    public abstract class BaseDbContext : DbContext
    {
        protected BaseDbContext(string nameOrConnectionString) 
            : base(nameOrConnectionString)
        {
            // Disable proxy creation for all contexts
            this.Configuration.ProxyCreationEnabled = false;
            
            // Don't initialize database here - it's handled in Global.asax.cs
            // Not using SetInitializer here as it should be called for specific context types
        }
        
        // Common database configuration methods can go here
    }
} 