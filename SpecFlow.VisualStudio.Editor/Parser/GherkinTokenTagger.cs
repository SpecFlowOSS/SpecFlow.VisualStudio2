using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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

            var parserErrors = new List<ParserException>();

            var parser = new GherkinEditorParser();

            var reader = new StringReader(fileContent);
            var tokenTagBuilder = new GherkinTokenTagBuilder(snapshot);
            try
            {
                parser.Parse(new TokenScanner(reader), new TokenMatcher(VsGherkinDialectProvider.Instance), tokenTagBuilder);
            }
            catch (CompositeParserException compositeParserException)
            {
                parserErrors.AddRange(compositeParserException.Errors);
            }
            catch (ParserException parserException)
            {
                parserErrors.Add(parserException);
            }
            catch (Exception ex)
            {
                //nop;
                Debug.WriteLine(ex);
            }


            var tokenTags = new List<GherkinTokenTag>();
            tokenTags.AddRange((GherkinTokenTag[])tokenTagBuilder.GetResult());
            tokenTags.AddRange(parserErrors.Select(e => new GherkinTokenTag(e, snapshot)));
            tokenTags.Sort((t1, t2) => t1.Span.Start.Position.CompareTo(t2.Span.Start.Position));

            //TODO: atomic set?
            lastParsedBufferVersion = snapshot.Version.VersionNumber;
            lastParsedTokenTags = tokenTags.ToArray();

            //TODO: only for the changed scope
            if (TagsChanged != null)
                TagsChanged(this, new SnapshotSpanEventArgs(new SnapshotSpan(snapshot, 0, snapshot.Length)));

            return lastParsedTokenTags;
        }
    }
}