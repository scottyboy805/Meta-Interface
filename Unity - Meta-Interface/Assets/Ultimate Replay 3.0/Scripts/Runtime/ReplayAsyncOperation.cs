using UnityEngine;

namespace UltimateReplay
{
    public sealed class ReplayAsyncOperation<T> : ReplayAsyncOperation
    {
        // Private
        private T result = default;

        // Properties
        public T Result
        {
            get { return result; }
        }

        // Constructor
        internal ReplayAsyncOperation() { }

        internal ReplayAsyncOperation(T result)
        {
            this.result = result;
        }

        // Methods
        internal void UpdateResult(T result)
        {
            this.result = result;
        }
    }

    /// <summary>
    /// An awaitable object that is used to report when an async operation has finished.
    /// </summary>
    public class ReplayAsyncOperation : CustomYieldInstruction
    {
        // Private
        private bool isDone = false;
        private bool success = false;
        private float progress = 0f;
        private string error = "";

        // Properties
        /// <summary>
        /// Returns true if the associated async operation is not yet completed.
        /// </summary>
        public override bool keepWaiting
        {
            get { return isDone == false; }
        }

        /// <summary>
        /// Check whether the associated async operation has finished.
        /// </summary>
        public bool IsDone
        {
            get { return isDone; }
        }

        /// <summary>
        /// Check whether the associated async operation was successful.
        /// </summary>
        public bool Success
        {
            get { return success; }
        }

        public float Progress
        {
            get { return progress; }
        }

        public string Error
        {
            get { return error; }
        }

        // Constructor
        internal ReplayAsyncOperation() { }

        // Methods
        internal void UpdateProgress(float progress)
        {
            this.progress = Mathf.Clamp01(progress);
        }

        internal void Complete(bool success, string error = "")
        {
            this.isDone = true;
            this.success = success;
            this.error = error;

            // Report error
            if (success == false && string.IsNullOrEmpty(error) == false)
                Debug.LogError(error);
        }
    }
}
