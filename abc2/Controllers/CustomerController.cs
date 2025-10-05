using abc2.Models;
using abc2.Services;
using Azure.Storage.Queues;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text;

namespace abc2.Controllers
{
    public class CustomerController : Controller
    {
        private readonly TableStorageService _tableStorageService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        public CustomerController(TableStorageService tableStorageService, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _tableStorageService = tableStorageService;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        public async Task<IActionResult> Index()
        {
            var httpClient = _httpClientFactory.CreateClient();
            var apiBase = _configuration["FunctionApi:BaseUrl"];

            try
            {
                var httpResponse = await httpClient.GetAsync($"{apiBase}customers");

                if (httpResponse.IsSuccessStatusCode)
                {
                    using var contentStream = await httpResponse.Content.ReadAsStreamAsync();
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    var customers = await JsonSerializer.DeserializeAsync<IEnumerable<Customer>>(contentStream, options);
                    return View(customers);
                }
            }
            catch (HttpRequestException)
            {
                ViewBag.ErrorMessage = "Could not connect to API, please ensure the Azure function is running.";
                return View(new List<Customer>());
            }

            ViewBag.ErrorMessage = "An error has occurred while retrieving the data from the API.";
            return View(new List<Customer>());
        }
        public async Task<IActionResult> Details(string partitionKey, string rowKey)
        {
            var customer = await _tableStorageService.CustomerDetailsAsync(partitionKey, rowKey);
            if (customer == null)
            {
                return NotFound();
            }

            return View(customer);
        }

        public async Task<IActionResult> Delete(string partitionKey, string rowKey)
        {
            await _tableStorageService.DeleteCustomerAsync(partitionKey, rowKey);
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> AddCustomer(Customer model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var httpClient = _httpClientFactory.CreateClient();
                var apiBaseUrl = _configuration["FunctionApi:BaseUrl"];

                var customerJson = JsonSerializer.Serialize(model, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });

                var content = new StringContent(customerJson, Encoding.UTF8, "application/json");
                var httpResponseMessage = await httpClient.PostAsync($"{apiBaseUrl.TrimEnd('/')}/customers", content);

                if (httpResponseMessage.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = $"Successfully added {model.CustomerName}.";
                    return RedirectToAction("Index");
                }

                var errorContent = await httpResponseMessage.Content.ReadAsStringAsync();
                ModelState.AddModelError(string.Empty, $"API Error: {errorContent}");
                return View(model);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Exception: {ex.Message}");
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult AddCustomer()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string partitionKey, string rowKey)
        {
            var customer = await _tableStorageService.CustomerDetailsAsync(partitionKey, rowKey);
            if (customer == null)
            {
                return NotFound();
            }

            return View(customer);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Customer updatedCustomer)
        {
            await _tableStorageService.UpdateCustomerAsync(updatedCustomer);
            return RedirectToAction("Index");
        }

        public async Task SendCustomerToQueue(Customer model)
        {
            var queueConnectionString = _configuration["AzureStorage"]; // from appsettings
            var queueName = "customer-queue";

            // Create QueueClient
            var queueClient = new QueueClient(queueConnectionString, queueName);
            await queueClient.CreateIfNotExistsAsync(); // creates queue if not exists

            // Serialize Customer to JSON
            var customerJson = JsonSerializer.Serialize(model);

            // Encode message as Base64
            var messageBytes = Encoding.UTF8.GetBytes(customerJson);
            var base64Message = Convert.ToBase64String(messageBytes);

            // Send message to queue
            await queueClient.SendMessageAsync(base64Message);
        }


    }
}