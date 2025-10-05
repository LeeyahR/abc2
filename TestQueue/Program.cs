using Azure.Storage.Queues;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace TestQueue
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var connectionString = "DefaultEndpointsProtocol=https;AccountName=leeyahabc;AccountKey=qkX1wvQeL7RJklKmmSY57qcNWqZ0+3v1FyG08fOpOx+zm7UpI3FcMIoUvPooSz4Xkz8X9ToiZDuG+AStcanfxA==;EndpointSuffix=core.windows.net";

            // Customer queue
            var customerQueue = new QueueClient(connectionString, "customer-queue", new QueueClientOptions { MessageEncoding = QueueMessageEncoding.Base64 });
            await customerQueue.CreateIfNotExistsAsync();

            var customer = new { CustomerName = "Abigail", Address = "85 Alpha Lane", ContactNo = "0335894586", Email = "Abigail@gmail.com", Password = "45863" };
            string customerJson = JsonSerializer.Serialize(customer);
            await customerQueue.SendMessageAsync(customerJson);
            Console.WriteLine($"Customer message sent: {customerJson}");

            //product queue
            var productQueue = new QueueClient(connectionString, "product-queue", new QueueClientOptions { MessageEncoding = QueueMessageEncoding.Base64 });
            await productQueue.CreateIfNotExistsAsync();

            var product = new { ProductName = "Mirror", Details = "Glass with gold frame", Price = 1299.99, Quantity = 19, ImageUrl = "https://example.com/mirror.png" };
            string productJson = JsonSerializer.Serialize(product);
            await productQueue.SendMessageAsync(productJson);
            Console.WriteLine($"Product message sent: {productJson}");

            //order queue
            var orderQueue = new QueueClient(connectionString, "order-queue", new QueueClientOptions { MessageEncoding = QueueMessageEncoding.Base64 });
            await orderQueue.CreateIfNotExistsAsync();

            var order = new { CustomerId = 1, ProductId = 101, Details = "Processing", OrderDate = DateTime.UtcNow, OrderLocation = "Umhlanga" };
            string orderJson = JsonSerializer.Serialize(order);
            await orderQueue.SendMessageAsync(orderJson);
            Console.WriteLine($"Order message sent: {orderJson}");
        }
    }
}