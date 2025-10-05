using abc2.Models;
using abc2.Services;
using Azure.Storage.Queues;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text;

namespace abc2.Controllers
{
    public class OrdersController : Controller
    {
        private readonly TableStorageService _tableStorageService;
        private readonly QueueService _queueService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public OrdersController(TableStorageService tableStorageService, QueueService queueService, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _tableStorageService = tableStorageService;
            _queueService = queueService;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        public async Task<IActionResult> Index()
        {
            var httpClient = _httpClientFactory.CreateClient();
            var apiBase = _configuration["FunctionApi:BaseUrl"];

            try
            {
                var httpResponse = await httpClient.GetAsync($"{apiBase}orders");

                if (httpResponse.IsSuccessStatusCode)
                {
                    using var contentStream = await httpResponse.Content.ReadAsStreamAsync();
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    var orders = await JsonSerializer.DeserializeAsync<IEnumerable<Order>>(contentStream, options);
                    return View(orders);
                }
            }
            catch (HttpRequestException)
            {
                ViewBag.ErrorMessage = "Could not connect to API, please ensure the Azure function is running.";
                return View(new List<Order>());
            }

            ViewBag.ErrorMessage = "An error has occurred while retrieving the data from the API.";
            return View(new List<Order>());
        }

        [HttpGet]
        public async Task<IActionResult> Register()
        {
            var httpClient = _httpClientFactory.CreateClient();
            var apiBaseUrl = _configuration["FunctionApi:BaseUrl"];

            // Fetch Customers
            var customerResponse = await httpClient.GetAsync($"{apiBaseUrl.TrimEnd('/')}/customers");
            var customers = new List<Customer>();
            if (customerResponse.IsSuccessStatusCode)
                customers = JsonSerializer.Deserialize<List<Customer>>(await customerResponse.Content.ReadAsStringAsync(),
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            // Fetch Products
            var productResponse = await httpClient.GetAsync($"{apiBaseUrl.TrimEnd('/')}/products");
            var products = new List<Product>();
            if (productResponse.IsSuccessStatusCode)
                products = JsonSerializer.Deserialize<List<Product>>(await productResponse.Content.ReadAsStringAsync(),
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            ViewData["Customers"] = customers;
            ViewData["Products"] = products;

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(Order model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                // Ensure OrderDate is UTC
                model.OrderDate = DateTime.SpecifyKind(model.OrderDate, DateTimeKind.Utc);

                var httpClient = _httpClientFactory.CreateClient();
                var apiBaseUrl = _configuration["FunctionApi:BaseUrl"];

                var orderEntity = new OrderEntity
                {
                    CustomerID = model.CustomerID,
                    ProductId = model.ProductId,
                    Details = model.Details,
                    OrderDate = model.OrderDate
                };

                string orderJson = JsonSerializer.Serialize(orderEntity, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });

                var content = new StringContent(orderJson, Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync($"{apiBaseUrl.TrimEnd('/')}/orders", content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    ModelState.AddModelError(string.Empty, $"API Error: {errorContent}");
                    return View(model);
                }

                var queueMessage = new OrderEntity
                {
                    CustomerID = model.CustomerID,
                    ProductId = model.ProductId,
                    Details = model.Details,
                    OrderDate = model.OrderDate
                };

                string queueJson = JsonSerializer.Serialize(queueMessage, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                string connectionString = Environment.GetEnvironmentVariable("connection");
                QueueClient queueClient = new QueueClient(connectionString, "order-queue");
                await queueClient.CreateIfNotExistsAsync();
                await queueClient.SendMessageAsync(queueJson);

                TempData["SuccessMessage"] = "Order added successfully and queued.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Exception: {ex.Message}");
                return View(model);
            }
        }

        // GET: show confirmation page
        public async Task<IActionResult> Delete(string partitionKey, string rowKey)
        {
            var order = await _tableStorageService.OrderDetailsAsync(partitionKey, rowKey);
            if (order == null)
                return NotFound();

            return View(order);
        }

        // POST: actually delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string partitionKey, string rowKey)
        {
            await _tableStorageService.DeleteOrderAsync(partitionKey, rowKey);
            TempData["SuccessMessage"] = "Order deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

    }
}