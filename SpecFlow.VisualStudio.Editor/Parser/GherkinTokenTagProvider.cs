using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace SpecFlow.VisualStudio.Editor.Parser
{
    [Export(typeof(ITaggerProvider))]
    [ContentType("gherkin")]
    [TagType(typeof(GherkinTokenTag))]
    internal sealed class GherkinTokenTagProvider : ITaggerProvider
    {

        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            return new GherkinTokenTagger(buffer) as ITagger<T>;
        }
    }
}