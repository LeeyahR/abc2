using Azure;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Files.Shares;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using QueueFunction;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace QueueFunctions
{
    public class Function1
    {
        private readonly ILogger<Function1> _logger;
        private readonly string _storageconnection;
        private TableClient _customer;
        private TableClient _product;
        private TableClient _order;
        private BlobContainerClient _blobContainerClient;

        public Function1(ILogger<Function1> logger)
        {
            _logger = logger;
            _storageconnection = "DefaultEndpointsProtocol=https;AccountName=leeyahabc;AccountKey=d+T9zab7RlirpGfhkoYCn8OozFzaqfO+vrgCZs50QbUSAW7lnO8wlAE1qxll82rohsBPODBZQuJl+AStHJV2Ug==;EndpointSuffix=core.windows.net";

            var serviceClient = new TableServiceClient(_storageconnection);
            _customer = serviceClient.GetTableClient("Customer");
            _product = serviceClient.GetTableClient("Product");
            _order = serviceClient.GetTableClient("Order");

            _blobContainerClient = new BlobContainerClient(_storageconnection, "images");
            _blobContainerClient.CreateIfNotExists();
        }

        // ========================= QUEUE TRIGGERS =========================

        [Function(nameof(QueueCustomerSender))]
        public async Task QueueCustomerSender([QueueTrigger("customer-queue", Connection = "connection")] QueueMessage message)
        {
            _logger.LogInformation("Processing customer queue message: {messageText}", message.MessageText);

            await _customer.CreateIfNotExistsAsync();
            var customer = JsonSerializer.Deserialize<CustomerEntity>(message.MessageText);

            if (customer == null)
            {
                _logger.LogError("Failed to deserialize customer JSON");
                return;
            }

            customer.RowKey = Guid.NewGuid().ToString();
            customer.PartitionKey = "Customers";

            await _customer.AddEntityAsync(customer);
            _logger.LogInformation("Saved customer {Name} to Table Storage", customer.Name);
        }

        [Function(nameof(QueueProductSender))]
        public async Task QueueProductSender([QueueTrigger("product-queue", Connection = "connection")] QueueMessage message)
        {
            _logger.LogInformation("Processing product queue message: {messageText}", message.MessageText);

            await _product.CreateIfNotExistsAsync();
            var product = JsonSerializer.Deserialize<ProductEntity>(message.MessageText);

            if (product == null)
            {
                _logger.LogError("Failed to deserialize product JSON");
                return;
            }

            product.RowKey = Guid.NewGuid().ToString();
            product.PartitionKey = "Products";

            await _product.AddEntityAsync(product);
            _logger.LogInformation("Saved product {Name} to Table Storage", product.ProductName);
        }

        [Function(nameof(QueueOrderSender))]
        public async Task QueueOrderSender([QueueTrigger("order-queue", Connection = "connection")] QueueMessage message)
        {
            _logger.LogInformation("Processing order queue message: {messageText}", message.MessageText);

            await _order.CreateIfNotExistsAsync();
            var orderMsg = JsonSerializer.Deserialize<Queue>(message.MessageText, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (orderMsg == null)
            {
                _logger.LogError("Failed to deserialize order JSON");
                return;
            }

            var orderEntity = new OrderEntity
            {
                CustomerId = orderMsg.CustomerId,
                ProductId = orderMsg.ProductId,
                Details = orderMsg.Details,
                OrderDate = orderMsg.OrderDate,
                RowKey = Guid.NewGuid().ToString(),
                PartitionKey = "Orders",
                Timestamp = DateTimeOffset.UtcNow
            };

            await _order.AddEntityAsync(orderEntity);
            _logger.LogInformation("Saved order {RowKey} to Table Storage", orderEntity.RowKey);
        }

        // ========================= GET ENDPOINTS =========================

        [Function("GetCustomers")]
        public async Task<HttpResponseData> GetCustomers([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "customers")] HttpRequestData req)
        {
            try
            {
                var customers = await _customer.QueryAsync<CustomerEntity>().ToListAsync();
                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(customers);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to query customers");
                var error = req.CreateResponse(HttpStatusCode.InternalServerError);
                await error.WriteStringAsync("Error retrieving customers.");
                return error;
            }
        }

        [Function("GetProducts")]
        public async Task<HttpResponseData> GetProducts([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "products")] HttpRequestData req)
        {
            try
            {
                var products = await _product.QueryAsync<ProductEntity>().ToListAsync();
                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(products);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to query products");
                var error = req.CreateResponse(HttpStatusCode.InternalServerError);
                await error.WriteStringAsync("Error retrieving products.");
                return error;
            }
        }

        [Function("GetOrders")]
        public async Task<HttpResponseData> GetOrders([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "orders")] HttpRequestData req)
        {
            try
            {
                var orders = await _order.QueryAsync<OrderEntity>().ToListAsync();
                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(orders);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to query orders");
                var error = req.CreateResponse(HttpStatusCode.InternalServerError);
                await error.WriteStringAsync("Error retrieving orders.");
                return error;
            }
        }

        // ========================= ADD ENDPOINTS =========================

        [Function("AddProduct")]
        public async Task<HttpResponseData> AddProduct([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "products")] HttpRequestData req)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var newProduct = JsonSerializer.Deserialize<ProductEntity>(requestBody);

            if (newProduct == null || string.IsNullOrEmpty(newProduct.ProductName) || newProduct.Price <= 0)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Invalid product data.");
                return badResponse;
            }

            newProduct.PartitionKey = "Products";
            newProduct.RowKey = Guid.NewGuid().ToString();

            await _product.AddEntityAsync(newProduct);
            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteStringAsync("Product added successfully.");
            return response;
        }

        [Function("AddCustomer")]
        public async Task<HttpResponseData> AddCustomer([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "customers")] HttpRequestData req)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var newCustomer = JsonSerializer.Deserialize<CustomerEntity>(requestBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (newCustomer == null || string.IsNullOrEmpty(newCustomer.Name) || string.IsNullOrEmpty(newCustomer.Email))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Invalid customer data.");
                return badResponse;
            }

            newCustomer.PartitionKey = "Customers";
            newCustomer.RowKey = Guid.NewGuid().ToString();

            await _customer.AddEntityAsync(newCustomer);
            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteStringAsync("Customer added successfully.");
            return response;
        }

        [Function("AddOrder")]
        public async Task<HttpResponseData> AddOrder([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "orders")] HttpRequestData req)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var newOrder = JsonSerializer.Deserialize<OrderEntity>(requestBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (newOrder == null || newOrder.CustomerId <= 0 || newOrder.ProductId <= 0)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Invalid order data.");
                return badResponse;
            }

            newOrder.PartitionKey = "Orders";
            newOrder.RowKey = Guid.NewGuid().ToString();
            newOrder.Timestamp = DateTimeOffset.UtcNow;
            newOrder.OrderDate = DateTime.SpecifyKind(newOrder.OrderDate, DateTimeKind.Utc);

            await _order.CreateIfNotExistsAsync();
            await _order.AddEntityAsync(newOrder);

            // Queue message for async processing
            QueueClient queueClient = new QueueClient(_storageconnection, "order-queue");
            await queueClient.CreateIfNotExistsAsync();

            var queueMessage = new Queue
            {
                CustomerId = newOrder.CustomerId,
                ProductId = newOrder.ProductId,
                Details = newOrder.Details,
                OrderDate = newOrder.OrderDate
            };
            string json = JsonSerializer.Serialize(queueMessage);
            await queueClient.SendMessageAsync(json);

            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteStringAsync("Order added successfully and queued.");
            return response;
        }

        // ========================= FILE UPLOADS =========================

        [Function("UploadToAzureFiles")]
        public async Task<HttpResponseData> UploadToAzureFiles([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "uploads")] HttpRequestData req)
        {
            try
            {
                var contentType = req.Headers.GetValues("Content-Type").FirstOrDefault();
                if (string.IsNullOrEmpty(contentType) || !contentType.Contains("multipart/form-data"))
                {
                    var badResp = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badResp.WriteStringAsync("Request must be multipart/form-data");
                    return badResp;
                }

                var boundary = HeaderUtilities.RemoveQuotes(MediaTypeHeaderValue.Parse(contentType).Boundary).Value;
                var reader = new MultipartReader(boundary, req.Body);
                var section = await reader.ReadNextSectionAsync();

                if (section == null || !ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var contentDisposition) || string.IsNullOrEmpty(contentDisposition.FileName.Value))
                {
                    var badResp = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badResp.WriteStringAsync("No file found in the request.");
                    return badResp;
                }

                string fileName = contentDisposition.FileName.Value.Trim('"');
                using var memoryStream = new MemoryStream();
                await section.Body.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                string shareName = "uploads";
                var shareClient = new ShareClient(_storageconnection, shareName);
                await shareClient.CreateIfNotExistsAsync();

                var rootDir = shareClient.GetRootDirectoryClient();
                var fileClient = rootDir.GetFileClient(fileName);

                await fileClient.CreateAsync(memoryStream.Length);
                memoryStream.Position = 0;
                await fileClient.UploadRangeAsync(new HttpRange(0, memoryStream.Length), memoryStream);

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteStringAsync($"File '{fileName}' uploaded successfully!");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file");
                var errorResp = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResp.WriteStringAsync($"Upload failed: {ex.Message}");
                return errorResp;
            }
        }

        [Function("GetUploadedFiles")]
        public async Task<HttpResponseData> GetUploadedFiles([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "uploads")] HttpRequestData req)
        {
            var files = new List<FileEntity>();

            try
            {
                string shareName = "uploads";
                var shareClient = new ShareClient(_storageconnection, shareName);
                await shareClient.CreateIfNotExistsAsync();

                var rootDir = shareClient.GetRootDirectoryClient();

                await foreach (var item in rootDir.GetFilesAndDirectoriesAsync())
                {
                    if (!item.IsDirectory)
                    {
                        var fileClient = rootDir.GetFileClient(item.Name);
                        var props = await fileClient.GetPropertiesAsync();

                        files.Add(new FileEntity
                        {
                            FileName = item.Name,
                            Size = props.Value.ContentLength,
                            DisplaySize = FormatSize(props.Value.ContentLength),
                            LastModified = props.Value.LastModified
                        });
                    }
                }

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(files);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to list files");
                var errorResp = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResp.WriteStringAsync($"Failed to list files: {ex.Message}");
                return errorResp;
            }
        }

        private string FormatSize(long bytes)
        {
            if (bytes >= 1024 * 1024)
                return $"{bytes / (1024 * 1024.0):F2} MB";
            else if (bytes >= 1024)
                return $"{bytes / 1024.0:F2} KB";
            else
                return $"{bytes} B";
        }
    }
}