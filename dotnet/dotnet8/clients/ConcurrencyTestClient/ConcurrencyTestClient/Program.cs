const string baseUrl = "http://localhost:5000"; 
const int recordId = 1;
const int concurrentRequests = 20;

Console.WriteLine($"Sending {concurrentRequests} concurrent increment requests...");

var tasks = new List<Task>();
var client = new HttpClient();

for (int i = 0; i < concurrentRequests; i++)
{
    tasks.Add(Task.Run(async () =>
    {
        var response = await client.PutAsync($"{baseUrl}/data/records/{recordId}/increment-retry", null);
        var result = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Response: {response.StatusCode} - {result}");
    }));
}

await Task.WhenAll(tasks);
Console.WriteLine("All requests finished.");
