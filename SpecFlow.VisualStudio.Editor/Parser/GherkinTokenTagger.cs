using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Gherkin;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace SpecFlow.VisualStudio.Editor.Parser
{
    internal sealed class GherkinTokenTagger : ITagger<GherkinTokenTag>
    {
        private class ParsingResult
        {
            public int SnapshotVersion { get; private set; }
            public GherkinTokenTag[] Tags { get; private set; }

            public ParsingResult(int snapshotVersion, GherkinTokenTag[] tags)
            {
                SnapshotVersion = snapshotVersion;
                Tags = tags;
            }
        }

        private readonly ITextBuffer buffer;

        private ParsingResult lastResult = null;

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
            var currentLastResult = lastResult;
            if (currentLastResult != null && currentLastResult.SnapshotVersion == snapshot.Version.VersionNumber)
                return currentLastResult.Tags;
                
            var fileContent = snapshot.GetText();

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var parserErrors = new List<ParserException>();

            var parser = new GherkinEditorParser();

            var reader = new StringReader(fileContent);
            var tokenTagBuilder = new GherkinTokenTagBuilder(snapshot);
            try
            {
                parser.Parse(new TokenScanner(reader), new TokenMatcher(VsGherkinDialectProvider.Instance));
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

            stopwatch.Stop();
            Debug.WriteLine("Gherkin3: parsed v{0} on thread {1} in {2} ms", snapshot.Version.VersionNumber, Thread.CurrentThread.ManagedThreadId, stopwatch.ElapsedMilliseconds);

            var result = new ParsingResult(snapshot.Version.VersionNumber, tokenTags.ToArray());
            Thread.MemoryBarrier();
            lastResult = result;

            //TODO: only for the changed scope
            if (TagsChanged != null)
                TagsChanged(this, new SnapshotSpanEventArgs(new SnapshotSpan(snapshot, 0, snapshot.Length)));

            return result.Tags;
        }
    }
}