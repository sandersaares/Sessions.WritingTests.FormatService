using System.Threading;
using System.Threading.Tasks;

namespace FormatService
{
    public interface IFormattedImagePublisher
    {
        Task<string> PublishAsync(string filePath, string containerName, string publishFilename, CancellationToken cancel);
    }
}
