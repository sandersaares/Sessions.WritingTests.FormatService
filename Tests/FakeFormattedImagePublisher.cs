using Axinom.Toolkit;
using FormatService;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Tests
{
    public sealed class FakeFormattedImagePublisher : IFormattedImagePublisher
    {
        /// <summary>
        /// All published images are added to this list.
        /// </summary>
        public List<(string filePath, string containerName, string publishFilename, byte[] imageBytes)> PublishedImages { get; } = new List<(string filePath, string containerName, string publishFilename, byte[] imageBytes)>();

        public bool AlwaysFails { get; set; }

        // Image formatting is parallelized, so we need to synchronize!
        private readonly object _lock = new object();

        public Task<string> PublishAsync(string filePath, string containerName, string publishFilename, CancellationToken cancel)
        {
            if (!File.Exists(filePath))
            {
                lock (_lock)
                    PublishedImages.Add((filePath, containerName, publishFilename, null));

                throw new FileNotFoundException("Image file to publish does not exist.", filePath);
            }
            else
            {
                lock (_lock)
                    PublishedImages.Add((filePath, containerName, publishFilename, File.ReadAllBytes(filePath)));
            }

            if (AlwaysFails)
                throw new EnvironmentException("Publishing failed.");

            return Task.FromResult("http://whhatever.local/" + publishFilename);
        }
    }
}
