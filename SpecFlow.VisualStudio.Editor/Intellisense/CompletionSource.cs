using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Gherkin;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using SpecFlow.VisualStudio.Editor.Parser;

namespace SpecFlow.VisualStudio.Editor.Intellisense
{
    [Export(typeof(ICompletionSourceProvider))]
    [ContentType("gherkin")]
    [Name("gherkinCompletion")]
    class GherkinFileCompletionSourceProvider : ICompletionSourceProvider
    {
        [Import]
        internal IBufferTagAggregatorFactoryService AggregatorFactory = null;

        public ICompletionSource TryCreateCompletionSource(ITextBuffer textBuffer)
        {
            var gherkinTagAggregator = AggregatorFactory.CreateTagAggregator<GherkinTokenTag>(textBuffer);
            return new GherkinFileCompletionSource(textBuffer, gherkinTagAggregator);
        }
    }

    class GherkinFileCompletionSource : ICompletionSource
    {
        private ITextBuffer _buffer;
        private readonly ITagAggregator<GherkinTokenTag> gherkinTagAggregator;
        private bool _disposed = false;

        public GherkinFileCompletionSource(ITextBuffer buffer, ITagAggregator<GherkinTokenTag> gherkinTagAggregator)
        {
            _buffer = buffer;
            this.gherkinTagAggregator = gherkinTagAggregator;
        }

        public void AugmentCompletionSession(ICompletionSession session, IList<CompletionSet> completionSets)
        {
            if (_disposed)
                throw new ObjectDisposedException("GherkinFileCompletionSource");

            ITextSnapshot snapshot = _buffer.CurrentSnapshot;
            var triggerPoint = (SnapshotPoint)session.GetTriggerPoint(snapshot);

            if (triggerPoint == null)
                return;

            var line = triggerPoint.GetContainingLine();

            List<Completion> completions = new List<Completion>();
            var state = GetLineStartState(line);
            var expectedTokens = GherkinEditorParser.GetExpectedTokens(state);
            foreach (var expectedToken in expectedTokens)
            {
                switch (expectedToken)
                {
                    case TokenType.FeatureLine:
                        completions.Add(new Completion("Feature: "));
                        break;
                    case TokenType.BackgroundLine:
                        completions.Add(new Completion("Background: "));
                        break;
                    case TokenType.ScenarioLine:
                        completions.Add(new Completion("Scenario: "));
                        break;
                    case TokenType.ScenarioOutlineLine:
                        completions.Add(new Completion("Scenario Outline: "));
                        break;
                    case TokenType.ExamplesLine:
                        completions.Add(new Completion("Examples: "));
                        break;
                    case TokenType.StepLine:
                        completions.Add(new Completion("Given "));
                        completions.Add(new Completion("When "));
                        completions.Add(new Completion("Then "));
                        completions.Add(new Completion("And "));
                        completions.Add(new Completion("But "));
                        completions.Add(new Completion("* "));
                        break;
                    case TokenType.DocStringSeparator:
                        completions.Add(new Completion("\"\"\""));
                        completions.Add(new Completion("'''"));
                        break;
                    case TokenType.TableRow:
                        completions.Add(new Completion("| "));
                        break;
                    case TokenType.Language:
                        completions.Add(new Completion("#language: "));
                        break;
                    case TokenType.TagLine:
                        completions.Add(new Completion("@mytag "));
                        break;
                }
            }

            if (completions.Count == 0)
                return;

            SnapshotPoint start = triggerPoint;

            while (start > line.Start && !char.IsWhiteSpace((start - 1).GetChar()))
            {
                start -= 1;
            }

            var applicableTo = snapshot.CreateTrackingSpan(new SnapshotSpan(start, triggerPoint), SpanTrackingMode.EdgeInclusive);

            completionSets.Add(new CompletionSet("All", "All", applicableTo, completions, Enumerable.Empty<Completion>()));
        }

        private int GetLineStartState(ITextSnapshotLine line)
        {
            var state = 0;
            if (line.LineNumber > 0)
            {
                var prevLine = line.Snapshot.GetLineFromLineNumber(line.LineNumber - 1);
                var gherkinMappingTagSpans = gherkinTagAggregator.GetTags(prevLine.Extent);
                var tagSpan = gherkinMappingTagSpans.LastOrDefault();
                if (tagSpan != null)
                {
                    state = tagSpan.Tag.NewState;
                }
            }
            return state;
        }

        public void Dispose()
        {
            _disposed = true;
        }
    }
}

