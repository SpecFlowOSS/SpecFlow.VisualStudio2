using System;
using Gherkin;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace SpecFlow.VisualStudio.Editor.Parser
{
    public class GherkinTokenTag : ITag
    {
        public Token Token { get; private set; }

        public GherkinTokenTag(Token token)
        {
            this.Token = token;
        }

        public SnapshotSpan GetSpan(ITextSnapshot snapshot)
        {
            var line = snapshot.GetLineFromLineNumber(Token.Location.Line - 1);
            var start = line.Start.Add(Token.Location.Column - 1);
            return new SnapshotSpan(start, line.End);
        }
    }
}