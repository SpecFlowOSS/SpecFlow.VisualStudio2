using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gherkin;

namespace SpecFlow.VisualStudio.Editor.Parser
{
    internal class GherkinTokenTagBuilder : IAstBuilder
    {
        private List<GherkinTokenTag> tokenTags = new List<GherkinTokenTag>();

        public void Build(Token token)
        {
            if (token.IsEOF)
                return;

            tokenTags.Add(new GherkinTokenTag(token));
        }

        public void StartRule(RuleType ruleType)
        {
            //nop
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
