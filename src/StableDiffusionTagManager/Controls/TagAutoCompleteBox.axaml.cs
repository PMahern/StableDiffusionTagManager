using Avalonia.Controls;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Avalonia;

namespace StableDiffusionTagManager.Controls
{
    public partial class TagAutoCompleteBox : UserControl
    {
        public TagAutoCompleteBox()
        {
            InitializeComponent();
        }

        public async  Task<IEnumerable<object>> SearchTags(string text, CancellationToken token)
        {
            var app = App.Current as App;
            if (app != null)
            {
                return await app.SearchTags(text, token);
            }
            return Enumerable.Empty<object>();
        }

        public static readonly StyledProperty<string> TextProperty =
            AvaloniaProperty.Register<TagAutoCompleteBox, string>(nameof(Text), "", defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

        public string Text
        {
            get => GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public new void Focus()
        {
            this.AutoComplete.Focus();
        }
    }
}
