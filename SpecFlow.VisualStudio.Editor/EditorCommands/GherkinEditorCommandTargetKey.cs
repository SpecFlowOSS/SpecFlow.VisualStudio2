using System;
using Microsoft.VisualStudio;

namespace SpecFlow.VisualStudio.Editor.EditorCommands
{
    public struct GherkinEditorCommandTargetKey
    {
        public readonly Guid CommandGroup;
        public readonly uint CommandId;

        public GherkinEditorCommandTargetKey(Guid commandGroup, VSConstants.VSStd2KCmdID commandId) : this(commandGroup, (uint)commandId)
        {
        }

        public GherkinEditorCommandTargetKey(Guid commandGroup, uint commandId)
        {
            CommandGroup = commandGroup;
            CommandId = commandId;
        }

        #region Equality

        public bool Equals(GherkinEditorCommandTargetKey other)
        {
            return CommandGroup.Equals(other.CommandGroup) && CommandId == other.CommandId;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is GherkinEditorCommandTargetKey && Equals((GherkinEditorCommandTargetKey) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (CommandGroup.GetHashCode()*397) ^ (int) CommandId;
            }
        }

        #endregion
    }
}