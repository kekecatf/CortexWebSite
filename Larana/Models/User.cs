using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Cryptography;
using System.Text;
using System.Linq;

namespace Larana.Models
{
    public enum UserRole
    {
        Customer = 0,
        Seller = 1,
        Admin = 2
    }

    [Table("Users")]
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Kullanıcı adı zorunludur.")]
        [StringLength(50, ErrorMessage = "Kullanıcı adı en fazla 50 karakter olabilir.")]
        public string Username { get; set; }

        [Required(ErrorMessage = "E-posta adresi zorunludur.")]
        [StringLength(100, ErrorMessage = "E-posta adresi en fazla 100 karakter olabilir.")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
        public string Email { get; set; }
        
        [StringLength(50, ErrorMessage = "Ad en fazla 50 karakter olabilir.")]
        public string FirstName { get; set; }
        
        [StringLength(50, ErrorMessage = "Soyad en fazla 50 karakter olabilir.")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Şifre zorunludur.")]
        [StringLength(50, ErrorMessage = "Şifre en fazla 50 karakter olabilir.")]
        public string Password { get; set; }

        [StringLength(50)]
        public string PasswordSalt { get; set; }

        public string Address { get; set; }
        
        [Phone(ErrorMessage = "Geçerli bir telefon numarası giriniz.")]
        public string PhoneNumber { get; set; }
        
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginDate { get; set; }
        public bool IsActive { get; set; }
        public UserType UserType { get; set; }

        [Required]
        public UserRole Role { get; set; }

        [StringLength(50)]
        public string Roles { get; set; }

        [NotMapped]
        public int RoleId
        {
            get { return (int)Role; }
            set { Role = (UserRole)value; }
        }
        
        [NotMapped]
        public string FullName
        {
            get
            {
                if (!string.IsNullOrEmpty(FirstName) && !string.IsNullOrEmpty(LastName))
                    return FirstName + " " + LastName;
                else if (!string.IsNullOrEmpty(FirstName))
                    return FirstName;
                else if (!string.IsNullOrEmpty(LastName))
                    return LastName;
                else
                    return Username;
            }
        }

        // Navigation properties
        public virtual ICollection<Cart> Carts { get; set; }
        public virtual ICollection<Dukkan> Dukkans { get; set; }

        public User()
        {
            Carts = new HashSet<Cart>();
            Dukkans = new HashSet<Dukkan>();
            CreatedAt = DateTime.Now;
            IsActive = true;
            UserType = UserType.Customer;
            Role = UserRole.Customer;
            Roles = "Customer";
            FirstName = "";
            LastName = "";
        }

        public static string GenerateSalt()
        {
            byte[] salt = new byte[16];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(salt);
            }
            return Convert.ToBase64String(salt);
        }

        public static string HashPassword(string password, string salt)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, Convert.FromBase64String(salt), 10000))
            {
                byte[] hash = pbkdf2.GetBytes(32);
                return Convert.ToBase64String(hash);
            }
        }

        public static bool VerifyPassword(string password, string hashedPassword, string salt)
        {
            string computedHash = HashPassword(password, salt);
            return computedHash == hashedPassword;
        }

        public void SetPassword(string password)
        {
            if (string.IsNullOrEmpty(PasswordSalt))
            {
                PasswordSalt = GenerateSalt();
            }
            Password = HashPassword(password, PasswordSalt);
        }

        public bool ValidatePassword(string password)
        {
            return VerifyPassword(password, Password, PasswordSalt);
        }
    }

    public enum UserType
    {
        Customer = 0,
        Seller = 1,
        Admin = 2
    }
}
