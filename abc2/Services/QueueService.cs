using Azure.Storage.Queues;

namespace abc2.Services
{
    public class QueueService
    {
        private readonly QueueClient _queueClient;
        public QueueService(string connectionString, string queueName)
        {
            _queueClient = new QueueClient(connectionString, queueName);
            _queueClient.CreateIfNotExists();
        }
        public async Task SendMessage(string message)
        {
            await _queueClient.SendMessageAsync(message);
        }
    }
}