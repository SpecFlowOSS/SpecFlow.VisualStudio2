using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;

namespace SpecFlow.VisualStudio.Editor.Intellisense
{
    [Export(typeof(ICompletionSourceProvider))]
    [ContentType("gherkin")]
    [Name("gherkinCompletion")]
    class GherkinFileCompletionSourceProvider : ICompletionSourceProvider
    {
        public ICompletionSource TryCreateCompletionSource(ITextBuffer textBuffer)
        {
            return new GherkinFileCompletionSource(textBuffer);
        }
    }

    class GherkinFileCompletionSource : ICompletionSource
    {
        private ITextBuffer _buffer;
        private bool _disposed = false;

        public GherkinFileCompletionSource(ITextBuffer buffer)
        {
            _buffer = buffer;
        }

        public void AugmentCompletionSession(ICompletionSession session, IList<CompletionSet> completionSets)
        {
            if (_disposed)
                throw new ObjectDisposedException("GherkinFileCompletionSource");

            List<Completion> completions = new List<Completion>()
            {
                new Completion("Given "),
                new Completion("When "),
                new Completion("Then ")
            };
            
            ITextSnapshot snapshot = _buffer.CurrentSnapshot;
            var triggerPoint = (SnapshotPoint)session.GetTriggerPoint(snapshot);

            if (triggerPoint == null)
                return;

            var line = triggerPoint.GetContainingLine();
            SnapshotPoint start = triggerPoint;

            while (start > line.Start && !char.IsWhiteSpace((start - 1).GetChar()))
            {
                start -= 1;
            }

            var applicableTo = snapshot.CreateTrackingSpan(new SnapshotSpan(start, triggerPoint), SpanTrackingMode.EdgeInclusive);

            completionSets.Add(new CompletionSet("All", "All", applicableTo, completions, Enumerable.Empty<Completion>()));
        }

        public void Dispose()
        {
            _disposed = true;
        }
    }
}

