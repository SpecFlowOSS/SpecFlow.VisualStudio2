using System;
using System.Collections.Generic;
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

        public IEnumerable<ITagSpan<IOutliningRegionTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            var snapshot = spans[0].Snapshot;
            var tags = GetGherkinTags(new SnapshotSpan(snapshot, 0, snapshot.Length), t => t.IsToken);

            SnapshotPoint? startPoint = null;
            SnapshotPoint? endPoint = null;
            foreach (var gherkinTagSpan in tags)
            {
                if (gherkinTagSpan.Value.StartsAnyRule(RuleType.Scenario, RuleType.ScenarioOutline, RuleType.Background))
                {
                    if (startPoint != null && endPoint != null)
                        yield return CreateOutliningRegionTag(startPoint.Value, endPoint.Value);

                    startPoint = gherkinTagSpan.Value.Span.Start.GetContainingLine().Start;
                }

                if (!gherkinTagSpan.Value.IsAnyTokenType(TokenType.Empty, TokenType.TagLine, TokenType.Comment))
                    endPoint = gherkinTagSpan.Value.Span.Start.GetContainingLine().End;
            }

            if (startPoint != null && endPoint != null)
                yield return CreateOutliningRegionTag(startPoint.Value, endPoint.Value);
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