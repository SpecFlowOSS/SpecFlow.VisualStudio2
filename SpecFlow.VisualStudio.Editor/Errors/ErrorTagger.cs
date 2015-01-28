using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using SpecFlow.VisualStudio.Editor.Parser;

namespace SpecFlow.VisualStudio.Editor.Errors
{
    internal class ErrorTagger : GherkinTokenTagConsumer, ITagger<ErrorTag>
    {
        public ErrorTagger(ITextBuffer buffer, ITagAggregator<GherkinTokenTag> gherkinTagAggregator) : base(buffer, gherkinTagAggregator)
        {
        }

        public IEnumerable<ITagSpan<ErrorTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            return GetGherkinTags(spans, t => t.IsError)
                .Select(gherkinTagSpan => new TagSpan<ErrorTag>(gherkinTagSpan.Key, new ErrorTag("syntax error", gherkinTagSpan.Value.ParserException.Message)));
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