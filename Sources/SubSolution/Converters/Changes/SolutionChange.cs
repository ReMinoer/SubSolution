using System;
using SubSolution.FileSystems;

namespace SubSolution.Converters.Changes
{
    public class SolutionChange : IEquatable<SolutionChange>, IComparable<SolutionChange>
    {
        public SolutionChangeType ChangeType { get; }
        public SolutionObjectType ObjectType { get; }
        public string ObjectName { get; }
        public SolutionObjectType? TargetType { get; }
        public string? TargetName { get; }

        public object? ObjectTag { get; set; }

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
                    TargetName = targetName ?? "solution root";
                    break;
                case SolutionObjectType.ProjectContext:
                    TargetType = SolutionObjectType.ConfigurationPlatform;
                    break;
                case SolutionObjectType.SharedProject:
                    TargetType = SolutionObjectType.Project;
                    break;
                case SolutionObjectType.ConfigurationPlatform:
                    TargetType = null;
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        public string GetMessage(bool startWithBullet = false, IFileSystem? getFileNameFileSystem = null)
        {
            string objectName = FormatName(ObjectName, ObjectType, getFileNameFileSystem);
            string bullet = startWithBullet ? $"[{Bullet}] " : string.Empty;

            if (TargetName is null)
                return $"{bullet}{ChangeType} {ObjectType} \"{objectName}\"";

            string targetWord = ChangeType == SolutionChangeType.Remove || ChangeType == SolutionChangeType.Edit ? "from" : "to";
            string targetName = FormatName(TargetName, TargetType, getFileNameFileSystem);

            return $"{bullet}{ChangeType} {ObjectType} \"{objectName}\" {targetWord} \"{targetName}\"";
        }

        private string FormatName(string name, SolutionObjectType? objectType, IFileSystem? getFileNameFileSystem)
        {
            switch (objectType)
            {
                case SolutionObjectType.Project:
                case SolutionObjectType.ProjectContext:
                case SolutionObjectType.SharedProject:
                    return getFileNameFileSystem?.GetFileNameWithoutExtension(name) ?? name;
                case SolutionObjectType.File:
                    return getFileNameFileSystem?.GetName(name) ?? name;
                default:
                    return name;
            }
        }

        private char Bullet
        {
            get
            {
                switch (ChangeType)
                {
                    case SolutionChangeType.Add:
                        return '+';
                    case SolutionChangeType.Remove:
                        return '-';
                    case SolutionChangeType.Edit:
                        return '*';
                    case SolutionChangeType.Move:
                        return '>';
                    default:
                        throw new NotSupportedException();
                }
            }
        }

        public override string ToString() => GetMessage();

        public bool Equals(SolutionChange? other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;

            return ChangeType == other.ChangeType
                && ObjectType == other.ObjectType
                && ObjectName == other.ObjectName
                && TargetType == other.TargetType
                && TargetName == other.TargetName;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != GetType())
                return false;

            return Equals((SolutionChange)obj);
        }

        public override int GetHashCode() => (ChangeType, ObjectType, ObjectName, TargetType, TargetName).GetHashCode();

        public int CompareTo(SolutionChange? other)
        {
            if (ReferenceEquals(this, other))
                return 0;
            if (ReferenceEquals(null, other))
                return 1;

            int comparison = ChangeType.CompareTo(other.ChangeType);
            if (comparison != 0)
                return comparison;

            comparison = ObjectType.CompareTo(other.ObjectType);
            if (comparison != 0)
                return comparison;

            return string.Compare(ObjectName, other.ObjectName, StringComparison.Ordinal);
        }
    }
}