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
            var tags = GetGherkinTags(new SnapshotSpan(snapshot, 0, snapshot.Length), t => t.IsToken);

            SnapshotPoint? lastNonIgnoredLineEnd = null;

            var outlineStartTags = new Stack<GherkinTokenTag>();

            foreach (var gherkinTagSpan in tags)
            {
                if (!gherkinTagSpan.Value.IsAnyTokenType(TokenType.Empty, TokenType.Comment))
                    lastNonIgnoredLineEnd = gherkinTagSpan.Value.Span.Start.GetContainingLine().End;

                var levelToClose = OutlineGroups.FindIndex(0, outlineStartTags.Count,
                    outlineGroup => gherkinTagSpan.Value.FinishesAnyRule(outlineGroup));
                if (levelToClose >= 0)
                {
                    Debug.Assert(lastNonIgnoredLineEnd != null);

                    while (outlineStartTags.Count > levelToClose)
                    {
                        var startTag = outlineStartTags.Pop();
                        yield return CreateOutliningRegionTag(startTag.Span.Start, lastNonIgnoredLineEnd.Value, startTag);
                    }
                }

                foreach (var outlineGroup in OutlineGroups.Skip(outlineStartTags.Count))
                {
                    if (gherkinTagSpan.Value.StartsAnyRule(outlineGroup))
                        outlineStartTags.Push(gherkinTagSpan.Value);
                }
            }
        }

        private static TagSpan<IOutliningRegionTag> CreateOutliningRegionTag(SnapshotPoint startPoint, SnapshotPoint endPoint, GherkinTokenTag tag)
        {
            var collapsedText = tag.Token.GetTokenValue();
            return new TagSpan<IOutliningRegionTag>(new SnapshotSpan(startPoint, endPoint), new OutliningRegionTag(false, false, collapsedText, "TODO"));
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