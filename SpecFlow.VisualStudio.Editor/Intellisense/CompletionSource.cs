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
            this.defaultGherkinDialect = VsGherkinDialectProvider.Instance.DefaultDialect; //TODO: get default dialect from config
        }

        public void AugmentCompletionSession(ICompletionSession session, IList<CompletionSet> completionSets)
        {
            if (_disposed)
                throw new ObjectDisposedException("GherkinFileCompletionSource");

            ITextSnapshot snapshot = _buffer.CurrentSnapshot;
            var triggerPoint = session.GetTriggerPoint(snapshot);

            if (triggerPoint == null)
                return;

            AugmentCompletionSession(completionSets, triggerPoint.Value, snapshot);
        }

        private void AugmentCompletionSession(IList<CompletionSet> completionSets, SnapshotPoint triggerPoint, ITextSnapshot snapshot)
        {
            var line = triggerPoint.GetContainingLine();

            List<Completion> completions = new List<Completion>();
            var stateAndDialect = GetLineStartStateAndDialect(line);
            var expectedTokens = GherkinEditorParser.GetExpectedTokens(stateAndDialect.Item1);
            AddCompletionsFromExpectedTokens(expectedTokens, completions, stateAndDialect.Item2);

            if (completions.Count == 0)
                return;

            var applicableToSpan = CalculateApplicableToSpan(triggerPoint, line);
            var applicableToText = applicableToSpan.GetText();
            if (applicableToText.Length > 0 && completions.Any(c => applicableToText.StartsWith(c.InsertionText)))
                return;

            var applicableTo = snapshot.CreateTrackingSpan(applicableToSpan, SpanTrackingMode.EdgeInclusive);
            completionSets.Add(new CompletionSet("Gherkin", "Gherkin", applicableTo, completions, Enumerable.Empty<Completion>()));
        }

        private static SnapshotSpan CalculateApplicableToSpan(SnapshotPoint triggerPoint, ITextSnapshotLine line)
        {
            SnapshotPoint start = line.Start;

            while (start < triggerPoint && char.IsWhiteSpace(start.GetChar()))
            {
                start += 1;
            }

            var applicableToSpan = new SnapshotSpan(start, triggerPoint);
            return applicableToSpan;
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
            while (line.LineNumber > 0)
            {
                line = line.Snapshot.GetLineFromLineNumber(line.LineNumber - 1);
                var gherkinMappingTagSpans = gherkinTagAggregator.GetTags(line.Extent).Where(t => t.Tag.IsToken);
                var tagSpan = gherkinMappingTagSpans.LastOrDefault();
                if (tagSpan != null)
                {
                    return new Tuple<int, GherkinDialect>(tagSpan.Tag.NewState, tagSpan.Tag.Token.MatchedGherkinDialect);
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

