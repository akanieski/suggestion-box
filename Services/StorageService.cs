using Azure;
using Azure.Data.Tables;
using Microsoft.Extensions.Configuration;

namespace SuggestionBox.Services
{
    public class StorageService
    {
        private readonly TableClient tableClient;
        private readonly ContentAnalyzerService contentAnalyzerService;

        public delegate void SuggestionMadeEventHandler(Suggestion suggestion);
        public event SuggestionMadeEventHandler SuggestionMade;
        

        public StorageService(IConfiguration configuration, ContentAnalyzerService contentAnalyzerService)
        {
            string connectionString = configuration.GetValue<string>("AzureStorage");
            this.tableClient = new TableClient(connectionString, "Suggestions");
            this.contentAnalyzerService = contentAnalyzerService;
        }

        public async Task<Suggestion> MakeSuggestion(string message, string boxId)
        {
            if (await contentAnalyzerService.ContainsUnsafeContent(message))
            {
                throw new InvalidDataException("Your message contains potentially unsafe content. Please rephrase and try again.");
            }
            else
            {
                Suggestion suggestion = new Suggestion { Message = message, PartitionKey = boxId, RowKey = Guid.NewGuid().ToString() };
                await tableClient.AddEntityAsync(suggestion);
                SuggestionMade?.Invoke(suggestion);
                return suggestion;
            }
        }

        public async Task<IEnumerable<Suggestion>> GetSuggestions(string boxId)
        {
            var results = new List<Suggestion>();
            await foreach (Page<Suggestion> page in tableClient.QueryAsync<Suggestion>($"PartitionKey eq '{boxId}'").AsPages())
            {
                foreach (Suggestion item in page.Values)
                {
                    results.Add(item);
                }
            }
            return results;
        }
    }

    public class Suggestion : ITableEntity
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public string Message { get; set; }
        public DateTimeOffset? Timestamp { get; set; } = DateTimeOffset.UtcNow;
        public ETag ETag { get; set; }
    }
}
