using System;

namespace SubSolution.Converters
{
    public class SolutionChange
    {
        public SolutionChangeType ChangeType { get; }
        public SolutionObjectType ObjectType { get; }
        public string ObjectName { get; }
        public SolutionObjectType? TargetType { get; }
        public string? TargetName { get; }

        public string Message { get; }

        public SolutionChange(SolutionChangeType changeType, SolutionObjectType objectType, string objectName, string? targetName)
        {
            ChangeType = changeType;
            ObjectType = objectType;
            ObjectName = objectName;
            TargetName = targetName;

            switch (objectType)
            {
                case SolutionObjectType.File:
                case SolutionObjectType.Project:
                case SolutionObjectType.Folder:
                    TargetType = SolutionObjectType.Folder;
                    TargetName = targetName ?? "solution path";
                    break;
                case SolutionObjectType.ProjectContext:
                    TargetType = SolutionObjectType.ConfigurationPlatform;
                    break;
                case SolutionObjectType.ConfigurationPlatform:
                    TargetType = null;
                    break;
                default:
                    throw new NotSupportedException();
            }

            if (targetName is null)
            {
                Message = $"{ChangeType} {ObjectType} \"{ObjectName}\"";
            }
            else
            {
                string targetWord = changeType == SolutionChangeType.Remove || changeType == SolutionChangeType.Edit ? "from" : "to";
                Message = $"{ChangeType} {ObjectType} \"{ObjectName}\" {targetWord} {TargetType} \"{TargetName}\"";
            }
        }

        public override string ToString() => Message;
    }
}