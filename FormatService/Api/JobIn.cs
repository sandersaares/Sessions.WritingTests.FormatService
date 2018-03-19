using System;
using System.ComponentModel.DataAnnotations;

namespace FormatService.Api
{
    public sealed class JobIn
    {
        /// <summary>
        /// URL of the image to format as part of this job.
        /// </summary>
        [Required]
        public Uri ImageUrl { get; set; }

        /// <summary>
        /// Name of the container to use in Azure storage when publishing images. Will be created if missing.
        /// The account will be taken from service configuration and cannot be chosen manually.
        /// </summary>
        [Required]
        [RegularExpression("[a-z0-9-._]+")]
        public string OutputStorageContainerName { get; set; }
    }
}
