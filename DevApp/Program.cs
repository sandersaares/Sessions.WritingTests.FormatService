using Axinom.Toolkit;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace DevApp
{
    // Just saves manual labor in Fiddler.
    class Program
    {
        static readonly HttpClient _client = new HttpClient();

        static async Task Main(string[] args)
        {
            var jobId = await StartJobAsync();

            Console.WriteLine($"Job {jobId} started. Waiting for it to complete.");

            // Now we monitor the job until it is finished.

            while (true)
            {
                var status = await GetJobStatusAsync(jobId);

                if (status["isCompleted"].Value<bool>())
                {
                    var error = status["error"].Value<string>();
                    if (string.IsNullOrWhiteSpace(error))
                    {
                        Console.WriteLine("Job completed successfully.");
                    }
                    else
                    {
                        Console.Error.WriteLine("Job failed");
                        Console.Error.WriteLine(error);
                        Environment.ExitCode = 1;
                    }

                    Console.WriteLine("Press enter to exit.");
                    Console.ReadLine();
                    return;
                }

                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }

        static async Task<JObject> GetJobStatusAsync(Guid jobId)
        {
            var response = await _client.GetAsync("http://localhost:5643/Jobs/" + jobId);
            await response.EnsureSuccessStatusCodeAndReportFailureDetailsAsync();

            var contentString = await response.Content.ReadAsStringAsync();

            Console.WriteLine(contentString);

            return JsonConvert.DeserializeObject(contentString) as JObject;
        }

        static async Task<Guid> StartJobAsync()
        {
            var payload = new
            {
                ImageUrl = "https://www.hdwallpapers.in/walls/planet_mars_4k_8k-HD.jpg",
                OutputStorageContainerName = "mars"
            };

            var json = JsonConvert.SerializeObject(payload);

            var response = await _client.PostAsync("http://localhost:5643/Jobs", new StringContent(json, Encoding.UTF8, "application/json"));
            await response.EnsureSuccessStatusCodeAndReportFailureDetailsAsync();

            var contentString = await response.Content.ReadAsStringAsync();

            Console.WriteLine(contentString);

            var job = JsonConvert.DeserializeObject(contentString) as JObject;
            return Guid.Parse(job["id"].Value<string>());
        }
    }
}
