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

            Stack<SnapshotPoint> outlineStarts = new Stack<SnapshotPoint>();

            foreach (var gherkinTagSpan in tags)
            {
                if (!gherkinTagSpan.Value.IsAnyTokenType(TokenType.Empty, TokenType.Comment))
                    lastNonIgnoredLineEnd = gherkinTagSpan.Value.Span.Start.GetContainingLine().End;

                var levelToClose = OutlineGroups.FindIndex(0, outlineStarts.Count,
                    outlineGroup => gherkinTagSpan.Value.FinishesAnyRule(outlineGroup));
                if (levelToClose >= 0)
                {
                    Debug.Assert(lastNonIgnoredLineEnd != null);

                    while (outlineStarts.Count > levelToClose)
                    {
                        var startPoint = outlineStarts.Pop();
                        yield return CreateOutliningRegionTag(startPoint, lastNonIgnoredLineEnd.Value);
                    }
                }

                foreach (var outlineGroup in OutlineGroups.Skip(outlineStarts.Count))
                {
                    if (gherkinTagSpan.Value.StartsAnyRule(outlineGroup))
                        outlineStarts.Push(gherkinTagSpan.Value.Span.Start);
                }
            }
        }

        private static TagSpan<IOutliningRegionTag> CreateOutliningRegionTag(SnapshotPoint startPoint, SnapshotPoint endPoint)
        {
            return new TagSpan<IOutliningRegionTag>(new SnapshotSpan(startPoint, endPoint), new OutliningRegionTag());
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