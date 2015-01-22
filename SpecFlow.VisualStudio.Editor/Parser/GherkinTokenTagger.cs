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

        private GherkinTokenTag[] lastParsedTokenTags;
        private int lastParsedBufferVersion = -1;

        internal GherkinTokenTagger(ITextBuffer buffer)
        {
            this.buffer = buffer;
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        public IEnumerable<ITagSpan<GherkinTokenTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            var snapshot = spans[0].Snapshot;
            var tokenTags = Parse(snapshot);

            if (tokenTags == null)
                yield break;

            foreach (SnapshotSpan queriedSpan in spans)
            {
                foreach (var tokenTag in tokenTags)
                {
                    var tokenSpan = tokenTag.Span;
                    if (tokenSpan.IntersectsWith(queriedSpan))
                        yield return new TagSpan<GherkinTokenTag>(tokenSpan, tokenTag);

                    if (tokenSpan.Start > queriedSpan.End)
                        break;
                }
            }
        }

        private GherkinTokenTag[] Parse(ITextSnapshot snapshot)
        {
            if (snapshot.Version.VersionNumber == lastParsedBufferVersion)
                return lastParsedTokenTags;

            var fileContent = snapshot.GetText();

            Gherkin.Parser parser = new Gherkin.Parser();

            var reader = new StringReader(fileContent);
            var tokenTagBuilder = new GherkinTokenTagBuilder(snapshot);
            try
            {
                parser.Parse(new TokenScanner(reader), new TokenMatcher(), tokenTagBuilder);
            }
            catch (Exception ex)
            {
                //nop;
            }

            var tokenTags = tokenTagBuilder.GetResult() as GherkinTokenTag[];

            //TODO: atomic set?
            lastParsedBufferVersion = snapshot.Version.VersionNumber;
            lastParsedTokenTags = tokenTags;

            //TODO: only for the changed scope
            if (TagsChanged != null)
                TagsChanged(this, new SnapshotSpanEventArgs(new SnapshotSpan(snapshot, 0, snapshot.Length)));

            return tokenTags;
        }
    }
}