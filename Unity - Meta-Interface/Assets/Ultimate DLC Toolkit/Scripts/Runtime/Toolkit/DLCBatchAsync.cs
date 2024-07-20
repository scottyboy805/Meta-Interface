using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace DLCToolkit
{
    /// <summary>
    /// A batch async awaitable operation that contains multiple sub async operations.
    /// Can represent the progress and status of all operations combined and will wait until all operations have finished.
    /// Note that all sub operations will run in parallel in most cases, so a batch operation will only usually take as much time as the longest sub operation in most cases, plus some minor overhead for management.
    /// </summary>
    /// <typeparam name="T">The generic type returned by each sub operation</typeparam>
    public sealed class DLCBatchAsync<T> : DLCAsync<T[]>
    {
        // Private
        private List<DLCAsync<T>> items = null;

        // Properties
        /// <summary>
        /// Get the total number of async operations in this batch.
        /// </summary>
        public int TotalCount
        {
            get { return items.Count; }
        }

        /// <summary>
        /// Get the total number of async operations in this batch that have not yet completed.
        /// </summary>
        public int InProgressCount
        {
            get { return items.Where(i => i.IsDone == false).Count(); }
        }

        /// <summary>
        /// Get the total number of async operations in this batch that have completed with or without error.
        /// </summary>
        public int CompletedCount
        {
            get { return items.Where(i => i.IsDone == true).Count(); }
        }

        /// <summary>
        /// Get access to each individual async task that is contained in this batch.
        /// Useful to access progress and status of a specific operation rather that using the total combined <see cref="DLCAsync.Progress"/>.
        /// </summary>
        public IReadOnlyList<DLCAsync<T>> Tasks
        {
            get { return items; }
        }

        // Constructor
        internal DLCBatchAsync(IEnumerable<DLCAsync<T>> items)
        {
            this.items = new List<DLCAsync<T>>(items);
        }

        // Methods
        internal IEnumerator UpdateTasksRoutine()
        {
            while (IsDone == false)
            {
                UpdateTasks();
                yield return null;
            }
        }

        /// <summary>
        /// Called when this async operation can update.
        /// </summary>
        protected override void UpdateTasks()
        {
            // Get markers
            int completed = CompletedCount;
            int total = TotalCount;

            // Update status
            UpdateStatus(string.Format("Batch in progress ({0} / {1})", completed, total));

            // Calculate progress
            UpdateProgress(completed, total);


            // Check for all requests completed
            if(AllCompleted() == true)
            {
                // Check for success
                if(AllSuccessful() == true)
                {
                    this.Complete(true, items.Select(i => i.Result).ToArray());
                }
                else
                {
                    // Get number of successful
                    int successfulCount = items.Where(i => i.IsSuccessful == true).Count();

                    this.Error(string.Format("({0} / {1}) successful", successfulCount, TotalCount));
                }
            }
        }

        private bool AllCompleted()
        {
            return CompletedCount == TotalCount;
        }

        private bool AllSuccessful()
        {
            return items.Where(i => i.IsSuccessful == true).Count() == TotalCount;
        }
    }

    /// <summary>
    /// A batch async awaitable operation that contains multiple sub async operations.
    /// Can represent the progress and status of all operations combined and will wait until all operations have finished.
    /// Note that all sub operations will run in parallel in most cases, so a batch operation will only usually take as much time as the longest sub operation in most cases, plus some minor overhead for management.
    /// </summary>
    /// <typeparam name="T">The generic type returned by each sub operation</typeparam>
    public sealed class DLCBatchAsync : DLCAsync
    {
        // Private
        private List<DLCAsync> items = null;

        // Properties
        /// <summary>
        /// Get the total number of async operations in this batch.
        /// </summary>
        public int TotalCount
        {
            get { return items.Count; }
        }

        /// <summary>
        /// Get the total number of async operations in this batch that have not yet completed.
        /// </summary>
        public int InProgressCount
        {
            get { return items.Where(i => i.IsDone == false).Count(); }
        }

        /// <summary>
        /// Get the total number of async operations in this batch that have completed with or without error.
        /// </summary>
        public int CompletedCount
        {
            get { return items.Where(i => i.IsDone == true).Count(); }
        }

        /// <summary>
        /// Get access to each individual async task that is contained in this batch.
        /// Useful to access progress and status of a specific operation rather that using the total combined <see cref="DLCAsync.Progress"/>.
        /// </summary>
        public IReadOnlyList<DLCAsync> Tasks
        {
            get { return items; }
        }

        // Constructor
        internal DLCBatchAsync(IEnumerable<DLCAsync> items)
        {
            this.items = new List<DLCAsync>(items);
        }

        // Methods
        internal IEnumerator UpdateTasksRoutine()
        {
            while (IsDone == false)
            {
                UpdateTasks();
                yield return null;
            }
        }

        /// <summary>
        /// Called when this async operation can update.
        /// </summary>
        protected override void UpdateTasks()
        {
            // Get markers
            int completed = CompletedCount;
            int total = TotalCount;

            // Update status
            UpdateStatus(string.Format("Batch in progress ({0} / {1})", completed, total));

            // Calculate progress
            UpdateProgress(completed, total);


            // Check for all requests completed
            if (AllCompleted() == true)
            {
                // Check for success
                if (AllSuccessful() == true)
                {
                    this.Complete(true);
                }
                else
                {
                    // Get number of successful
                    int successfulCount = items.Where(i => i.IsSuccessful == true).Count();

                    this.Error(string.Format("({0} / {1}) successful", successfulCount, TotalCount));
                }
            }
        }

        private bool AllCompleted()
        {
            return CompletedCount == TotalCount;
        }

        private bool AllSuccessful()
        {
            return items.Where(i => i.IsSuccessful == true).Count() == TotalCount;
        }
    }
}
