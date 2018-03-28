using Axinom.Toolkit;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FormatService
{
    /// <summary>
    /// This class contains the actual core logic used for image formatting.
    /// </summary>
    public sealed class ImageFormatting
    {
        /// <summary>
        /// Formats an image and publishes all of its formats to the specified container.
        /// </summary>
        public static async Task<FormattedImage[]> FormatAsync(Uri imageUrl, string publishStorageContainerName, string filenamePrefix, ImageFormat[] formats, IImageDownloader imageDownloader, IFormattedImagePublisher publisher, CancellationToken cancel)
        {
            Helpers.Argument.ValidateIsAbsoluteUrl(imageUrl, nameof(imageUrl));
            Helpers.Argument.ValidateIsNotNullOrWhitespace(publishStorageContainerName, nameof(publishStorageContainerName));
            Helpers.Argument.ValidateIsNotNullOrWhitespace(filenamePrefix, nameof(filenamePrefix));

            var log = _rootLog.CreateChildSource(imageUrl.ToString());
            log.Info($"Formatting with {formats.Length} formats.");

            var duration = Stopwatch.StartNew();

            try
            {
                using (var imageMagick = new ImageMagick())
                using (var workingDirectory = new TemporaryDirectory())
                {
                    log.Debug("Downloading input image.");

                    // Name it .bin to signal that we do not care about the extension - it is just binary data.
                    var inputPath = Path.Combine(workingDirectory.Path, "Input.bin");

                    using (var inputStream = await imageDownloader.GetStreamAsync(imageUrl, cancel))
                    using (var inputFile = File.Create(inputPath))
                        await inputStream.CopyToAsync(inputFile, 81920 /* Default */, cancel);

                    // Format the image using each format.
                    log.Debug("Starting formatting subtasks.");

                    var formatSubtasks = new List<(ImageFormat format, string filename, string outputPath, Task resizeTask)>();

                    foreach (var format in formats)
                    {
                        // Output is always JPG, even when PNG might be better. This inefficiency is by (poor) design.
                        var outputFilename = filenamePrefix + $"-{format.Height}p.jpg";
                        var outputPath = Path.Combine(workingDirectory.Path, outputFilename);

                        var resizeTask = imageMagick.ResizeAsync(inputPath, outputPath, format.Width, format.Height);

                        formatSubtasks.Add((format, outputFilename, outputPath, resizeTask));
                    }

                    await Task.WhenAll(formatSubtasks.Select(subtask => subtask.resizeTask));

                    // Upload each formatted image and return the URLs per format.
                    log.Debug("Starting upload subtasks.");

                    var uploadSubtasks = new List<(ImageFormat format, Task<string> uploadTask)>();

                    foreach (var formatTask in formatSubtasks)
                    {
                        var publishFilename = Path.GetFileName(formatTask.outputPath);

                        var uploadTask = Task.Run(() => publisher.PublishAsync(formatTask.outputPath, publishStorageContainerName, publishFilename, cancel));

                        uploadSubtasks.Add((formatTask.format, uploadTask));
                    }

                    await Task.WhenAll(uploadSubtasks.Select(subtask => subtask.uploadTask));

                    log.Info($"Finished image formatting in {duration.Elapsed.TotalSeconds:F2} seconds.");

                    return uploadSubtasks
                        .Select(subtask => new FormattedImage(subtask.format.Name, new Uri(subtask.uploadTask.Result)))
                        .ToArray();
                }
            }
            catch (Exception ex)
            {
                // Log it here so that it is more closely correlated with the specific image.
                log.Error(ex.Demystify().ToString());
                throw;
            }
        }

        public sealed class FormattedImage
        {
            public string FormatName { get; }
            public Uri Url { get; }

            public FormattedImage(string formatName, Uri url)
            {
                Helpers.Argument.ValidateIsNotNullOrWhitespace(formatName, nameof(formatName));
                Helpers.Argument.ValidateIsAbsoluteUrl(url, nameof(url));

                FormatName = formatName;
                Url = url;
            }
        }

        private static readonly LogSource _rootLog = Log.Default.CreateChildSource(nameof(ImageFormatting));
    }
}
