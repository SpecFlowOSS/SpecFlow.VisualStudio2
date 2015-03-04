using System;
using System.Collections.Generic;
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
            return GetOrCreate(buffer, () => new GherkinTokenTagger(buffer) as ITagger<T>);
        }

        public TService GetOrCreate<TService>(ITextBuffer textBuffer, Func<TService> factory) where TService : class
        {
            return textBuffer.Properties.GetOrCreateSingletonProperty(typeof(TService), factory);
        }
    }
}