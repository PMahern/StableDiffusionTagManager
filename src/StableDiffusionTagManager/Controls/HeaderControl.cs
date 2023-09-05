using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.VisualTree;
using CommunityToolkit.Mvvm.Input;
using System.Linq;

namespace StableDiffusionTagManager.Controls
{
    public class HeaderControl : ContentControl
    {
        public HeaderControl()
        {
            title = "";
            this.TemplateApplied += OnTemplateApplied;
        }

        public static new readonly DirectProperty<HeaderControl, Thickness> PaddingProperty =
           AvaloniaProperty.RegisterDirect<HeaderControl, Thickness>(
               nameof(Padding),
               o => o.Padding,
               (c, v) => c.Padding = v);

        private Thickness padding = new Thickness(5);
        public new Thickness Padding
        {
            get => padding;
            set
            {
                if (!SetAndRaise(PaddingProperty, ref padding, value)) return;
                var presenter = this.GetVisualDescendants().OfType<ContentPresenter>().FirstOrDefault(desc => desc.Name == "Presenter");
                if (presenter != null)
                {
                    presenter.Padding = padding;
                }
            }
        }

        public static readonly DirectProperty<HeaderControl, string> TitleProperty =
            AvaloniaProperty.RegisterDirect<HeaderControl, string>(
                nameof(Title),
                o => o.Title,
                (c, v) => c.Title = v);

        private string title;
        /// <summary>
        /// Gets or sets if control can render the image
        public string Title
        {
            get => title;
            set
            {
                if (!SetAndRaise(TitleProperty, ref title, value)) return;
                var label = this.GetVisualDescendants().OfType<Label>().FirstOrDefault(desc => desc.Name == "TitleLabel");
                if (label != null)
                {
                    label.Content = title;
                }
            }
        }

        public static readonly DirectProperty<HeaderControl, RelayCommand?> CloseCommandProperty =
            AvaloniaProperty.RegisterDirect<HeaderControl, RelayCommand?>(
                nameof(CloseCommand),
                o => o.CloseCommand,
                (c, v) => c.CloseCommand = v);

        private RelayCommand? closeCommand;
        /// <summary>
        /// Gets or sets if control can render the image
        public RelayCommand? CloseCommand
        {
            get => closeCommand;
            set
            {
                if (!SetAndRaise(CloseCommandProperty, ref closeCommand, value)) return;
                var closeButton = this.GetVisualDescendants().OfType<Button>().FirstOrDefault(desc => desc.Name == "CloseButton");
                if (closeButton != null)
                {
                    closeButton.IsVisible = closeCommand != null;
                    closeButton.Command = closeCommand;
                }

            }
        }

        private void OnTemplateApplied(object? sender, TemplateAppliedEventArgs e)
        {
            var label = this.GetVisualDescendants().OfType<Label>().FirstOrDefault(desc => desc.Name == "TitleLabel");
            if (label != null)
            {
                label.Content = title;
            }

            var closeButton = this.GetVisualDescendants().OfType<Button>().FirstOrDefault(desc => desc.Name == "CloseButton");
            if (closeButton != null)
            {
                closeButton.IsVisible = closeCommand != null;
                closeButton.Command = closeCommand;
            }

            var presenter = this.GetVisualDescendants().OfType<ContentPresenter>().FirstOrDefault(desc => desc.Name == "Presenter");
            if (presenter != null)
            {
                presenter.Padding = padding;
            }
        }
    }
}
