using Azure.Data.Tables;
using Azure;
using System.Text.Json.Serialization;

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
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public ETag ETag { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
    }
}
