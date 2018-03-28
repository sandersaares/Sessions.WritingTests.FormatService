using Axinom.Toolkit;
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace FormatService
{
    public sealed class HttpImageDownloader : IImageDownloader
    {
        private readonly HttpClient _client = new HttpClient();

        public void Dispose()
        {
            _client.Dispose();
        }

        public Task<Stream> GetStreamAsync(Uri requestUrl, CancellationToken cancel)
        {
            return _client.GetStreamAsync(requestUrl).WithAbandonment(cancel);
        }
    }
}
