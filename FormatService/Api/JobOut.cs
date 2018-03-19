using System;

namespace FormatService.Api
{
    public sealed class JobOut
    {
        /// <summary>
        /// Unique ID of the job.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Whether the job has been completed.
        /// </summary>
        public bool IsCompleted { get; set; }

        /// <summary>
        /// If this is not null, the job failed. This contains the error message.
        /// </summary>
        public string Error { get; set; }

        /// <summary>
        /// The results of the job. Available once IsCompleted is true.
        /// </summary>
        public FormattedImageOut[] OutputImages { get; set; }
    }
}
