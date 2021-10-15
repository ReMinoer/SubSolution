using System;

namespace SubSolution.Utils
{
    [AttributeUsage(AttributeTargets.Field)]
    public class ProjectTypeGuidAttribute : Attribute
    {
        public Guid Guid { get; }

        public ProjectTypeGuidAttribute(string guidString)
        {
            if (Guid.TryParse(guidString, out Guid guid))
                Guid = guid;
        }
    }
}