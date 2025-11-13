using System.ComponentModel.DataAnnotations;

namespace abc2.Models
{
    public class UserAccount
    {
        [Key]
        public int UserId { get; set; }

        [Required, StringLength(50)]
        public string UserName { get; set; }

        [Required, StringLength(100)]
        public string Password { get; set; }

        [Required, StringLength(20)]
        public string Role { get; set; } // "Customer" or "Admin"

        // link to Customer table
        public int? CustomerID { get; set; }
        public Customer? Customer { get; set; }
    }
}
