using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Text.Editor;
using SpecFlow.VisualStudio.Editor.EditorCommands.Infrastructure;

namespace SpecFlow.VisualStudio.Editor.EditorCommands
{
    [Export(typeof(IGherkinEditorCommand))]
    public class FormatDocumentCommand : GherkinEditorCommandBase
    {
        public override GherkinEditorCommandTargetKey[] Targets
        {
            get { return new []
            {
                new GherkinEditorCommandTargetKey(VSConstants.VSStd2K, VSConstants.VSStd2KCmdID.FORMATDOCUMENT),
            }; }
        }

        public override bool PreExec(IWpfTextView textView, Guid commandGroup, uint commandId, IntPtr inArgs)
        {
            //TODO add formatting logic
            return base.PreExec(textView, commandGroup, commandId, inArgs);
        }
    }
}
