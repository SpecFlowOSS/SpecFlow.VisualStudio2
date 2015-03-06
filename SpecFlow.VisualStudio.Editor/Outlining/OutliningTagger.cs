using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Gherkin;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using SpecFlow.VisualStudio.Editor.Parser;

namespace SpecFlow.VisualStudio.Editor.Outlining
{
    internal class OutliningTagger : GherkinTokenTagConsumer, ITagger<IOutliningRegionTag>
    {
        public OutliningTagger(ITextBuffer buffer, ITagAggregator<GherkinTokenTag> gherkinTagAggregator) : base(buffer, gherkinTagAggregator)
        {
        }

        static private readonly List<RuleType[]> OutlineGroups = new List<RuleType[]>
            {
                new []{ RuleType.Scenario, RuleType.ScenarioOutline, RuleType.Background }, // level-0
                new []{ RuleType.Examples, RuleType.DataTable, RuleType.DocString } // level-1
            };

        public IEnumerable<ITagSpan<IOutliningRegionTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            var snapshot = spans[0].Snapshot;
            // we recalculate the outlinings for the entire file...
            var tags = GetGherkinTags(new SnapshotSpan(snapshot, 0, snapshot.Length), t => t.IsToken);

            SnapshotPoint? lastNonIgnoredLineEnd = null;

            var outlineStartTags = new Stack<GherkinTokenTag>();

            foreach (var gherkinTagSpan in tags)
            {
                if (!gherkinTagSpan.Value.IsAnyTokenType(TokenType.Empty, TokenType.Comment))
                    lastNonIgnoredLineEnd = gherkinTagSpan.Value.Span.End;

                // finds the most outer level that has to be closed
                var levelToClose = OutlineGroups.FindIndex(0, outlineStartTags.Count,
                    outlineGroup => gherkinTagSpan.Value.FinishesAnyRule(outlineGroup));
                if (levelToClose >= 0)
                {
                    Debug.Assert(lastNonIgnoredLineEnd != null);

                    // closes all outlining levels up to the expected one
                    while (outlineStartTags.Count > levelToClose)
                    {
                        var startTag = outlineStartTags.Pop();
                        yield return CreateOutliningRegionTag(startTag.Span.Start, lastNonIgnoredLineEnd.Value, startTag);
                    }
                }

                // checks if new outlinings should be open
                foreach (var outlineGroup in OutlineGroups.Skip(outlineStartTags.Count))
                {
                    if (gherkinTagSpan.Value.StartsAnyRule(outlineGroup))
                        outlineStartTags.Push(gherkinTagSpan.Value);
                }
            }
        }

        private static TagSpan<IOutliningRegionTag> CreateOutliningRegionTag(SnapshotPoint startPoint, SnapshotPoint endPoint, GherkinTokenTag tag)
        {
            var outliningSpan = new SnapshotSpan(startPoint, endPoint);
            var collapseSpan = new SnapshotSpan(outliningSpan.Start + tag.Token.GetTokenValue().TrimEnd().Length, outliningSpan.End);
            var hintText = new OutliningHintTextProvider(outliningSpan);
            return new TagSpan<IOutliningRegionTag>(collapseSpan, new OutliningRegionTag(false, false, "...", hintText));
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;
        protected override void RaiseChanged(SnapshotSpan span)
        {
            var tagsChanged = TagsChanged;
            if (tagsChanged != null)
                tagsChanged(this, new SnapshotSpanEventArgs(span));
        }
    }
}