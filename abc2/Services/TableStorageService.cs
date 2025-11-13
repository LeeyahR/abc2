using abc2.Models;
using Azure.Data.Tables;
using Azure;

namespace abc2.Services
{
    public class TableStorageService
    {
        public readonly TableClient _customerTableClient;
        public readonly TableClient _productTableClient;
        public readonly TableClient _orderTableClient;

        public TableStorageService(string connectionString)
        {
            _customerTableClient = new TableClient(connectionString, "Customer");
            _productTableClient = new TableClient(connectionString, "Product");
            _orderTableClient = new TableClient(connectionString, "Order");

            // Create tables if they don't exist
            _customerTableClient.CreateIfNotExists();
            _productTableClient.CreateIfNotExists();
            _orderTableClient.CreateIfNotExists();
        }

        // ---------------- Customers ----------------
        public async Task<List<Customer>> GetAllCustomersAsync()
        {
            var customers = new List<Customer>();
            await foreach (var customer in _customerTableClient.QueryAsync<Customer>())
                customers.Add(customer);
            return customers;
        }

        public async Task AddCustomerAsync(Customer customer)
        {
            if (string.IsNullOrEmpty(customer.PartitionKey) || string.IsNullOrEmpty(customer.RowKey))
                throw new ArgumentException("PartitionKey and RowKey must be set.");

            await _customerTableClient.AddEntityAsync(customer);
        }

        public async Task<Customer> CustomerDetailsAsync(string partitionKey, string rowKey)
        {
            var details = await _customerTableClient.GetEntityAsync<Customer>(partitionKey, rowKey);
            return details.Value;
        }

        public async Task UpdateCustomerAsync(Customer updatedCustomer)
        {
            await _customerTableClient.UpdateEntityAsync(updatedCustomer, ETag.All, TableUpdateMode.Replace);
        }

        public async Task DeleteCustomerAsync(string partitionKey, string rowKey)
        {
            await _customerTableClient.DeleteEntityAsync(partitionKey, rowKey);
        }

        // ---------------- Products ----------------
        public async Task<List<Product>> GetAllProductsAsync()
        {
            var products = new List<Product>();
            await foreach (var product in _productTableClient.QueryAsync<Product>())
                products.Add(product);
            return products;
        }

        public async Task AddProductAsync(Product product)
        {
            if (string.IsNullOrEmpty(product.PartitionKey) || string.IsNullOrEmpty(product.RowKey))
                throw new ArgumentException("PartitionKey and RowKey must be set.");

            await _productTableClient.AddEntityAsync(product);
        }

        public async Task<Product> ProductDetailsAsync(string partitionKey, string rowKey)
        {
            var details = await _productTableClient.GetEntityAsync<Product>(partitionKey, rowKey);
            return details.Value;
        }

        public async Task UpdateProductAsync(Product product)
        {
            await _productTableClient.UpdateEntityAsync(product, ETag.All, TableUpdateMode.Replace);
        }

        public async Task DeleteProductAsync(string partitionKey, string rowKey)
        {
            await _productTableClient.DeleteEntityAsync(partitionKey, rowKey);
        }

        // ---------------- Orders ----------------
        public async Task<List<Order>> GetAllOrdersAsync()
        {
            var orders = new List<Order>();
            await foreach (var order in _orderTableClient.QueryAsync<Order>())
                orders.Add(order);
            return orders;
        }

        public async Task<Order?> OrderDetailsAsync(string partitionKey, string rowKey)
        {
            try
            {
                var response = await _orderTableClient.GetEntityAsync<Order>(partitionKey, rowKey);
                return response.Value;
            }
            catch (RequestFailedException)
            {
                return null;
            }
        }

        public async Task AddOrderAsync(Order order)
        {
            if (string.IsNullOrEmpty(order.PartitionKey) || string.IsNullOrEmpty(order.RowKey))
                throw new ArgumentException("PartitionKey and RowKey must be set.");

            await _orderTableClient.AddEntityAsync(order);
        }

        public async Task DeleteOrderAsync(string partitionKey, string rowKey)
        {
            
            await _orderTableClient.DeleteEntityAsync(partitionKey, rowKey);
        }
    }
}
