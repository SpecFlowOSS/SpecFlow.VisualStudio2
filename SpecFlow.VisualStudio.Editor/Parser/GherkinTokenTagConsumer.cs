using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace SpecFlow.VisualStudio.Editor.Parser
{
    internal abstract class GherkinTokenTagConsumer : IDisposable
    {
        protected readonly ITextBuffer buffer;
        protected readonly ITagAggregator<GherkinTokenTag> gherkinTagAggregator;

        protected GherkinTokenTagConsumer(ITextBuffer buffer, ITagAggregator<GherkinTokenTag> gherkinTagAggregator)
        {
            this.buffer = buffer;
            this.gherkinTagAggregator = gherkinTagAggregator;

            this.gherkinTagAggregator.BatchedTagsChanged += GherkinTagAggregatorOnBatchedTagsChanged;
        }

        private void GherkinTagAggregatorOnBatchedTagsChanged(object sender, BatchedTagsChangedEventArgs batchedTagsChangedEventArgs)
        {
            var span = new SnapshotSpan(buffer.CurrentSnapshot, 0, buffer.CurrentSnapshot.Length); //TODO: raise event only for the span received in args
            RaiseChanged(span);
        }

        protected abstract void RaiseChanged(SnapshotSpan span);

        public void Dispose()
        {
            this.gherkinTagAggregator.BatchedTagsChanged -= GherkinTagAggregatorOnBatchedTagsChanged;
        }

        protected IEnumerable<KeyValuePair<SnapshotSpan, GherkinTokenTag>> GetGherkinTags(SnapshotSpan span, Predicate<GherkinTokenTag> filter = null)
        {
            return GetGherkinTags(new NormalizedSnapshotSpanCollection(span), filter);
        }

        protected IEnumerable<KeyValuePair<SnapshotSpan, GherkinTokenTag>> GetGherkinTags(NormalizedSnapshotSpanCollection spans, Predicate<GherkinTokenTag> filter = null)
        {
            var snapshot = spans[0].Snapshot;
            var gherkinMappingTagSpans = gherkinTagAggregator.GetTags(spans);
            if (filter != null)
                gherkinMappingTagSpans = gherkinMappingTagSpans.Where(t => filter(t.Tag));

            return gherkinMappingTagSpans.SelectMany(
                mappingTagSpan => mappingTagSpan.Span.GetSpans(snapshot),
                (mappingTagSpan, mappedTagSpan) => new KeyValuePair<SnapshotSpan, GherkinTokenTag>(mappedTagSpan, mappingTagSpan.Tag));
        }
    }
}
