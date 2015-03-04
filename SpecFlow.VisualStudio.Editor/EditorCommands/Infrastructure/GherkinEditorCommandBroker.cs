using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;

namespace SpecFlow.VisualStudio.Editor.EditorCommands.Infrastructure
{
    [Export(typeof(IVsTextViewCreationListener))]
    [ContentType("gherkin")]
    [TextViewRole(PredefinedTextViewRoles.Interactive)]
    public class GherkinEditorCommandBroker : IVsTextViewCreationListener
    {
        #region Command Filter
        private class EditorCommandsFilter : IOleCommandTarget
        {
            public EditorCommandsFilter(IWpfTextView textView, Dictionary<GherkinEditorCommandTargetKey, IGherkinEditorCommand[]> commandRegistry)
            {
                TextView = textView;
                CommandRegistry = commandRegistry;
            }

            public IWpfTextView TextView { get; private set; }
            public Dictionary<GherkinEditorCommandTargetKey, IGherkinEditorCommand[]> CommandRegistry { get; set; }
            public IOleCommandTarget Next { get; set; }

            public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
            {
                var commandKey = new GherkinEditorCommandTargetKey(pguidCmdGroup, prgCmds[0].cmdID);
                IGherkinEditorCommand[] commands;
                if (CommandRegistry.TryGetValue(commandKey, out commands))
                {
                    foreach (var editorCommand in commands)
                    {
                        var status = editorCommand.QueryStatus(TextView, pguidCmdGroup, prgCmds[0].cmdID);
                        if (status != GherkinEditorCommandStatus.NotSupported)
                        {
                            prgCmds[0].cmdf = (uint)OLECMDF.OLECMDF_SUPPORTED;
                            if (status == GherkinEditorCommandStatus.Supported)
                                prgCmds[0].cmdf |= (uint) OLECMDF.OLECMDF_ENABLED;
                            return VSConstants.S_OK;
                        }
                    }

                }

                return Next.QueryStatus(pguidCmdGroup, cCmds, prgCmds, pCmdText);
            }

            public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
            {
                bool handled = false;
                int hresult = VSConstants.S_OK;

                var commandKey = new GherkinEditorCommandTargetKey(pguidCmdGroup, nCmdID);
                IGherkinEditorCommand[] commands;
                if (!CommandRegistry.TryGetValue(commandKey, out commands))
                {
                    return Next.Exec(pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
                }

                // Pre-process
                foreach (var editorCommand in commands)
                {
                    handled = editorCommand.PreExec(TextView, pguidCmdGroup, nCmdID, pvaIn);
                    if (handled)
                        break;
                }

                if (!handled)
                    hresult = Next.Exec(pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);

                // Post-process
                foreach (var editorCommand in commands)
                {
                    editorCommand.PostExec(TextView, pguidCmdGroup, nCmdID, pvaIn);
                }

                return hresult;
            }
        }
        #endregion

        [Import]
        IVsEditorAdaptersFactoryService AdaptersFactory = null;

        [ImportMany(typeof(IGherkinEditorCommand))]
        List<IGherkinEditorCommand> Commands = null;

        private readonly Lazy<Dictionary<GherkinEditorCommandTargetKey, IGherkinEditorCommand[]>> editorCommandRegistry;

        public GherkinEditorCommandBroker()
        {
            editorCommandRegistry = new Lazy<Dictionary<GherkinEditorCommandTargetKey, IGherkinEditorCommand[]>>(BuildRegistry, LazyThreadSafetyMode.ExecutionAndPublication);
        }

        private Dictionary<GherkinEditorCommandTargetKey, IGherkinEditorCommand[]> BuildRegistry()
        {
            var list = new List<KeyValuePair<GherkinEditorCommandTargetKey, IGherkinEditorCommand>>();
            foreach (var editorCommand in Commands)
            {
                list.AddRange(editorCommand.Targets.Select(target => new KeyValuePair<GherkinEditorCommandTargetKey, IGherkinEditorCommand>(new GherkinEditorCommandTargetKey(target.CommandGroup, target.CommandId), editorCommand)));
            }
            return list.GroupBy(item => item.Key).ToDictionary(g => g.Key, g => g.Select(item => item.Value).ToArray());
        }

        public void VsTextViewCreated(IVsTextView textViewAdapter)
        {
            IWpfTextView view = AdaptersFactory.GetWpfTextView(textViewAdapter);
            Debug.Assert(view != null);

            var filter = new EditorCommandsFilter(view, editorCommandRegistry.Value);

            IOleCommandTarget next;
            textViewAdapter.AddCommandFilter(filter, out next);
            filter.Next = next;
        }
    }
}
