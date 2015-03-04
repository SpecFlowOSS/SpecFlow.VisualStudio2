using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Text.Editor;

namespace SpecFlow.VisualStudio.Editor.EditorCommands
{
    public abstract class GherkinEditorTypeCharCommandBase : GherkinEditorCommandBase
    {
        public override GherkinEditorCommandTargetKey Target
        {
            get { return new GherkinEditorCommandTargetKey(VSConstants.VSStd2K, VSConstants.VSStd2KCmdID.TYPECHAR); }
        }

        private char GetTypeChar(IntPtr inArgs)
        {
            return (char)(ushort)Marshal.GetObjectForNativeVariant(inArgs);
        }

        public override bool PostExec(IWpfTextView textView, Guid commandGroup, uint commandId, IntPtr inArgs)
        {
            char ch = GetTypeChar(inArgs);
            return PostExec(textView, ch);
        }

        protected abstract bool PostExec(IWpfTextView textView, char ch);
    }
}