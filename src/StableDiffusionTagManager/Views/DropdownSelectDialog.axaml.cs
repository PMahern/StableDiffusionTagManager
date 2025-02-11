using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using CommunityToolkit.Mvvm.Input;
using StableDiffusionTagManager.Controls;
using StableDiffusionTagManager.ViewModels;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StableDiffusionTagManager.Views
{
    public partial class DropdownSelectDialog : Window, IDialogWithResultAsync<DropdownSelectItem?>
    {
        public DropdownSelectDialog()
        {
            InitializeComponent();
        }

        public static readonly StyledProperty<List<DropdownSelectItem>> DropdownItemsProperty =
            AvaloniaProperty.Register<DropdownSelectDialog, List<DropdownSelectItem>>(nameof(DropdownItems), new List<DropdownSelectItem>());

        /// <summary>
        /// Gets or sets the image to be displayed
        /// </summary>
        public List<DropdownSelectItem> DropdownItems
        {
            get => GetValue(DropdownItemsProperty);
            set => SetValue(DropdownItemsProperty, value);
        }

        public static readonly StyledProperty<DropdownSelectItem?> SelectedItemProperty =
            AvaloniaProperty.Register<DropdownSelectDialog, DropdownSelectItem?>(nameof(SelectedItem));

        /// <summary>
        /// Gets or sets the image to be displayed
        /// </summary>
        public DropdownSelectItem? SelectedItem
        {
            get => GetValue(SelectedItemProperty);
            set => SetValue(SelectedItemProperty, value);
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

        public async Task<DropdownSelectItem?> ShowWithResult(Window parent)
        {
            await ShowDialog(parent);

            if (this.Success)
            {
                return SelectedItem;
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
