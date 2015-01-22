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

            this.gherkinTagAggregator.BatchedTagsChanged += (sender, args) =>
            {
                //TODO: raise event only for the span received in args
                if (ClassificationChanged != null)
                    ClassificationChanged(sender, new ClassificationChangedEventArgs(new SnapshotSpan(buffer.CurrentSnapshot, 0, buffer.CurrentSnapshot.Length)));
            };
        }

        public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
        {
            List<ClassificationSpan> classifications = new List<ClassificationSpan>();

            var gherkinMappingTagSpans = gherkinTagAggregator.GetTags(span);
            foreach (var mappingTagSpan in gherkinMappingTagSpans)
            {
                var tagSpans = mappingTagSpan.Span.GetSpans(span.Snapshot);
                AddClassifications(classifications, mappingTagSpan.Tag, tagSpans[0]);
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
                    AddItemClassifications(classifications, tag, tagSpan, gherkinFileEditorClassifications.Tag);
                    break;
                case TokenType.TableRow:
                    AddItemClassifications(classifications, tag, tagSpan, gherkinFileEditorClassifications.TableCell);
                    break;
                case TokenType.DocStringSeparator:
                    classifications.Add(new ClassificationSpan(new SnapshotSpan(tagSpan.Start, tagSpan.End), gherkinFileEditorClassifications.MultilineText));
                    break;
                case TokenType.Other:
                    var classificationType = 
                        tag.RuleType == RuleType.DocString ? 
                        gherkinFileEditorClassifications.MultilineText :
                        gherkinFileEditorClassifications.Description;
                    classifications.Add(new ClassificationSpan(new SnapshotSpan(tagSpan.Start, tagSpan.End), classificationType));
                    break;
                case TokenType.Empty:
                    break;
            }
        }

        private static void AddItemClassifications(List<ClassificationSpan> classifications, GherkinTokenTag tag, SnapshotSpan tagSpan,
            IClassificationType classificationType)
        {
            foreach (var gherkinLineSpan in tag.Token.MathcedItems)
            {
                classifications.Add(
                    new ClassificationSpan(
                        new SnapshotSpan(tagSpan.Start.Add(gherkinLineSpan.Column - tag.Token.Location.Column), gherkinLineSpan.Text.Length), classificationType));
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
