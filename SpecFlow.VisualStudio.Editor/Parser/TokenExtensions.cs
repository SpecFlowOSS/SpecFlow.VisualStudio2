using System.Linq;
using Gherkin;

namespace SpecFlow.VisualStudio.Editor.Parser
{
    public static class TokenExtensions
    {
        public static bool IsGiven(this Token token)
        {
            return token.MatchedGherkinDialect.GivenStepKeywords.Contains(token.MatchedKeyword);
        }

        public static bool IsWhen(this Token token)
        {
            return token.MatchedGherkinDialect.WhenStepKeywords.Contains(token.MatchedKeyword);
        }

        public static bool IsThen(this Token token)
        {
            return token.MatchedGherkinDialect.ThenStepKeywords.Contains(token.MatchedKeyword);
        }
    }
}