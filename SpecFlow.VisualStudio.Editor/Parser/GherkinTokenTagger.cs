using System;
using System.Collections.Generic;
using System.IO;
using Gherkin;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace SpecFlow.VisualStudio.Editor.Parser
{
    internal sealed class GherkinTokenTagger : ITagger<GherkinTokenTag>
    {
        private readonly ITextBuffer buffer;

        internal GherkinTokenTagger(ITextBuffer buffer)
        {
            this.buffer = buffer;
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        public IEnumerable<ITagSpan<GherkinTokenTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            var snapshot = spans[0].Snapshot;
            var fileContent = snapshot.GetText();

            Gherkin.Parser parser = new Gherkin.Parser();

            var reader = new StringReader(fileContent);
            var tokenTagBuilder = new GherkinTokenTagBuilder();
            try
            {
                parser.Parse(new TokenScanner(reader), new TokenMatcher(), tokenTagBuilder);
            }
            catch (Exception ex)
            {
                //nop;
            }

            var tokenTags = tokenTagBuilder.GetResult() as GherkinTokenTag[];

            if (tokenTags == null)
                yield break;

            foreach (SnapshotSpan curSpan in spans)
            {
                foreach (var tokenTag in tokenTags)
                {
                    SnapshotSpan tokenSpan = tokenTag.GetSpan(snapshot);
                    if (tokenSpan.IntersectsWith(curSpan))
                        yield return new TagSpan<GherkinTokenTag>(tokenSpan, tokenTag);
                }
            }
        }
    }
}