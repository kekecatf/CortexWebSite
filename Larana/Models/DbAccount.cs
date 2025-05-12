// Bu dosya artık kullanılmayacak ve Data/DbAccount.cs dosyası kullanılacak.
// Tüm işlevsellik oraya taşındı.
// Bu dosya silinebilir.

using System.Data.Entity;
using Larana.Models;
using Larana.Data;

namespace Larana.Models
{
    // Not needed as a separate context - all functionality merged into ApplicationDbContext
    public class DbAccount : BaseDbContext
    {
        public DbAccount() : base("LaranaConnection") // Use the same connection as ApplicationDbContext
        {
        }

        // These DbSets are for backward compatibility only - we should eventually
        // remove this context and use ApplicationDbContext for all entities
        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
    }
}

