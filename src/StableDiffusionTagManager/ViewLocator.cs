using Avalonia.Controls;
using Avalonia.Controls.Templates;
using StableDiffusionTagManager.ViewModels;
using System;

namespace StableDiffusionTagManager
{
    public class ViewLocator : IDataTemplate
    {
        public bool Match(object data)
        {
            return data is ViewModelBase;
        }

        Control? ITemplate<object?, Control?>.Build(object? param)
        {
            if(param != null)
            {
                var name = param.GetType().FullName!.Replace("ViewModel", "View");
                if (name != param.GetType().FullName)
                {
                    var type = Type.GetType(name);

                    if (type != null)
                    {
                        return (Control)Activator.CreateInstance(type)!;
                    }
                }
            }
            
            return new TextBlock { Text = "Param is null." };
        }
    }
}