using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gherkin;
using Microsoft.VisualStudio.Text;

namespace SpecFlow.VisualStudio.Editor.Parser
{
    internal class GherkinTokenTagBuilder : IAstBuilder
    {
        private readonly ITextSnapshot snapshot;
        private List<GherkinTokenTag> tokenTags = new List<GherkinTokenTag>();
        private RuleType lastRuleType = RuleType.None;

        public GherkinTokenTagBuilder(ITextSnapshot snapshot)
        {
            this.snapshot = snapshot;
        }

        public void Build(Token token)
        {
            if (token.IsEOF)
                return;

            tokenTags.Add(new GherkinTokenTag(token, lastRuleType, snapshot));
        }

        public void StartRule(RuleType ruleType)
        {
            lastRuleType = ruleType;
        }

        public void EndRule(RuleType ruleType)
        {
            //nop
        }

        public object GetResult()
        {
            return tokenTags.ToArray();
        }
    }
}
