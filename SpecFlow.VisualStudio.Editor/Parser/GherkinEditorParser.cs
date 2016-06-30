using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Gherkin;
using Gherkin.Ast;

namespace SpecFlow.VisualStudio.Editor.Parser
{
    internal class GherkinEditorParser : Gherkin.Parser
    {
        private readonly GherkinTokenTagBuilder _astBuilder;

        public GherkinEditorParser(IAstBuilder<GherkinDocument> astBuilder) : base(astBuilder)
        {
            _astBuilder = astBuilder as GherkinTokenTagBuilder;
        }

        protected override int MatchToken(int state, Token token, ParserContext context)
        {
            var newState = base.MatchToken(state, token, context);
            if (token.MatchedType != TokenType.None)
            {
                _astBuilder?.SetNewState(token, newState);
                //((GherkinTokenTagBuilder) context.Builder).SetNewState(token, newState);
            }
            return newState;
        }

        class AllFalseTokenMatcher : ITokenMatcher
        {
            public bool Match_EOF(Token token)
            {
                return false;
            }

            public bool Match_Empty(Token token)
            {
                return false;
            }

            public bool Match_Comment(Token token)
            {
                return false;
            }

            public bool Match_TagLine(Token token)
            {
                return false;
            }

            public bool Match_FeatureLine(Token token)
            {
                return false;
            }

            public bool Match_BackgroundLine(Token token)
            {
                return false;
            }

            public bool Match_ScenarioLine(Token token)
            {
                return false;
            }

            public bool Match_ScenarioOutlineLine(Token token)
            {
                return false;
            }

            public bool Match_ExamplesLine(Token token)
            {
                return false;
            }

            public bool Match_StepLine(Token token)
            {
                return false;
            }

            public bool Match_DocStringSeparator(Token token)
            {
                return false;
            }

            public bool Match_TableRow(Token token)
            {
                return false;
            }

            public bool Match_Language(Token token)
            {
                return false;
            }

            public bool Match_Other(Token token)
            {
                return false;
            }

            public void Reset()
            {
                
            }
        }
        class NullTokenScanner : ITokenScanner
        {
            public Token Read()
            {
                return new Token(null, new Location());
            }
        }
        class NullAstBuilder : IAstBuilder<object>
        {
            public void Build(Token token)
            {
            }

            public void StartRule(RuleType ruleType)
            {
            }

            public void EndRule(RuleType ruleType)
            {
            }

            public object GetResult()
            {
                return null;
            }

            public void Reset()
            {
                
            }
        }

        public static TokenType[] GetExpectedTokens(int state)
        {
            var parser = new GherkinEditorParser(new GherkinTokenTagBuilder(null))
            {
                StopAtFirstError = true
            };

            try
            {
                parser.MatchToken(state, new Token(null, new Location()), new ParserContext()
                {
                    //Builder = new NullAstBuilder(),
                    Errors = new List<ParserException>(),
                    TokenMatcher = new AllFalseTokenMatcher(),
                    TokenQueue = new Queue<Token>(),
                    TokenScanner = new NullTokenScanner()
                });
            }
            catch (UnexpectedEOFException ex)
            {
                return ex.ExpectedTokenTypes.Select(type => (TokenType)Enum.Parse(typeof(TokenType), type.TrimStart('#'))).ToArray();
            }

            return new TokenType[0];
        }
    }
}
