using System;
using System.Collections.Generic;
using System.IO;
using Gherkin;
using Gherkin.Ast;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace SpecFlow.VisualStudio.Editor.Parser
{
    internal sealed class GherkinTokenTagger : ITagger<GherkinTokenTag>
    {

        ITextBuffer _buffer;

        internal GherkinTokenTagger(ITextBuffer buffer)
        {
            _buffer = buffer;
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged
        {
            add { }
            remove { }
        }

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

            //var feature = parser.Parse(reader) as Feature;


/*
            foreach (SnapshotSpan curSpan in spans)
            {
                ITextSnapshotLine containingLine = curSpan.Start.GetContainingLine();
                int curLoc = containingLine.Start.Position;
                string[] tokens = containingLine.GetText().ToLower().Split(' ');

                foreach (string ookToken in tokens)
                {
                    if (_ookTypes.ContainsKey(ookToken))
                    {
                        var tokenSpan = new SnapshotSpan(curSpan.Snapshot, new Span(curLoc, ookToken.Length));
                        if (tokenSpan.IntersectsWith(curSpan))
                            yield return new TagSpan<GherkinTokenTag>(tokenSpan,
                                new GherkinTokenTag(_ookTypes[ookToken]));
                    }

                    //add an extra char location because of the space
                    curLoc += ookToken.Length + 1;
                }
            }
*/
        }
    }
}