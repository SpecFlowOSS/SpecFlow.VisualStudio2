using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Text.Editor;

namespace SpecFlow.VisualStudio.Editor.EditorCommands
{
    [Export(typeof(IGherkinEditorCommand))]
    public class UncommentCommand : GherkinEditorCommandBase
    {
        public override GherkinEditorCommandTargetKey[] Targets
        {
            get
            {
                return new[]
                {
                    new GherkinEditorCommandTargetKey(VSConstants.VSStd2K, VSConstants.VSStd2KCmdID.UNCOMMENTBLOCK),
                    new GherkinEditorCommandTargetKey(VSConstants.VSStd2K, VSConstants.VSStd2KCmdID.UNCOMMENT_BLOCK)
                };
            }
        }

        public override bool PreExec(IWpfTextView textView, Guid commandGroup, uint commandId, IntPtr inArgs)
        {
            var selectionSpan = GetSelectionSpan(textView);
            var lines = GetSpanFullLines(selectionSpan).ToArray();
            Debug.Assert(lines.Length > 0);

            using (var textEdit = selectionSpan.Snapshot.TextBuffer.CreateEdit())
            {
                foreach (var line in lines)
                {
                    int commentCharPosition = line.GetText().IndexOf('#');
                    if (commentCharPosition >= 0)
                        textEdit.Delete(line.Start.Position + commentCharPosition, 1);
                }
                textEdit.Apply();
            }

            SetSelectionToChangedLines(textView, lines);

            return false;
        }
    }
}