using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace SpecFlow.VisualStudio.Editor
{
    internal static class EditorClassifierClassificationDefinition
    {
        /// <summary>
        /// Defines the "EditorClassifier" classification type.
        /// </summary>
        [Export(typeof(ClassificationTypeDefinition))]
        [Name("EditorClassifier")]
        internal static ClassificationTypeDefinition EditorClassifierType = null;
    }
}
