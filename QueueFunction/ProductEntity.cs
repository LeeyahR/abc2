using Azure.Data.Tables;
using Azure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueueFunction
{
    class ProductEntity : ITableEntity
    {
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public string? Details { get; set; }
        public string? ImageUrl { get; set; }
        public int? Quantity { get; set; }
        public double Price { get; set; }

        // --- Required Table Storage properties ---
        // Default PartitionKey groups related entities
        public string PartitionKey { get; set; } = "Products";
        // RowKey is unique per partition
        public string RowKey { get; set; } = string.Empty;
        // Required by ITableEntity
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}
