using Azure;
using Azure.Data.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueueFunction
{
    class OrderEntity : ITableEntity
    {
        public int OrderId { get; set; }
        //ITableEntity
        public string? PartitionKey { get; set; }
        public string? RowKey { get; set; }
        public ETag ETag { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public int CustomerId { get; set; }
        public int ProductId { get; set; }
        public string? Details { get; set; }
        public DateTime OrderDate { get; set; }
    }
}
