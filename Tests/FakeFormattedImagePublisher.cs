using FormatService;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Tests
{
    public sealed class FakeFormattedImagePublisher : IFormattedImagePublisher
    {
        public byte[] PublishedImageBytes { get; set; }

        public Task<string> PublishAsync(string filePath, string containerName, string publishFilename, CancellationToken cancel)
        {
            PublishedImageBytes = File.ReadAllBytes(filePath);

            return Task.FromResult("http://whhatever.local");
        }
    }
}
