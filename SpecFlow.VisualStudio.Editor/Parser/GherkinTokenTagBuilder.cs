using System.Collections.Generic;
using System.Linq;
using Gherkin;
using Microsoft.VisualStudio.Text;

namespace SpecFlow.VisualStudio.Editor.Parser
{
    internal class GherkinTokenTagBuilder : IAstBuilder
    {
        private readonly ITextSnapshot snapshot;
        private List<GherkinTokenTag> tokenTags = new List<GherkinTokenTag>();
        private List<RuleType> lastRuleTypes = new List<RuleType>();

        public GherkinTokenTagBuilder(ITextSnapshot snapshot)
        {
            this.snapshot = snapshot;
        }

        public void Build(Token token)
        {
            if (token.IsEOF)
                return;

            var stepType = GetStepType(token);
            tokenTags.Add(new GherkinTokenTag(token, lastRuleTypes.ToArray(), snapshot, stepType));

            lastRuleTypes.Clear();
        }

        private StepType GetStepType(Token token)
        {
            if (token.MatchedType == TokenType.StepLine)
            {
                if (token.IsGiven())
                {
                    CurrentStepType = StepType.Given;
                }
                if (token.IsWhen())
                {
                    CurrentStepType = StepType.When;
                }
                if (token.IsThen())
                {
                    CurrentStepType = StepType.Then;
                }
            }
            else
            {
                CurrentStepType = StepType.NotAStep;
            }
            return CurrentStepType;
        }

        private StepType CurrentStepType { get; set; }

        public void StartRule(RuleType ruleType)
        {
            lastRuleTypes.Add(ruleType);
        }

        public void EndRule(RuleType ruleType)
        {
            var lastToenTag = tokenTags.LastOrDefault();
            if (lastToenTag == null)
                return;

            lastToenTag.RuleTypesFinished.Add(ruleType);
        }

        public object GetResult()
        {
            return tokenTags.ToArray();
        }

        public void SetNewState(Token token, int newState)
        {
            if (token.IsEOF)
                return;

            var lastTokenTag = tokenTags.Last();
            if (lastTokenTag.Token != token)
                return;

            lastTokenTag.NewState = newState;
        }
    }
}
