using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Gherkin;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;
using SpecFlow.VisualStudio.Editor.Parser;

namespace SpecFlow.VisualStudio.Editor.Classification
{
    internal class GherkinFileClassifier : GherkinTokenTagConsumer, IClassifier
    {
        private readonly GherkinFileEditorClassifications gherkinFileEditorClassifications;

        public GherkinFileClassifier(ITextBuffer buffer, ITagAggregator<GherkinTokenTag> gherkinTagAggregator, IClassificationTypeRegistryService classificationTypeRegistryService)
            : base(buffer, gherkinTagAggregator)
        {

            gherkinFileEditorClassifications = new GherkinFileEditorClassifications(classificationTypeRegistryService);
        }

        public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
        {
            List<ClassificationSpan> classifications = new List<ClassificationSpan>();

            foreach (var gherkinTagSpan in GetGherkinTags(span, t => t.IsToken))
            {
                AddClassifications(classifications, gherkinTagSpan.Value, gherkinTagSpan.Key);
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
                    var classification = GetClassification(tag.StepType);
                    classifications.Add(new ClassificationSpan(new SnapshotSpan(tagSpan.Start, tag.Token.MatchedKeyword.Length), classification));
                    //classifications.Add(new ClassificationSpan(new SnapshotSpan(tagSpan.Start.Add(tag.Token.MatchedKeyword.Length), tagSpan.End), gherkinFileEditorClassifications.StepText));
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
                        tag.RuleTypesStarted.Contains(RuleType.DocString) ? 
                        gherkinFileEditorClassifications.MultilineText :
                        gherkinFileEditorClassifications.Description;
                    classifications.Add(new ClassificationSpan(new SnapshotSpan(tagSpan.Start, tagSpan.End), classificationType));
                    break;
                case TokenType.Empty:
                    break;
            }
        }

        private IClassificationType GetClassification(StepType stepType)
        {
            switch (stepType)
            {
                 case StepType.Given:
                    return gherkinFileEditorClassifications.KeywordGiven;
                 case StepType.When:
                    return gherkinFileEditorClassifications.KeywordWhen;
                 case StepType.Then:
                    return gherkinFileEditorClassifications.KeywordThen;
                 case StepType.NotAStep:
                    return gherkinFileEditorClassifications.Keyword;
                default:
                    throw new InvalidEnumArgumentException("stepType", (int)stepType, typeof (StepType));
            }
        }

        private static void AddItemClassifications(List<ClassificationSpan> classifications, GherkinTokenTag tag, SnapshotSpan tagSpan,
            IClassificationType classificationType)
        {
            foreach (var gherkinLineSpan in tag.Token.MatchedItems)
            {
                classifications.Add(
                    new ClassificationSpan(
                        new SnapshotSpan(tagSpan.Start.Add(gherkinLineSpan.Column - tag.Token.Location.Column), gherkinLineSpan.Text.Length), classificationType));
            }
        }

        // This event gets raised if a non-text change would affect the classification in some way,
        // for example typing /* would cause the classification to change in C# without directly
        // affecting the span.
        public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged;
        protected override void RaiseChanged(SnapshotSpan span)
        {
            var classificationChanged = ClassificationChanged;
            if (classificationChanged != null)
                classificationChanged(this, new ClassificationChangedEventArgs(span));
        }
    }
}
