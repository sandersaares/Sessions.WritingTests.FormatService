using Axinom.Toolkit;
using FormatService.Api;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FormatService
{
    [Route("[controller]")]
    public sealed class JobsController : Controller
    {
        /// <summary>
        /// All the currently running or recently finished jobs known to this service.
        /// </summary>
        private static readonly Dictionary<Guid, JobState> _jobs = new Dictionary<Guid, JobState>();

        // Synchronizes access to _jobs.
        private static readonly SemaphoreSlim _jobsLock = new SemaphoreSlim(1);

        // Completed jobs are removed after this much time expires.
        // The idea is to give enough time to read the result but not keep it around for too much longer.
        private static readonly TimeSpan CompletedJobExpirationDelay = TimeSpan.FromHours(1);

        // Collects all the job state that we want to store in memory.
        private sealed class JobState
        {
            public JobIn Input { get; set; }

            public Task<ImageFormatting.FormattedImage[]> FormattingTask { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody]JobIn job, CancellationToken cancel)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var jobId = Guid.NewGuid();

            // Start the actual formatting. The job ID is used as the prefix for all output filenames.
            var formattingTask = Task.Run(() => ImageFormatting.FormatAsync(job.ImageUrl, job.OutputStorageContainerName, jobId.ToString(), cancel));

            // And make it expire some time after the task is done, to avoid leaving garbage in memory.
            formattingTask.ContinueWith(async delegate (Task t)
            {
                await Task.Delay(CompletedJobExpirationDelay);

                using (await SemaphoreLock.TakeAsync(_jobsLock))
                    _jobs.Remove(jobId);
            }).Forget();

            using (await SemaphoreLock.TakeAsync(_jobsLock, cancel))
            {
                _jobs[jobId] = new JobState
                {
                    Input = job,
                    FormattingTask = formattingTask
                };
            }

            return Json(new JobOut
            {
                Id = jobId
            });
        }

        [HttpGet("{jobId}")]
        public async Task<IActionResult> Get(Guid jobId, CancellationToken cancel)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            JobState job;
            using (await SemaphoreLock.TakeAsync(_jobsLock, cancel))
            {
                if (!_jobs.ContainsKey(jobId))
                    return NotFound();

                job = _jobs[jobId];
            }

            // Job is still in progress, no useful status to report.
            if (!job.FormattingTask.IsCompleted)
                return Json(new JobOut
                {
                    Id = jobId
                });

            // Something went wrong! Report the problem.
            if (job.FormattingTask.IsCanceled || job.FormattingTask.IsFaulted)
                return Json(new JobOut
                {
                    Id = jobId,
                    IsCompleted = true,
                    Error = job.FormattingTask.Exception.ToString()
                });

            // Job has finished with success. Grab the output and convert to our API data model.
            var formattedImages = new List<FormattedImageOut>();

            foreach (var image in job.FormattingTask.Result)
            {
                formattedImages.Add(new FormattedImageOut
                {
                    SourceUrl = job.Input.ImageUrl,

                    FormatName = image.FormatName,
                    Url = image.Url
                });
            }

            return Json(new JobOut
            {
                Id = jobId,
                IsCompleted = true,
                OutputImages = formattedImages.ToArray()
            });
        }
    }
}
