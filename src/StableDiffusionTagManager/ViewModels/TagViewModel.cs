using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace StableDiffusionTagManager.ViewModels
{
    public class TagViewModel : ObservableObject
    {
        public TagViewModel(string tag)
        {
            Tag = tag;
        }

        private string tag;
        public string Tag
        {
            get => tag;
            set {
                var temp = tag;
                tag = value;
                TagChanged?.Invoke(temp, value);
                OnPropertyChanged(); 
            }
        }

        public Action<string, string>? TagChanged;

        private bool isBeingDragged;
        public bool IsBeingDragged
        {
            get { return isBeingDragged; }
            set
            {
                isBeingDragged = value;
                OnPropertyChanged();
            }
        }

        //private TagViewModel draggeOverSource;
        //public TagViewModel DraggeOverSource
        //{
        //    get { return draggeOverSource; }
        //    set
        //    {
        //        draggeOverSource = value;
        //        OnPropertyChanged();
        //    }
        //}

        //private bool isPlaceHolder;

        //public bool IsPlaceHolder
        //{
        //    get { return isPlaceHolder; }
        //    set { isPlaceHolder = value; OnPropertyChanged(); }
        //}

        //private bool isFocused;

        //public bool IsFocused
        //{
        //    get { return isFocused; }
        //    set { isFocused = value; OnPropertyChanged(); }
        //}


        //public Visibility DropPreviewVisibility => DraggeOverSource != null ? Visibility.Visible : Visibility.Collapsed;
        //public Color BackgroundColor => isPlaceHolder ? Color.Parse("Orange") : 
        //                               isBeingDragged ? Color.Parse("Grey") : Color.Parse("LightBlue"); 
    }
}
