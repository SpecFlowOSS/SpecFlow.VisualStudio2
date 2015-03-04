using System;
using Microsoft.VisualStudio.Text.Editor;

namespace SpecFlow.VisualStudio.Editor.EditorCommands.Infrastructure
{
    public interface IGherkinEditorCommand
    {
        GherkinEditorCommandTargetKey[] Targets { get; }

        GherkinEditorCommandStatus QueryStatus(IWpfTextView textView, Guid commandGroup, uint commandId);
        bool PreExec(IWpfTextView textView, Guid commandGroup, uint commandId, IntPtr inArgs);
        bool PostExec(IWpfTextView textView, Guid commandGroup, uint commandId, IntPtr inArgs);
    }
}