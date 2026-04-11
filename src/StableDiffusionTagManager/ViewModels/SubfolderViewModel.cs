using CommunityToolkit.Mvvm.ComponentModel;
using StableDiffusionTagManager.Models;

namespace StableDiffusionTagManager.ViewModels
{
    /// <summary>
    /// Represents a selectable entry in the subfolder dropdown.
    /// SubProject is the project file owned directly by this folder (may be null).
    /// Effective settings are resolved by the MainWindowViewModel by walking the
    /// ancestry chain from root to this folder.
    /// </summary>
    public partial class SubfolderViewModel : ObservableObject
    {
        public string DisplayName { get; }
        public string FolderPath { get; }

        /// <summary>
        /// The project file owned directly by this folder.
        /// Null means this folder has no _project.xml yet.
        /// Can be set lazily when the user first edits settings for this folder.
        /// </summary>
        [ObservableProperty]
        private Project? subProject;

        public SubfolderViewModel(string displayName, string folderPath, Project? subProject = null)
        {
            DisplayName = displayName;
            FolderPath = folderPath;
            SubProject = subProject;
        }

        public override string ToString() => DisplayName;
    }
}
