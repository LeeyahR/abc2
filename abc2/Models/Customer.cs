using Azure.Data.Tables;
using Azure;
using System.ComponentModel.DataAnnotations;

namespace abc2.Models
{
    public class Customer : ITableEntity
    {
        [Key]
        public int CustomerID { get; set; } //Ensure this property exists and is populated
        public string? CustomerName { get; set; } //Ensure this property exists and is populated
        public string? Address { get; set; }
        public string? ContactNo { get; set; }
        public string? Email { get; set; }
        public string? Password { get; set; }

        // ITableEntity implementation
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public ETag ETag { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
    }
}
