using System;

namespace StableDiffusionTagManager.Attributes
{
    public  class SupportedPlatformsAttribute : Attribute
    {
        public SupportedPlatformsAttribute(params string[] platforms)
        {
            Platforms = platforms;
        }

        public string[] Platforms { get; }
    }
}
