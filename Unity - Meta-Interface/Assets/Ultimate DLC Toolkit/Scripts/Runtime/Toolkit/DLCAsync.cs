using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DLCToolkit
{
    /// <summary>
    /// An awaitable object that is returned by async operations so you can wait for completion in a coroutine as well as access progress and status information.
    /// Used to wait for an async operation to be completed with a result object.
    /// </summary>
    /// <typeparam name="T">The generic result type</typeparam>
    public class DLCAsync<T> : DLCAsync
    {
        // Private
        private T result = default;
        private TaskCompletionSource<T> taskSource = null;

        // Properties
        /// <summary>
        /// Get the <see cref="System.Threading.Tasks.Task"/> for this async operation for use in C# async/await contexts.
        /// </summary>
        public new Task<T> Task
        {
            get
            {
                if (taskSource == null)
                {
                    // Create the awaitable task
                    taskSource = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);

                    // Check for completed
                    if (IsDone == true)
                    {
                        if (isSuccessful == false)
                            taskSource.SetException(new Exception(status));
                        else
                            taskSource.SetResult(default);
                    }
                }
                return taskSource.Task;
            }
        }

        /// <summary>
        /// Get the generic result of the async operation. 
        /// </summary>
        public new T Result
        {
            get
            {
                try
                {
                    // Try to cast
                    return (T)result;
                }
                catch
                {
                    return default(T);
                }
            }
        }

        // Constructor
        /// <summary>
        /// Create a new instance.
        /// </summary>
        /// <param name="awaitable">Can this operation be awaited o the main thread, by blocking until the task has completed. This is only possible for requests that run on a background thread, and UnityWebRequest for example cannot support this</param>
        public DLCAsync(bool awaitable = true)
            : base(awaitable)
        { 
        }

        // Methods
        /// <summary>
        /// Update the status message for this operation.
        /// Useful to show the current status if a failure occurs.
        /// </summary>
        /// <param name="status">The status message</param>
        protected internal new DLCAsync<T> UpdateStatus(string status)
        {
            base.UpdateStatus(status);
            return this;
        }

        /// <summary>
        /// Update the load progress for this operation.
        /// Calculates the progress as a value from 0-1 based on the input values.
        /// </summary>
        /// <param name="current">The current number of tasks that have been completed</param>
        /// <param name="total">The total number of tasks that should be completed</param>
        protected internal new DLCAsync<T> UpdateProgress(int current, int total)
        {
            base.UpdateProgress(current, total);
            return this;
        }

        /// <summary>
        /// Update the load progress for this operation.
        /// The specified progress value should be in the range of 0-1, and will be clamped if not.
        /// </summary>
        /// <param name="progress">The current progress value between 0-1</param>
        protected internal new DLCAsync<T> UpdateProgress(float progress)
        {
            base.UpdateProgress(progress);
            return this;
        }

        /// <summary>
        /// Reset the async operation.
        /// </summary>
        public override void Reset()
        {
            base.Reset();
            taskSource = null;
        }

        /// <summary>
        /// Complete the operation with an error status.
        /// This will cause <see cref="DLCAsync.IsDone"/> to become true and <see cref="DLCAsync.Progress"/> to become 1.
        /// </summary>
        /// <param name="status">The error message for the failure</param>
        /// <param name="result">An optional result object</param>
        public void Error(string status, T result = default)
        {
            this.result = result;
            base.Error(status);

            // Check for task
            if (taskSource != null)
                taskSource.SetException(new Exception(status));
        }

        /// <summary>
        /// Complete the operation with the specified success status.
        /// This will cause <see cref="DLCAsync.IsDone"/> to become true and <see cref="DLCAsync.Progress"/> to become 1.
        /// </summary>
        /// <param name="success">Was the operation completed successfully</param>
        /// <param name="result">An optional result object</param>
        public void Complete(bool success, T result = default)
        {
            this.result = result;
            base.Complete(success, result as Object);

            // Check for task
            if(taskSource != null)
                taskSource.SetResult(result);
        }

        /// <summary>
        /// Create a new instance with the specified success status.
        /// This will cause <see cref="DLCAsync.IsDone"/> to become true and <see cref="DLCAsync.Progress"/> to become 1.
        /// </summary>
        /// <param name="success">Was the operation completed successfully</param>
        /// <param name="result">An optional result object</param>
        public static DLCAsync<T> Completed(bool success, T result = default)
        {
            DLCAsync<T> async = new DLCAsync<T>();
            async.Complete(success, result);

            return async;
        }

        /// <summary>
        /// Create a new instance an error status.
        /// This will cause <see cref="DLCAsync.IsDone"/> to become true and <see cref="DLCAsync.Progress"/> to become 1.
        /// </summary>
        /// <param name="error">The error message for the failure</param>
        public static new DLCAsync<T> Error(string error)
        {
            DLCAsync<T> async = new DLCAsync<T>
            {
                status = error,
            };
            async.Complete(false);

            return async;
        }
    }

    /// <summary>
    /// An awaitable object that is returned by async operations so you can wait for completion in a coroutine as well as access progress and status information.
    /// Used to wait until an async operation has been completed
    /// </summary>
    public class DLCAsync : IEnumerator
    {
        // Internal
        internal bool awaitable = true;

        // Private
        private TaskCompletionSource<object> taskSource = null;

        // Protected
        /// <summary>
        /// Was the operation successful or did something go wrong.
        /// </summary>
        protected bool isSuccessful = false;
        /// <summary>
        /// Get the current status of the async operation.
        /// </summary>
        protected string status = string.Empty;

        // Private
        private Object result = null;
        private float progress = 0;
        private bool isDone = false;

        // Properties
        /// <summary>
        /// Get the <see cref="System.Threading.Tasks.Task"/> for this async operation for use in C# async/await contexts.
        /// </summary>
        public Task Task
        {
            get
            {
                if(taskSource == null)
                {
                    // Create the awaitable task
                    taskSource = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

                    // Check for completed
                    if (IsDone == true)
                    {
                        if (isSuccessful == false)
                            taskSource.SetException(new Exception(status));
                        else
                            taskSource.SetResult(null);
                    }
                }
                return taskSource.Task;
            }
        }

        /// <summary>
        /// Get the <see cref="Object"/> result of the async operation. 
        /// </summary>
        public Object Result
        {
            get { return result; }
            protected internal set { result = value; }
        }

        /// <summary>
        /// Get the current status of the async operation.
        /// </summary>
        public string Status
        {
            get { return status; }
            protected internal set { status = value; }
        }

        /// <summary>
        /// Get the current progress of the async operation.
        /// This is a normalized value between 0-1.
        /// </summary>
        public float Progress
        {
            get { return progress; }
            protected internal set { progress = Mathf.Clamp01(value); }
        }

        /// <summary>
        /// Get the current progress percentage of the async operation.
        /// </summary>
        public int ProgressPercentage
        {
            get { return Mathf.RoundToInt(progress * 100f); }
        }

        /// <summary>
        /// Returns true if the async operation has finished or false if it is still running.
        /// </summary>
        public bool IsDone
        {
            get { return isDone; }
        }

        /// <summary>
        /// Returns true if the async operation completed successfully or false if an error occurred.
        /// </summary>
        public bool IsSuccessful
        {
            get { return isSuccessful; }
        }

        /// <summary>
        /// IEnumerator.Current implementation.
        /// </summary>
        public object Current
        {
            get { return result; }
        }

        // Constructor
        /// <summary>
        /// Create new operation.
        /// </summary>
        /// <param name="awaitable">Can this operation be awaited o the main thread, by blocking until the task has completed. This is only possible for requests that run on a background thread, and UnityWebRequest for example cannot support this</param>
        public DLCAsync(bool awaitable = true) 
        { 
            this.awaitable = awaitable;
        }

        // Methods
        /// <summary>
        /// Called when the async operation can perform some logic.
        /// </summary>
        protected virtual void UpdateTasks() { }

        /// <summary>
        /// IEnumerator.MoveNext() implementation.
        /// </summary>
        /// <returns>True if the enumerator advanced successfully or false if not</returns>
        public bool MoveNext()
        {
            if (IsDone == false)
            {
                // Advance the enumerator (continue waiting)
                return true;
            }

            // Task is finished
            return false;
        }

        /// <summary>
        /// IEnumerator.Reset() implementation.
        /// </summary>
        public virtual void Reset()
        {
            taskSource = null;
            result = null;
            status = string.Empty;
            progress = 0;
            isDone = false;
        }

        /// <summary>
        /// Block the main thread until the async operation has completed.
        /// Use with caution. This can cause an infinite loop if the async operation never completes, if the operation is not true async, or if the async operation relies on data from the main thread.
        /// </summary>
        /// <exception cref="TimeoutException">The await operation took longer that the specified timeout milliseconds, so was aborted to avoid infinite waiting</exception>
        public void Await(long msTimeout = 10000)
        {
            // Check for already done
            if (isDone == true)
                return;

            // Check for awaitable
            if (awaitable == false)
                throw new InvalidOperationException("DLC async await caller does not support waiting on the main thread. You should use the equivalent async API for this call");

            // Get current time
            DateTime start = DateTime.Now;

            // Wait until complete
            while (isDone == false)
            {
                // Check if we have timed out
                if (msTimeout > 0 && (DateTime.Now - start).TotalMilliseconds > msTimeout)
                    throw new TimeoutException("DLC async await call was aborted because the operation timed out");

                // Block the thread
                Thread.Sleep(1);
            }
        }

        /// <summary>
        /// Update the status message for this operation.
        /// Useful to show the current status if a failure occurs.
        /// </summary>
        /// <param name="status">The status message</param>
        protected internal DLCAsync UpdateStatus(string status)
        {
            this.status = status;

            if(this.status == null)
                this.status = string.Empty;

            return this;
        }

        /// <summary>
        /// Update the load progress for this operation.
        /// Calculates the progress as a value from 0-1 based on the input values.
        /// </summary>
        /// <param name="current">The current number of tasks that have been completed</param>
        /// <param name="total">The total number of tasks that should be completed</param>
        protected internal DLCAsync UpdateProgress(int current, int total)
        {
            this.progress = Mathf.InverseLerp(0, total, current);
            return this;
        }

        /// <summary>
        /// Update the load progress for this operation.
        /// The specified progress value should be in the range of 0-1, and will be clamped if not.
        /// </summary>
        /// <param name="progress">The current progress value between 0-1</param>
        protected internal DLCAsync UpdateProgress(float progress)
        {
            this.progress = Mathf.Clamp01(progress);
            return this;
        }

        /// <summary>
        /// Complete the operation with an error status.
        /// This will cause <see cref="IsDone"/> to become true and <see cref="Progress"/> to become 1.
        /// </summary>
        /// <param name="status">The error message for the failure</param>
        /// <param name="result">An optional result object</param>
        public void Error(string status, Object result = null)
        {
            this.isSuccessful = false;
            this.status = status;
            this.result = result;
            this.progress = 1f;
            this.isDone = true;

            // Complete task
            if(taskSource != null)
                taskSource.SetException(new Exception(status));
        }

        /// <summary>
        /// Complete the operation with the specified success status.
        /// This will cause <see cref="IsDone"/> to become true and <see cref="Progress"/> to become 1.
        /// </summary>
        /// <param name="success">Was the operation completed successfully</param>
        /// <param name="result">An optional result object</param>
        public void Complete(bool success, Object result = null)
        {
            this.isSuccessful = success;
            this.result = result;
            this.progress = 1f;
            this.isDone = true;

            // Complete the task
            if (taskSource != null)
                taskSource.SetResult(null);
        }

        /// <summary>
        /// Create a new instance with the specified success status.
        /// This will cause <see cref="IsDone"/> to become true and <see cref="Progress"/> to become 1.
        /// </summary>
        /// <param name="success">Was the operation completed successfully</param>
        /// <param name="result">An optional result object</param>
        public static DLCAsync Completed(bool success, Object result = null)
        {
            return new DLCAsync
            {
                isSuccessful = success,
                result = result,
                progress = 1f,
                isDone = true,
            };
        }

        /// <summary>
        /// Create a new instance with an error status.
        /// This will cause <see cref="DLCAsync.IsDone"/> to become true and <see cref="DLCAsync.Progress"/> to become 1.
        /// </summary>
        /// <param name="error">The error message for the failure</param>
        public static DLCAsync Error(string error)
        {
            return new DLCAsync
            {
                status = error,
                isSuccessful = false,
                progress = 1f,
                isDone = true,
            };
        }
    }
}
