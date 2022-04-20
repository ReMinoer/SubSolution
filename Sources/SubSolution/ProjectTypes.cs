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

        static ProjectTypes()
        {
            Type enumType = typeof(ProjectType);

            DisplayNames = new Dictionary<ProjectType, string>();
            Guids = new Dictionary<ProjectType, Guid>();

            foreach (string name in Enum.GetNames(enumType))
            {
                var value = (ProjectType)Enum.Parse(typeof(ProjectType), name);
                MemberInfo memberInfo = enumType.GetMember(name)[0];

                string displayName = memberInfo.GetCustomAttribute<ProjectTypeNameAttribute>().DisplayName;
                Guid guid = memberInfo.GetCustomAttribute<ProjectTypeGuidAttribute>().Guid;

                DisplayNames.Add(value, displayName);
                Guids.Add(value, guid);

                if (value == ProjectType.Folder)
                    FolderGuid = guid;
            }
        }

        static public ProjectType? FromExtension(string extension, Func<bool> hasDotNetSdkFunc)
        {
            return ProjectFileExtensions.ByExtensions.TryGetValue(extension, out ProjectFileExtension projectExtension)
                ? FromExtension(projectExtension, hasDotNetSdkFunc)
                : null;
        }

        static public ProjectType? FromExtension(ProjectFileExtension projectExtension, Func<bool> hasDotNetSdkFunc)
        {
            switch (projectExtension)
            {
                case ProjectFileExtension.Csproj:
                    return hasDotNetSdkFunc() ? ProjectType.CSharpDotNetSdk : ProjectType.CSharpLegacy;
                case ProjectFileExtension.Fsproj:
                    return hasDotNetSdkFunc() ? ProjectType.FSharpDotNetSdk : ProjectType.FSharpLegacy;
                case ProjectFileExtension.Vbproj:
                    return hasDotNetSdkFunc() ? ProjectType.VisualBasicDotNetSdk : ProjectType.VisualBasicLegacy;
                case ProjectFileExtension.Vcxproj:
                case ProjectFileExtension.Vcproj:
                    return ProjectType.Cpp;
                case ProjectFileExtension.Njsproj:
                    return ProjectType.NodeJs;
                case ProjectFileExtension.Pyproj:
                    return ProjectType.Python;
                case ProjectFileExtension.Sqlproj:
                    return ProjectType.Sql;
                case ProjectFileExtension.Wapproj:
                    return ProjectType.Wap;
                case ProjectFileExtension.Shproj:
                    return ProjectType.Shared;
                case ProjectFileExtension.Vcxitems:
                    return ProjectType.SharedItems;
                default:
                    return null;
            }
        }

        static public ProjectType? FromGuidAndExtension(Guid projectTypeGuid, ProjectFileExtension extension)
        {
            switch (extension)
            {
                case ProjectFileExtension.Csproj:
                    if (projectTypeGuid == Guids[ProjectType.CSharpDotNetSdk])
                        return ProjectType.CSharpDotNetSdk;
                    else if (projectTypeGuid == Guids[ProjectType.CSharpLegacy])
                        return ProjectType.CSharpLegacy;
                    else
                        return null;
                case ProjectFileExtension.Fsproj:
                    if (projectTypeGuid == Guids[ProjectType.FSharpDotNetSdk])
                        return ProjectType.FSharpDotNetSdk;
                    else if (projectTypeGuid == Guids[ProjectType.FSharpLegacy])
                        return ProjectType.FSharpLegacy;
                    else
                        return null;
                case ProjectFileExtension.Vbproj:
                    if (projectTypeGuid == Guids[ProjectType.VisualBasicDotNetSdk])
                        return ProjectType.VisualBasicDotNetSdk;
                    else if (projectTypeGuid == Guids[ProjectType.VisualBasicLegacy])
                        return ProjectType.VisualBasicLegacy;
                    else
                        return null;
                case ProjectFileExtension.Vcxproj:
                case ProjectFileExtension.Vcproj:
                    return ProjectType.Cpp;
                case ProjectFileExtension.Njsproj:
                    return ProjectType.NodeJs;
                case ProjectFileExtension.Pyproj:
                    return ProjectType.Python;
                case ProjectFileExtension.Sqlproj:
                    return ProjectType.Sql;
                case ProjectFileExtension.Wapproj:
                    return ProjectType.Wap;
                case ProjectFileExtension.Shproj:
                    return ProjectType.Shared;
                case ProjectFileExtension.Vcxitems:
                    return ProjectType.SharedItems;
                default:
                    return null;
            }
        }
    }
}