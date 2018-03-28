using FormatService;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Tests
{
    public sealed class FakeImageDownloader : IImageDownloader
    {
        public byte[] ImageBytes { get; set; }

        public void Dispose()
        {
        }

        public Task<Stream> GetStreamAsync(Uri requestUrl, CancellationToken cancel)
        {
            return Task.FromResult<Stream>(new MemoryStream(ImageBytes));
        }
    }
}
