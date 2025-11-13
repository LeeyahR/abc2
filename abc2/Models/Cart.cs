using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace abc2.Models
{
    public class Cart
    {
        [Key]
        public int CartId { get; set; }

        [Required]
        public int CustomerId { get; set; }
        public Customer Customer { get; set; }

        public ICollection<CartItem> Items { get; set; } = new List<CartItem>();
    }
}
