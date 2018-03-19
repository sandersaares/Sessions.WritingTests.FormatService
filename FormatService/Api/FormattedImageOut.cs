using System;

namespace FormatService.Api
{
    /// <summary>
    /// Describes one image produced by a formatting job.
    /// </summary>
    public sealed class FormattedImageOut
    {
        /// <summary>
        /// URL of the source image used to generate this formatted image.
        /// </summary>
        public Uri SourceUrl { get; set; }

        /// <summary>
        /// URL of the image in Azure Blob Storage, including an access token that allows public access.
        /// </summary>
        public Uri Url { get; set; }

        /// <summary>
        /// Name of the image format used to generate this image.
        /// </summary>
        public string FormatName { get; set; }
    }
}
