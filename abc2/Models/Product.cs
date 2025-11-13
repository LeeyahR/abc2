using Azure;
using Azure.Data.Tables;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace abc2.Models
{
    public class Product : ITableEntity
    {
        [Key]
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public string? Details { get; set; }
        public string? ImageUrl { get; set; }

        [NotMapped]
        public IFormFile ImageFile { get; set; }

        public int? Quantity { get; set; }
        public double? Price { get; set; }

        //ITableEntity
        [NotMapped]
        public string? PartitionKey { get; set; } = "Product";
        [NotMapped]
        public string? RowKey { get; set; } = string.Empty;
        [NotMapped]
        public ETag ETag { get; set; }
        [NotMapped]
        public DateTimeOffset? Timestamp { get; set; }
    }
}
