using System;
using System.Linq;
using Gherkin;
using Gherkin.Ast;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace SpecFlow.VisualStudio.Editor.Parser
{
    public class GherkinTokenTag : ITag
    {
        public ParserException ParserException { get; private set; }
        public Token Token { get; private set; }
        public RuleType[] RuleTypes { get; private set; }
        public SnapshotSpan Span { get; private set; }
        public int NewState { get; set; }
        public bool IsToken { get { return Token != null; } }
        public bool IsError { get { return ParserException != null; } }

        public GherkinTokenTag(Token token, RuleType[] ruleTypes, ITextSnapshot snapshot)
        {
            this.Token = token;
            RuleTypes = ruleTypes;
            Span = GetSpan(snapshot, token.Location);
        }

        public GherkinTokenTag(ParserException parserException, ITextSnapshot snapshot)
        {
            ParserException = parserException;
            Span = GetSpan(snapshot, parserException.Location);
        }

        private SnapshotSpan GetSpan(ITextSnapshot snapshot, Location location)
        {
            var line = snapshot.GetLineFromLineNumber(
                location.Line == 0 ? 0 // global error
                : location.Line - 1 >= snapshot.LineCount ? snapshot.LineCount - 1 // unexpected end of file
                : location.Line - 1);
            var start = line.Start.Add(
                location.Column == 0 ? 0 // whole line error
                : location.Column - 1);
            return new SnapshotSpan(start, line.End);
        }

        public bool StartsAnyRule(params RuleType[] ruleTypes)
        {
            if (ruleTypes == null || ruleTypes.Length == 0) throw new ArgumentNullException("ruleTypes");
            return ruleTypes.Any(ruleType => RuleTypes.Contains(ruleType));
        }

        public bool IsAnyTokenType(params TokenType[] tokenTypes)
        {
            if (tokenTypes == null || tokenTypes.Length == 0) throw new ArgumentNullException("tokenTypes");

            return tokenTypes.Contains(Token.MatchedType);
        }
    }
}