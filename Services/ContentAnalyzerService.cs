using Azure.AI.ContentSafety;
using Azure;

namespace SuggestionBox.Services
{
    public class ContentAnalyzerService {
        private IConfiguration _config;

        public ContentAnalyzerService(IConfiguration config) { _config = config; }
        public async Task<bool> ContainsUnsafeContent(string text)
        {
            // retrieve the endpoint and key from the environment variables created earlier
            string endpoint = _config.GetValue<string>("CONTENT_SAFETY_ENDPOINT");
            string key = _config.GetValue<string>("CONTENT_SAFETY_KEY");
            int harmfulContentThreshold = _config.GetValue<int>("HARMFUL_CONTENT_THRESHOLD");

            ContentSafetyClient client = new ContentSafetyClient(new Uri(endpoint), new AzureKeyCredential(key));

            var request = new AnalyzeTextOptions(text);

            Response<AnalyzeTextResult> response;
            try
            {
                response = await client.AnalyzeTextAsync(request);
            }
            catch (RequestFailedException ex)
            {
                Console.WriteLine("Analyze text failed.\nStatus code: {0}, Error code: {1}, Error message: {2}", ex.Status, ex.ErrorCode, ex.Message);
                throw;
            }

            Console.WriteLine("Hate severity: {0}", response.Value.HateResult?.Severity ?? 0);
            Console.WriteLine("SelfHarm severity: {0}", response.Value.SelfHarmResult?.Severity ?? 0);
            Console.WriteLine("Sexual severity: {0}", response.Value.SexualResult?.Severity ?? 0);
            Console.WriteLine("Violence severity: {0}", response.Value.ViolenceResult?.Severity ?? 0);

            if (response.Value.HateResult?.Severity >= harmfulContentThreshold ||
                response.Value.SexualResult?.Severity >= harmfulContentThreshold ||
                response.Value.ViolenceResult?.Severity >= harmfulContentThreshold ||
                response.Value.SelfHarmResult?.Severity >= harmfulContentThreshold)
            {
                return true;
            }
            return false;
        }
    }
}
