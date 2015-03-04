using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Text;
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

        #region Helper methods
        protected void SetSelectionToChangedLines(IWpfTextView textView, ITextSnapshotLine[] lines)
        {
            var newSnapshot = textView.TextBuffer.CurrentSnapshot;
            var selectionStartPosition = newSnapshot.GetLineFromLineNumber(lines.First().LineNumber).Start;
            var selectionEndPosition = newSnapshot.GetLineFromLineNumber(lines.Last().LineNumber).End;
            textView.Selection.Select(new SnapshotSpan(
                selectionStartPosition,
                selectionEndPosition), false);
            textView.Caret.MoveTo(selectionEndPosition);
        }

        protected SnapshotSpan GetSelectionSpan(IWpfTextView textView)
        {
            return new SnapshotSpan(textView.Selection.Start.Position, textView.Selection.End.Position);
        }

        protected IEnumerable<ITextSnapshotLine> GetSpanFullLines(SnapshotSpan span)
        {
            var selectionStartLine = span.Start.GetContainingLine();
            var selectionEndLine = GetSelectionEndLine(selectionStartLine, span);
            for (int lineNumber = selectionStartLine.LineNumber; lineNumber <= selectionEndLine.LineNumber; lineNumber++)
            {
                yield return selectionStartLine.Snapshot.GetLineFromLineNumber(lineNumber);
            }
        }

        private ITextSnapshotLine GetSelectionEndLine(ITextSnapshotLine selectionStartLine, SnapshotSpan span)
        {
            var selectionEndLine = span.End.GetContainingLine();
            // if the selection ends exactly at the beginning of a new line (ie line select), we do not comment out the last line
            if (selectionStartLine.LineNumber != selectionEndLine.LineNumber && selectionEndLine.Start.Equals(span.End))
            {
                selectionEndLine = selectionEndLine.Snapshot.GetLineFromLineNumber(selectionEndLine.LineNumber - 1);
            }
            return selectionEndLine;
        }
        #endregion
    }
}