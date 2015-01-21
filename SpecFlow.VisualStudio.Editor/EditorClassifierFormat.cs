using System.ComponentModel.Composition;
using System.Windows.Media;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace SpecFlow.VisualStudio.Editor
{
    #region Format definition
    /// <summary>
    /// Defines an editor format for the EditorClassifier type that has a purple background
    /// and is underlined.
    /// </summary>
    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = "EditorClassifier")]
    [Name("EditorClassifier")]
    [UserVisible(true)] //this should be visible to the end user
    [Order(Before = Priority.Default)] //set the priority to be after the default classifiers
    internal sealed class EditorClassifierFormat : ClassificationFormatDefinition
    {
        /// <summary>
        /// Defines the visual format for the "EditorClassifier" classification type
        /// </summary>
        public EditorClassifierFormat()
        {
            this.DisplayName = "EditorClassifier"; //human readable version of the name
            this.BackgroundColor = Colors.BlueViolet;
            this.TextDecorations = System.Windows.TextDecorations.Underline;
        }
    }
    #endregion //Format definition
}
