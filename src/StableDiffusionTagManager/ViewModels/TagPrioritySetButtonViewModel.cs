﻿using StableDiffusionTagManager.Models;

namespace StableDiffusionTagManager.ViewModels
{
    public class TagPrioritySetButtonViewModel : ViewModelBase
    {
        public TagPrioritySetButtonViewModel(string name, TagPrioritySet prioritySet)
        {
            this.name = name;
            PrioritySet = prioritySet;
        }

        private string name;

        public string Name
        {
            get { return name; }
            set
            {
                name = value;
                OnPropertyChanged();
            }
        }

        public TagPrioritySet PrioritySet { get; }
    }
}
