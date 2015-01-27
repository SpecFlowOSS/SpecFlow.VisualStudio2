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
        private readonly GherkinDialect defaultGherkinDialect;

        private bool _disposed = false;

        public GherkinFileCompletionSource(ITextBuffer buffer, ITagAggregator<GherkinTokenTag> gherkinTagAggregator)
        {
            _buffer = buffer;
            this.gherkinTagAggregator = gherkinTagAggregator;
            this.defaultGherkinDialect = new GherkinDialectProvider().DefaultDialect; //TODO: get default dialect from config
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
            var stateAndDialect = GetLineStartStateAndDialect(line);
            var expectedTokens = GherkinEditorParser.GetExpectedTokens(stateAndDialect.Item1);
            AddCompletionsFromExpectedTokens(expectedTokens, completions, stateAndDialect.Item2);

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

        private void AddCompletionsFromExpectedTokens(TokenType[] expectedTokens, List<Completion> completions, GherkinDialect dialect)
        {
            foreach (var expectedToken in expectedTokens)
            {
                switch (expectedToken)
                {
                    case TokenType.FeatureLine:
                        AddCompletions(completions, dialect.FeatureKeywords, ": ");
                        break;
                    case TokenType.BackgroundLine:
                        AddCompletions(completions, dialect.BackgroundKeywords, ": ");
                        break;
                    case TokenType.ScenarioLine:
                        AddCompletions(completions, dialect.ScenarioKeywords, ": ");
                        break;
                    case TokenType.ScenarioOutlineLine:
                        AddCompletions(completions, dialect.ScenarioOutlineKeywords, ": ");
                        break;
                    case TokenType.ExamplesLine:
                        AddCompletions(completions, dialect.ExamplesKeywords, ": ");
                        break;
                    case TokenType.StepLine:
                        AddCompletions(completions, dialect.StepKeywords);
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
        }

        private void AddCompletions(List<Completion> completions, string[] keywords, string postfix = "")
        {
            completions.AddRange(keywords.Select(keyword => new Completion(keyword + postfix)));
        }

        private Tuple<int, GherkinDialect> GetLineStartStateAndDialect(ITextSnapshotLine line)
        {
            var state = new Tuple<int, GherkinDialect>(0, defaultGherkinDialect);
            if (line.LineNumber > 0)
            {
                var prevLine = line.Snapshot.GetLineFromLineNumber(line.LineNumber - 1);
                var gherkinMappingTagSpans = gherkinTagAggregator.GetTags(prevLine.Extent);
                var tagSpan = gherkinMappingTagSpans.LastOrDefault();
                if (tagSpan != null)
                {
                    state = new Tuple<int, GherkinDialect>(tagSpan.Tag.NewState, tagSpan.Tag.Token.MatchedGherkinDialect);
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

