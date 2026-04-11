using System;
using System.Threading.Tasks;

namespace StableDiffusionTagManager.ViewModels
{
    /// <summary>
    /// Implemented by interrogation ViewModels that can adapt their arguments
    /// per-folder during batch operations. The batch operation initializes the
    /// interrogator once, then calls <see cref="GetFolderInterrogateOperation"/>
    /// for each folder to obtain a closure that uses that folder's effective settings.
    /// </summary>
    public interface IFolderAwareInterrogationViewModel<T>
    {
        /// <summary>
        /// Returns an interrogate delegate that uses the supplied folder-specific
        /// settings, falling back to the ViewModel's own values when the overrides
        /// are null or empty.
        /// </summary>
        Func<byte[], Action<string>, Action<string>, Task<T>> GetFolderInterrogateOperation(
            string? effectivePrompt,
            string? effectiveEndpointUrl);
    }
}
