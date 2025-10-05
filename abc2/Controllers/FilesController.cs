using abc2.Models;
using abc2.Services;
using Microsoft.AspNetCore.Mvc;

namespace abc2.Controllers
{
    public class FilesController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public FilesController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        public async Task<IActionResult> Index()
        {
            List<FileModel> files = new List<FileModel>();
            try
            {
                var client = _httpClientFactory.CreateClient();
                var functionUrl = _configuration["FunctionApi:BaseUrl"] + "uploads";

                var response = await client.GetAsync(functionUrl);

                if (response.IsSuccessStatusCode)
                {
                    files = await response.Content.ReadFromJsonAsync<List<FileModel>>() ?? new List<FileModel>();
                }
                else
                {
                    ViewBag.Message = $"Failed to load files: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}";
                }
            }
            catch (Exception ex)
            {
                ViewBag.Message = $"Error loading files: {ex.Message}";
            }

            return View(files);
        }


        [HttpPost]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["Message"] = "Please select a file to upload.";
                return RedirectToAction("Index");
            }

            try
            {
                using var content = new MultipartFormDataContent();
                var fileContent = new StreamContent(file.OpenReadStream());
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
                content.Add(fileContent, "file", file.FileName);

                var client = _httpClientFactory.CreateClient();
                var functionUrl = _configuration["FunctionApi:BaseUrl"] + "uploads";

                var response = await client.PostAsync(functionUrl, content);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                    TempData["Message"] = $"Success: {responseBody}";
                else
                    TempData["Message"] = $"Failed: {response.StatusCode} - {responseBody}";
            }
            catch (Exception ex)
            {
                TempData["Message"] = $"Error: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> DownloadFile(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return BadRequest("File name cannot be null or empty.");

            try
            {
                var client = _httpClientFactory.CreateClient();
                var functionUrl = _configuration["FunctionApi:BaseUrl"] + $"uploads/download?fileName={Uri.EscapeDataString(fileName)}";

                var response = await client.GetAsync(functionUrl);

                if (!response.IsSuccessStatusCode)
                {
                    return NotFound($"File '{fileName}' not found. Status code: {response.StatusCode}");
                }

                var stream = await response.Content.ReadAsStreamAsync();
                var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
                return File(stream, contentType, fileName);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error downloading file: {ex.Message}");
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteFile(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                TempData["Message"] = "File name cannot be null or empty.";
                return RedirectToAction("Index");
            }

            try
            {
                var client = _httpClientFactory.CreateClient();
                var functionUrl = _configuration["FunctionApi:BaseUrl"] + $"uploads/delete?fileName={Uri.EscapeDataString(fileName)}";

                var response = await client.DeleteAsync(functionUrl);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                    TempData["Message"] = $"Success: {responseBody}";
                else
                    TempData["Message"] = $"Failed: {response.StatusCode} - {responseBody}";
            }
            catch (Exception ex)
            {
                TempData["Message"] = $"Error deleting file: {ex.Message}";
            }

            return RedirectToAction("Index");
        }
    }
}