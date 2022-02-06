using System.IO;
using System.Windows;
using Microsoft.VisualStudio.Imaging.Interop;
using SubSolution.Converters.Changes;
using SubSolution.VisualStudio.Helpers;

namespace SubSolution.VisualStudio.ViewModels
{
    public class SolutionChangeViewModel
    {
        public SolutionChangeType ChangeType { get; }
        public SolutionObjectType ObjectType { get; }
        public string ObjectName { get; }
        public string ObjectPath { get; }

        public Visibility TargetVisibility { get; }
        public string? TargetWord { get; }
        public string? TargetName { get; }
        public SolutionObjectType? TargetType { get; }

        public ImageMoniker ChangeTypeMoniker { get; }
        public ImageMoniker? ObjectTypeMoniker { get; }
        public ImageMoniker? TargetTypeMoniker { get; }

        public SolutionChangeViewModel(SolutionChange change)
        {
            ChangeType = change.ChangeType;
            ObjectType = change.ObjectType;
            ObjectPath = change.ObjectName;
            ObjectName = ObjectType switch
            {
                SolutionObjectType.Project => Path.GetFileNameWithoutExtension(change.ObjectName),
                SolutionObjectType.File => Path.GetFileName(change.ObjectName),
                _ => change.ObjectName
            };

            if (change.TargetName is not null)
            {
                TargetVisibility = Visibility.Visible;
                TargetWord = ChangeType == SolutionChangeType.Remove || ChangeType == SolutionChangeType.Edit ? "from" : "to";
                TargetName = change.TargetName;
                TargetType = change.TargetType;
            }
            else
            {
                TargetVisibility = Visibility.Collapsed;
                TargetWord = null;
                TargetName = null;
                TargetType = null;
            }

            ChangeTypeMoniker = SubSolutionMonikers.GetChangeTypeMoniker(ChangeType);
            ObjectTypeMoniker = SubSolutionMonikers.GetObjectTypeMoniker(ObjectType);
            TargetTypeMoniker = SubSolutionMonikers.GetObjectTypeMoniker(change.TargetType);
        }
    }
}