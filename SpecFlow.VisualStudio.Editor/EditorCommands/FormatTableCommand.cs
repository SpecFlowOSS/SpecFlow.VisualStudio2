using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gherkin;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using SpecFlow.VisualStudio.Editor.Parser;

namespace SpecFlow.VisualStudio.Editor.EditorCommands
{
    [Export(typeof(IGherkinEditorCommand))]
    public class FormatTableCommand : GherkinEditorTypeCharCommandBase
    {
        [Import]
        internal IBufferTagAggregatorFactoryService AggregatorFactory = null;

        protected override bool PostExec(IWpfTextView textView, char ch)
        {
            if (ch != '|')
                return false;

            ITagAggregator<GherkinTokenTag> gherkinTagAggregator = AggregatorFactory.CreateTagAggregator<GherkinTokenTag>(textView.TextBuffer);
            var caretBufferPosition = textView.Caret.Position.BufferPosition;
            var line = caretBufferPosition.GetContainingLine();
            int caretCellPosition = GetCaretCellPosition(line, caretBufferPosition);

            var tableSpan = GetTableSpan(line, gherkinTagAggregator);
            if (tableSpan == null)
                return false;

            var tableTokenTags = gherkinTagAggregator.GetTags(tableSpan.Value).Where(t => t.Tag.IsToken).Select(t => t.Tag).ToArray();
            var formattedTableText = GetFormattedTableText(tableTokenTags);

            var textEdit = textView.TextBuffer.CreateEdit();
            textEdit.Replace(tableSpan.Value, formattedTableText);
            textEdit.Apply();

            var restoredCaretPosition = GetCaretPositionFromCellPosition(textView.TextBuffer.CurrentSnapshot.GetLineFromLineNumber(line.LineNumber), caretCellPosition);
            textView.Caret.MoveTo(restoredCaretPosition);
            return true;
        }

        private int GetCaretCellPosition(ITextSnapshotLine line, SnapshotPoint caretBufferPosition)
        {
            var linePrefixText = new SnapshotSpan(line.Start, caretBufferPosition).GetText();
            return linePrefixText.Count(c => c == '|');
        }

        private SnapshotPoint GetCaretPositionFromCellPosition(ITextSnapshotLine line, int cellPosition)
        {
            var lineText = line.GetText();
            var linePosition = 0;
            while (cellPosition > 0 && linePosition >= 0)
            {
                linePosition = lineText.IndexOf('|', linePosition) + 1;
                cellPosition--;
            }

            if (linePosition < 0)
                return line.End;

            return line.Start + linePosition;
        }

        private string GetFormattedTableText(GherkinTokenTag[] tableTokenTags)
        {
            string indentText;
            var cellWidths = CalculateCellWiths(tableTokenTags.Where(t => t.IsAnyTokenType(TokenType.TableRow)).Select(t => t.Token), out indentText);
            var stringBuilder = new StringBuilder();
            foreach (var tag in tableTokenTags)
            {
                if (tag.IsAnyTokenType(TokenType.TableRow))
                {
                    stringBuilder.Append(indentText);
                    for (int i = 0; i < tag.Token.MathcedItems.Length; i++)
                    {
                        stringBuilder.Append("| ");
                        var cellText = tag.Token.MathcedItems[i].Text;
                        stringBuilder.Append(cellText);
                        if (i < cellWidths.Length)
                        {
                            stringBuilder.Append(' ', cellWidths[i] - cellText.Length);
                        }
                        stringBuilder.Append(' ');
                    }
                    stringBuilder.AppendLine("|");
                }
                else
                {
                    stringBuilder.AppendLine(tag.Token.Line.GetLineText(0));
                }
            }
            var replaceWith = stringBuilder.ToString();
            return replaceWith;
        }

        private int[] CalculateCellWiths(IEnumerable<Token> tokens, out string indentText)
        {
            int[] result = null;
            indentText = "";
            foreach (var token in tokens)
            {
                if (result == null)
                {
                    result = new int[token.MathcedItems.Length];
                    indentText = token.Line.GetLineText(0).Substring(0, token.MatchedIndent);
                }

                for (int i = 0; i < Math.Min(result.Length, token.MathcedItems.Length); i++)
                {
                    result[i] = Math.Max(result[i], token.MathcedItems[i].Text.Length);
                }
            }

            return result;
        }

        private const int SCAN_LINE_RADIUS = 10;

        private SnapshotSpan? GetTableSpan(ITextSnapshotLine line, ITagAggregator<GherkinTokenTag> gherkinTagAggregator)
        {
            if (!gherkinTagAggregator.GetTags(line.Extent).Any(t => t.Tag.IsToken && t.Tag.IsAnyTokenType(TokenType.TableRow)))
                return null;
            
            var snapshot = line.Snapshot;
            int scanLineFrom = Math.Max(0, line.LineNumber - SCAN_LINE_RADIUS);
            int scanLineTo = Math.Min(snapshot.LineCount - 1, line.LineNumber + SCAN_LINE_RADIUS);

            return GetTableSpan(line, gherkinTagAggregator, snapshot, scanLineFrom, scanLineTo);
        }

        private SnapshotSpan? GetTableSpan(ITextSnapshotLine line, ITagAggregator<GherkinTokenTag> gherkinTagAggregator, ITextSnapshot snapshot, int scanLineFrom, int scanLineTo)
        {
            var scanSpan = new SnapshotSpan(snapshot.GetLineFromLineNumber(scanLineFrom).Start, snapshot.GetLineFromLineNumber(scanLineTo).End);
            var gherkinMappingTagSpans = gherkinTagAggregator.GetTags(scanSpan).Where(t => t.Tag.IsToken).ToArray();

            var tagSpansBefore = gherkinMappingTagSpans.Where(t => t.Tag.Token.Location.Line - 1 < line.LineNumber).Reverse();
            var tableStartTagSpan =
                tagSpansBefore.TakeWhile(t => t.Tag.IsAnyTokenType(TokenType.TableRow, TokenType.Empty, TokenType.Comment))
                    .LastOrDefault();
            var tagSpansAfter = gherkinMappingTagSpans.Where(t => t.Tag.Token.Location.Line - 1 > line.LineNumber);
            var tableEndTagSpan =
                tagSpansAfter.TakeWhile(t => t.Tag.IsAnyTokenType(TokenType.TableRow, TokenType.Empty, TokenType.Comment))
                    .LastOrDefault();

            var startLine = line;
            var endLine = line;
            if (tableStartTagSpan != null)
            {
                var startLineNumber = tableStartTagSpan.Tag.Token.Location.Line - 1;
                if (startLineNumber > 0 && startLineNumber <= scanLineFrom)
                    return GetTableSpan(line, gherkinTagAggregator, snapshot, Math.Max(0, scanLineFrom - SCAN_LINE_RADIUS), scanLineTo);
                startLine = snapshot.GetLineFromLineNumber(startLineNumber);
            }
            if (tableEndTagSpan != null)
            {
                var endLineNumber = tableEndTagSpan.Tag.Token.Location.Line - 1;
                if (endLineNumber < snapshot.LineCount - 1 && endLineNumber >= scanLineTo)
                    return GetTableSpan(line, gherkinTagAggregator, snapshot, scanLineFrom, Math.Min(snapshot.LineCount - 1, scanLineTo + SCAN_LINE_RADIUS));
                endLine = snapshot.GetLineFromLineNumber(endLineNumber);
            }

            return new SnapshotSpan(startLine.Start, endLine.EndIncludingLineBreak);
        }
    }
}
