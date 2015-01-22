using System;
using System.Collections.Generic;
using System.Linq;
using Gherkin;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;
using SpecFlow.VisualStudio.Editor.Parser;

namespace SpecFlow.VisualStudio.Editor.Classification
{
    internal class GherkinFileClassifier : IClassifier
    {
        private readonly ITextBuffer buffer;
        private readonly ITagAggregator<GherkinTokenTag> gherkinTagAggregator;
        private readonly GherkinFileEditorClassifications gherkinFileEditorClassifications;

        public GherkinFileClassifier(ITextBuffer buffer, ITagAggregator<GherkinTokenTag> gherkinTagAggregator, IClassificationTypeRegistryService classificationTypeRegistryService)
        {
            this.buffer = buffer;
            this.gherkinTagAggregator = gherkinTagAggregator;
            
            gherkinFileEditorClassifications = new GherkinFileEditorClassifications(classificationTypeRegistryService);
        }

        public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
        {
            List<ClassificationSpan> classifications = new List<ClassificationSpan>();

            var gherkinMappingTagSpans = gherkinTagAggregator.GetTags(span);
            foreach (var mappingTagSpan in gherkinMappingTagSpans)
            {
                var tagSpans = mappingTagSpan.Span.GetSpans(span.Snapshot);
                AddClassifications(classifications, mappingTagSpan.Tag, tagSpans[0]);
                //classifications.Add(new ClassificationSpan(tagSpans[0], gherkinFileEditorClassifications.Keyword));
            }

            return classifications;
        }

        private void AddClassifications(List<ClassificationSpan> classifications, GherkinTokenTag tag, SnapshotSpan tagSpan)
        {
            switch (tag.Token.MatchedType)
            {
                case TokenType.FeatureLine:
                case TokenType.ScenarioLine:
                case TokenType.ScenarioOutlineLine:
                case TokenType.BackgroundLine:
                case TokenType.ExamplesLine:
                    classifications.Add(new ClassificationSpan(new SnapshotSpan(tagSpan.Start, tag.Token.MatchedKeyword.Length), gherkinFileEditorClassifications.Keyword));
                    //classifications.Add(new ClassificationSpan(new SnapshotSpan(tagSpan.Start.Add(tag.Token.MatchedKeyword.Length + 1), tagSpan.End), gherkinFileEditorClassifications.UnboundStepText));
                    break;
                case TokenType.StepLine:
                    classifications.Add(new ClassificationSpan(new SnapshotSpan(tagSpan.Start, tag.Token.MatchedKeyword.Length), gherkinFileEditorClassifications.Keyword));
                    classifications.Add(new ClassificationSpan(new SnapshotSpan(tagSpan.Start.Add(tag.Token.MatchedKeyword.Length), tagSpan.End), gherkinFileEditorClassifications.UnboundStepText));
                    break;
                case TokenType.Comment:
                case TokenType.Language:
                    classifications.Add(new ClassificationSpan(new SnapshotSpan(tagSpan.Start, tagSpan.End), gherkinFileEditorClassifications.Comment));
                    break;
                case TokenType.TagLine:
                    break;
                case TokenType.TableRow:
                    foreach (var gherkinLineSpan in tag.Token.MathcedItems)
                    {
                        classifications.Add(new ClassificationSpan(new SnapshotSpan(tagSpan.Start.Add(gherkinLineSpan.Column), gherkinLineSpan.Text.Length), gherkinFileEditorClassifications.TableCell));
                    }
                    break;
                case TokenType.DocStringSeparator:
                    classifications.Add(new ClassificationSpan(new SnapshotSpan(tagSpan.Start, tagSpan.End), gherkinFileEditorClassifications.MultilineText));
                    break;
                case TokenType.Empty:
                    break;
                case TokenType.Other:
                    //TODO: MultilineText
                    classifications.Add(new ClassificationSpan(new SnapshotSpan(tagSpan.Start, tagSpan.End), gherkinFileEditorClassifications.Description));
                    break;
            }
        }

#pragma warning disable 67
        // This event gets raised if a non-text change would affect the classification in some way,
        // for example typing /* would cause the classification to change in C# without directly
        // affecting the span.
        public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged;
#pragma warning restore 67
    }
}
