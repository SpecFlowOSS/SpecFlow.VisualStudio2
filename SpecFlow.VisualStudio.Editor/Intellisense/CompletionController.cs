using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;

namespace SpecFlow.VisualStudio.Editor.Intellisense
{
    #region Command Filter

    [Export(typeof(IVsTextViewCreationListener))]
    [ContentType("gherkin")]
    [TextViewRole(PredefinedTextViewRoles.Interactive)]
    internal sealed class VsTextViewCreationListener : IVsTextViewCreationListener
    {
        [Import]
        IVsEditorAdaptersFactoryService AdaptersFactory = null;

        [Import]
        ICompletionBroker CompletionBroker = null;

        public void VsTextViewCreated(IVsTextView textViewAdapter)
        {
            IWpfTextView view = AdaptersFactory.GetWpfTextView(textViewAdapter);
            Debug.Assert(view != null);

            CommandFilter filter = new CommandFilter(view, CompletionBroker);

            IOleCommandTarget next;
            textViewAdapter.AddCommandFilter(filter, out next);
            filter.Next = next;
        }
    }

    internal sealed class CommandFilter : IOleCommandTarget
    {
        ICompletionSession _currentSession;

        public CommandFilter(IWpfTextView textView, ICompletionBroker broker)
        {
            _currentSession = null;

            TextView = textView;
            Broker = broker;
        }

        public IWpfTextView TextView { get; private set; }
        public ICompletionBroker Broker { get; private set; }
        public IOleCommandTarget Next { get; set; }

        private char GetTypeChar(IntPtr pvaIn)
        {
            return (char)(ushort)Marshal.GetObjectForNativeVariant(pvaIn);
        }

        public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            bool handled = false;
            int hresult = VSConstants.S_OK;

            // 1. Pre-process
            if (pguidCmdGroup == VSConstants.VSStd2K)
            {
                switch ((VSConstants.VSStd2KCmdID)nCmdID)
                {
                    case VSConstants.VSStd2KCmdID.AUTOCOMPLETE:
                    case VSConstants.VSStd2KCmdID.COMPLETEWORD:
                        handled = TriggerCompletion();
                        break;
                    case VSConstants.VSStd2KCmdID.RETURN:
                        handled = Complete(false);
                        break;
                    case VSConstants.VSStd2KCmdID.TAB:
                        handled = Complete(true);
                        break;
                    case VSConstants.VSStd2KCmdID.CANCEL:
                        handled = Cancel();
                        break;
                }
            }

            if (!handled)
                hresult = Next.Exec(pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);

            if (ErrorHandler.Succeeded(hresult))
            {
                if (pguidCmdGroup == VSConstants.VSStd2K)
                {
                    switch ((VSConstants.VSStd2KCmdID)nCmdID)
                    {
                        case VSConstants.VSStd2KCmdID.TYPECHAR:
                            char ch = GetTypeChar(pvaIn);
                            if (_currentSession == null && ShouldStartSessionOnTyping(ch))
                                TriggerCompletion();
                            else if (_currentSession != null)
                                Filter();
                            break;
                        case VSConstants.VSStd2KCmdID.BACKSPACE:
                            Filter();
                            break;
                    }
                }
            }

            return hresult;
        }

        private bool ShouldStartSessionOnTyping(char ch)
        {
            if (char.IsWhiteSpace(ch))
                return false;

            if (ch == '|' || ch == '#' || ch == '*') //TODO: get this from parser?
                return false;

            var caretBufferPosition = TextView.Caret.Position.BufferPosition;

            var line = caretBufferPosition.GetContainingLine();
            if (caretBufferPosition == line.Start)
                return false; // we are at the beginning of a line (after an enter?)

            var linePrefixText = new SnapshotSpan(line.Start, caretBufferPosition.Subtract(1)).GetText();
            return linePrefixText.All(char.IsWhiteSpace); // start auto completion for the first typed in character in the line 
        }

        private void Filter()
        {
            if (_currentSession == null)
                return;

            var completionSet = _currentSession.SelectedCompletionSet;

            completionSet.SelectBestMatch();
            completionSet.Recalculate();

            if (completionSet.SelectionStatus.IsSelected &&
                completionSet.SelectionStatus.IsUnique &&
                completionSet.ApplicableTo.GetSpan(TextView.TextBuffer.CurrentSnapshot).GetText().Equals(completionSet.SelectionStatus.Completion.InsertionText, StringComparison.CurrentCultureIgnoreCase))
            {
                _currentSession.Commit();
                _currentSession = null;
            }

        }

        bool Cancel()
        {
            if (_currentSession == null)
                return false;

            _currentSession.Dismiss();

            return true;
        }

        bool Complete(bool force)
        {
            if (_currentSession == null)
                return false;

            if (!_currentSession.SelectedCompletionSet.SelectionStatus.IsSelected && !force)
            {
                _currentSession.Dismiss();
                return false;
            }
            else
            {
                _currentSession.Commit();
                return true;
            }
        }

        bool TriggerCompletion()
        {
            if (_currentSession == null)
            {
                if (!Broker.IsCompletionActive(TextView))
                {
                    _currentSession = Broker.TriggerCompletion(TextView);
                    if (_currentSession != null)
                    {
                        _currentSession.Dismissed += CurrentSessionOnDismissed;
                        _currentSession.Committed += CurrentSessionOnDismissed;
                    }
                }
                else
                {
                    _currentSession = Broker.GetSessions(TextView)[0];
                }
            }

            if (_currentSession != null && _currentSession.SelectedCompletionSet != null)
            {
                var completionSet = _currentSession.SelectedCompletionSet;
                if (completionSet.SelectionStatus.IsSelected &&
                    completionSet.SelectionStatus.IsUnique &&
                    completionSet.ApplicableTo.GetSpan(TextView.TextBuffer.CurrentSnapshot).Length >= 3)
                {
                    _currentSession.Commit();
                    _currentSession = null;
                }
            }

            if (_currentSession != null)
            {
                //NOTE: call _currentSession.Filter() to narrow the list to the applicable items only
            }

            return true;
        }

        private void CurrentSessionOnDismissed(object sender, EventArgs eventArgs)
        {
            _currentSession.Dismissed -= CurrentSessionOnDismissed;
            _currentSession.Committed -= CurrentSessionOnDismissed;
            _currentSession = null;
        }

        public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            if (pguidCmdGroup == VSConstants.VSStd2K)
            {
                switch ((VSConstants.VSStd2KCmdID)prgCmds[0].cmdID)
                {
                    case VSConstants.VSStd2KCmdID.AUTOCOMPLETE:
                    case VSConstants.VSStd2KCmdID.COMPLETEWORD:
                        prgCmds[0].cmdf = (uint)OLECMDF.OLECMDF_ENABLED | (uint)OLECMDF.OLECMDF_SUPPORTED;
                        return VSConstants.S_OK;
                }
            }
            return Next.QueryStatus(pguidCmdGroup, cCmds, prgCmds, pCmdText);
        }
    }

    #endregion
}