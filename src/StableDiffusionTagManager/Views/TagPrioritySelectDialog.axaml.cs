using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using CommunityToolkit.Mvvm.Input;
using StableDiffusionTagManager.Controls;
using StableDiffusionTagManager.ViewModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StableDiffusionTagManager.Views
{
    public partial class TagPrioritySelectDialog : Window, IDialogWithResultAsync<TagPrioritySetButtonViewModel?>
    {
        public TagPrioritySelectDialog()
        {
            InitializeComponent();
        }

        public static readonly StyledProperty<List<TagPrioritySetButtonViewModel>> TagPrioritySetsProperty =
            AvaloniaProperty.Register<TagPrioritySelectDialog, List<TagPrioritySetButtonViewModel>>(nameof(TagPrioritySets), new List<TagPrioritySetButtonViewModel>());

        /// <summary>
        /// Gets or sets the image to be displayed
        /// </summary>
        public List<TagPrioritySetButtonViewModel> TagPrioritySets
        {
            get => GetValue(TagPrioritySetsProperty);
            set => SetValue(TagPrioritySetsProperty, value);
        }

        public static readonly StyledProperty<TagPrioritySetButtonViewModel?> SelectedTagPrioritySetProperty =
            AvaloniaProperty.Register<TagPrioritySelectDialog, TagPrioritySetButtonViewModel?>(nameof(SelectedTagPrioritySet));

        /// <summary>
        /// Gets or sets the image to be displayed
        /// </summary>
        public TagPrioritySetButtonViewModel? SelectedTagPrioritySet
        {
            get => GetValue(SelectedTagPrioritySetProperty);
            set => SetValue(SelectedTagPrioritySetProperty, value);
        }

        public bool Success { get; set; } = false;

        [RelayCommand]
        public void Save()
        {
            this.Success = true;
            Close();
        }

        [RelayCommand]
        public void Cancel()
        {
            Close();
        }

        public async Task<TagPrioritySetButtonViewModel?> ShowWithResult(Window parent)
        {
            await ShowDialog(parent);

            if (this.Success)
            {
                return SelectedTagPrioritySet;
            }
            else
            {
                return null;
            }
        }

        public void WindowKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
        }
    }
}
