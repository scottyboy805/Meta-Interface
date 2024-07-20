using System.Collections;
using UnityEngine;

namespace DLCToolkit
{
    /// <summary>
    /// A host that is able to manage the invocation of an async operation.
    /// </summary>
    public interface IDLCAsyncProvider
    {
        // Methods
        /// <summary>
        /// Start a new async operation.
        /// </summary>
        /// <param name="routine">The async method to invoke</param>
        /// <returns>The coroutine for the async method</returns>
        Coroutine RunAsync(IEnumerator routine);
    }
}
