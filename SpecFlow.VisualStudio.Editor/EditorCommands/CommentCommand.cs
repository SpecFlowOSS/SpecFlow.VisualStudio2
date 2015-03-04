using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Text.Editor;
using SpecFlow.VisualStudio.Editor.EditorCommands.Infrastructure;

namespace SpecFlow.VisualStudio.Editor.EditorCommands
{
    [Export(typeof(IGherkinEditorCommand))]
    public class CommentCommand : GherkinEditorCommandBase
    {
        public override GherkinEditorCommandTargetKey[] Targets
        {
            get
            {
                return new[]
                {
                    new GherkinEditorCommandTargetKey(VSConstants.VSStd2K, VSConstants.VSStd2KCmdID.COMMENTBLOCK),
                    new GherkinEditorCommandTargetKey(VSConstants.VSStd2K, VSConstants.VSStd2KCmdID.COMMENT_BLOCK)
                };
            }
        }

        public override bool PreExec(IWpfTextView textView, Guid commandGroup, uint commandId, IntPtr inArgs)
        {
            var selectionSpan = GetSelectionSpan(textView);
            var lines = GetSpanFullLines(selectionSpan).ToArray();
            Debug.Assert(lines.Length > 0);

            int indent = lines.Min(l => l.GetText().TakeWhile(char.IsWhiteSpace).Count());

            using (var textEdit = selectionSpan.Snapshot.TextBuffer.CreateEdit())
            {
                foreach (var line in lines)
                {
                    textEdit.Insert(line.Start.Position + indent, "#");
                }
                textEdit.Apply();
            }

            SetSelectionToChangedLines(textView, lines);

            return false;
        }
    }
}
