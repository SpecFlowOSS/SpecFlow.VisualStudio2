using System;
using Microsoft.VisualStudio.Text.Editor;

namespace SpecFlow.VisualStudio.Editor.EditorCommands
{
    public abstract class GherkinEditorCommandBase : IGherkinEditorCommand
    {
        public virtual GherkinEditorCommandTargetKey Target
        {
            get { throw new NotImplementedException(); }
        }

        public virtual GherkinEditorCommandTargetKey[] Targets
        {
            get { return new [] { Target }; }
        }

        public virtual GherkinEditorCommandStatus QueryStatus(IWpfTextView textView, Guid commandGroup, uint commandId)
        {
            return GherkinEditorCommandStatus.Supported;
        }

        public virtual bool PreExec(IWpfTextView textView, Guid commandGroup, uint commandId, IntPtr inArgs)
        {
            return false;
        }

        public virtual bool PostExec(IWpfTextView textView, Guid commandGroup, uint commandId, IntPtr inArgs)
        {
            return false;
        }
    }
}