using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;
using SpecFlow.VisualStudio.Editor.Parser;

namespace SpecFlow.VisualStudio.Editor.Errors
{
    internal class ErrorTagger : ITagger<ErrorTag>
    {
        private readonly ITextBuffer buffer;
        private readonly ITagAggregator<GherkinTokenTag> gherkinTagAggregator;

        public ErrorTagger(ITextBuffer buffer, ITagAggregator<GherkinTokenTag> gherkinTagAggregator)
        {
            this.buffer = buffer;
            this.gherkinTagAggregator = gherkinTagAggregator;

            this.gherkinTagAggregator.BatchedTagsChanged += (sender, args) =>
            {
                //TODO: raise event only for the span received in args
                if (TagsChanged != null)
                    TagsChanged(sender, new SnapshotSpanEventArgs(new SnapshotSpan(buffer.CurrentSnapshot, 0, buffer.CurrentSnapshot.Length)));
            };
        }

        public IEnumerable<ITagSpan<ErrorTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            var snapshot = spans[0].Snapshot;
            var gherkinMappingTagSpans = gherkinTagAggregator.GetTags(spans).Where(t => t.Tag.IsError);
            foreach (var mappingTagSpan in gherkinMappingTagSpans)
            {
                var tagSpans = mappingTagSpan.Span.GetSpans(snapshot);
                yield return new TagSpan<ErrorTag>(tagSpans[0], new ErrorTag("syntax error"));
            }
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;
    }
}