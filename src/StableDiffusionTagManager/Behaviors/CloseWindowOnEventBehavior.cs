using Avalonia;
using Avalonia.Controls;
using Avalonia.Xaml.Interactivity;
using System;
using System.Reflection;

namespace StableDiffusionTagManager.Behaviors
{
    public class CloseWindowOnEventBehavior : Behavior<Window>
    {
        public static readonly StyledProperty<string> EventNameProperty =
            AvaloniaProperty.Register<CloseWindowOnEventBehavior, string>(nameof(EventName));

        public string EventName
        {
            get => GetValue(EventNameProperty);
            set => SetValue(EventNameProperty, value);
        }

        private EventInfo? _eventInfo;
        private Delegate? _eventHandler;

        protected override void OnAttached()
        {
            base.OnAttached();
            if (AssociatedObject != null)
            {
                AssociatedObject.DataContextChanged += OnDataContextChanged;
                AttachHandler(AssociatedObject.DataContext);
            }
        }

        protected override void OnDetaching()
        {
            if (AssociatedObject != null)
            {
                AssociatedObject.DataContextChanged -= OnDataContextChanged;
                DetachHandler();
            }
            base.OnDetaching();
        }

        private void OnDataContextChanged(object? sender, EventArgs e)
        {
            DetachHandler();
            AttachHandler(AssociatedObject?.DataContext);
        }

        private void AttachHandler(object? dataContext)
        {
            if (dataContext != null && !string.IsNullOrEmpty(EventName))
            {
                _eventInfo = dataContext.GetType().GetEvent(EventName);
                if (_eventInfo != null)
                {
                    _eventHandler = Delegate.CreateDelegate(_eventInfo.EventHandlerType!, this, nameof(OnEventRaised));
                    _eventInfo.AddEventHandler(dataContext, _eventHandler);
                }
            }
        }

        private void DetachHandler()
        {
            if (_eventInfo != null && _eventHandler != null)
            {
                _eventInfo.RemoveEventHandler(AssociatedObject?.DataContext, _eventHandler);
                _eventInfo = null;
                _eventHandler = null;
            }
        }

        private void OnEventRaised(object? sender, EventArgs e)
        {
            AssociatedObject?.Close();
        }
    }
}