using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FormatService
{
    public interface IImageDownloader : IDisposable
    {
        Task<Stream> GetStreamAsync(Uri requestUrl, CancellationToken cancel);
    }
}
