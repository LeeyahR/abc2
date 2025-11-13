using Azure.Data.Tables;
using Azure;
using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations.Schema;

namespace abc2.Models
{
    public class OrderEntity : ITableEntity
    {
        [JsonPropertyName("customerId")]
        public int CustomerID { get; set; }

        [JsonPropertyName("productId")]
        public int ProductId { get; set; }

        [JsonPropertyName("details")]
        public string? Details { get; set; }

        [JsonPropertyName("orderDate")]
        public DateTime OrderDate { get; set; }
        [JsonPropertyName("orderLocation")]
        public string? OrderLocation { get; set; }

        // Table storage properties
        [NotMapped]
        public string PartitionKey { get; set; }
        [NotMapped]
        public string RowKey { get; set; }
        [NotMapped]
        public ETag ETag { get; set; }
        [NotMapped]
        public DateTimeOffset? Timestamp { get; set; }
    }
}
