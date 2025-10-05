using abc2.Models;
using abc2.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace abc2.Controllers
{
    public class ProductsController : Controller
    {
        private readonly BlobService _blobService;
        private readonly TableStorageService _tableStorageService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        public ProductsController(BlobService blobService, TableStorageService tableStorageService, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _blobService = blobService;
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
                var httpResponse = await httpClient.GetAsync($"{apiBase}products");

                if (httpResponse.IsSuccessStatusCode)
                {
                    using var contentStream = await httpResponse.Content.ReadAsStreamAsync();
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    var products = await JsonSerializer.DeserializeAsync<IEnumerable<Product>>(contentStream, options);
                    return View(products);
                }
            }
            catch (HttpRequestException)
            {
                ViewBag.ErrorMessage = "Could not connect to API, please ensure the Azure function is running.";
                return View(new List<Product>());
            }

            ViewBag.ErrorMessage = "An error has occurred while retrieving the data from the API.";
            return View(new List<Product>());
        }

        [HttpGet]
        public IActionResult AddProduct()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AddProduct(Product model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // 1️⃣ Upload image to Blob Storage if present
            if (model.ImageFile != null)
            {
                using var stream = model.ImageFile.OpenReadStream();
                var blobUrl = await _blobService.UploadAsync(stream, model.ImageFile.FileName);
                model.ImageUrl = blobUrl; // Save Blob URL in the model
            }

            // 2️⃣ Send product data as JSON to Azure Function
            var httpClient = _httpClientFactory.CreateClient();
            var apiBaseUrl = _configuration["FunctionApi:BaseUrl"]; // e.g., https://<yourfunctionapp>.azurewebsites.net/api/

            var productJson = JsonSerializer.Serialize(model);
            var content = new StringContent(productJson, System.Text.Encoding.UTF8, "application/json");

            var httpResponse = await httpClient.PostAsync($"{apiBaseUrl}images", content);

            if (httpResponse.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = $"Successfully added {model.ProductName} with an image.";
                return RedirectToAction("Index");
            }

            ModelState.AddModelError(string.Empty, "Failed to save product. Try again.");
            return View(model);
        }




        [HttpPost]
        public async Task<IActionResult> DeleteProduct(string partitionKey, string rowKey, Product product)
        {
            if (product != null && !string.IsNullOrEmpty(product.ImageUrl))
            {
                await _blobService.DeleteBlobAsync(product.ImageUrl);
            }
            await _tableStorageService.DeleteProductAsync(partitionKey, rowKey);

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Product product, IFormFile file)
        {
            if (file != null)
            {
                var existingProduct = await _tableStorageService.ProductDetailsAsync(product.PartitionKey, product.RowKey);

                // Delete old blob if it exists
                if (!string.IsNullOrEmpty(existingProduct.ImageUrl))
                {
                    await _blobService.DeleteBlobAsync(existingProduct.ImageUrl);
                }

                // Upload new blob
                using var stream = file.OpenReadStream();
                var image = await _blobService.UploadAsync(stream, file.FileName);
                product.ImageUrl = image;
            }

            if (ModelState.IsValid)
            {
                await _tableStorageService.UpdateProductAsync(product);
                return RedirectToAction("Index");
            }

            return View(product);
        }
        [HttpGet]
        public async Task<IActionResult> Edit(string partitionKey, string rowKey)
        {
            var product = await _tableStorageService.ProductDetailsAsync(partitionKey, rowKey);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }

        [HttpGet]
        public async Task<IActionResult> Details(string partitionKey, string rowKey)
        {
            var product = await _tableStorageService.ProductDetailsAsync(partitionKey, rowKey);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }
    }
}