using Azure.Data.Tables;
using Azure;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace abc2.Models
{
    public class Order : ITableEntity
    {
        [Key]
        public int OrderId { get; set; }
        [NotMapped]
        public string? PartitionKey { get; set; }
        [NotMapped]
        public string? RowKey { get; set; }
        [NotMapped]
        public DateTimeOffset? Timestamp { get; set; }
        [NotMapped]
        public ETag ETag { get; set; }

        //Introduce validation example
        [Required(ErrorMessage = "Please select a Customer.")]
        public int CustomerID { get; set; } //FK to the customer who made the order
        [Required(ErrorMessage = "Please select a product.")]
        public int ProductId { get; set; } //FK to the product being ordered 
        [Required(ErrorMessage = "Please enter your order details")]
        public string? Details { get; set; }
        [Required(ErrorMessage = "Please select the date")]
        public DateTime OrderDate { get; set; }
        [Required(ErrorMessage = "Please enter your location")]
        public string? OrderLocation { get; set; }

        // NEW: Order status
        [Required]
        [Display(Name = "Order Status")]
        public string Status { get; set; } = "Pending"; // default status
    }
}
