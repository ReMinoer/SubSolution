using System;

namespace SubSolution.Utils
{
    [AttributeUsage(AttributeTargets.Field)]
    public class ProjectTypeNameAttribute : Attribute
    {
        public string DisplayName { get; }

        public ProjectTypeNameAttribute(string displayName)
        {
            DisplayName = displayName;
        }
    }
}