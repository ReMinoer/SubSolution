using System;
using System.Collections.Generic;
using System.Reflection;
using SubSolution.Utils;

namespace SubSolution
{
    static public class ProjectTypes
    {
        static public Guid FolderGuid { get; } 
        static public Dictionary<ProjectType, string> DisplayNames { get; }
        static public Dictionary<ProjectType, Guid> Guids { get; }
        static public Dictionary<Guid, ProjectType> ByGuids { get; }

        static ProjectTypes()
        {
            Type enumType = typeof(ProjectType);

            DisplayNames = new Dictionary<ProjectType, string>();
            Guids = new Dictionary<ProjectType, Guid>();
            ByGuids = new Dictionary<Guid, ProjectType>();

            foreach (string name in Enum.GetNames(enumType))
            {
                var type = Enum.Parse<ProjectType>(name);
                MemberInfo memberInfo = enumType.GetMember(name)[0];

                string displayName = memberInfo.GetCustomAttribute<ProjectTypeNameAttribute>().DisplayName;
                Guid guid = memberInfo.GetCustomAttribute<ProjectTypeGuidAttribute>().Guid;

                DisplayNames.Add(type, displayName);
                Guids.Add(type, guid);
                ByGuids.Add(guid, type);

                if (type == ProjectType.Folder)
                    FolderGuid = guid;
            }
        }
    }
}