using System;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.Text;

namespace SpecFlow.VisualStudio.Editor.Outlining
{
    class OutliningHintTextProvider
    {
        static private readonly Regex IndentRe = new Regex(@"(?<nl>\r?\n)(?<ws>[\t ]+)");
        private readonly Lazy<string> hintText; 

        public OutliningHintTextProvider(SnapshotSpan outliningSpan)
        {
            this.hintText = new Lazy<string>(() =>
            {
                var text = outliningSpan.GetText();
                var indent = outliningSpan.Start - outliningSpan.Start.GetContainingLine().Start;
                if (indent == 0)
                    return text;
                return IndentRe.Replace(text,
                    m => m.Groups["nl"].Value + SafeSubstring(m.Groups["ws"].Value, indent));
            });
        }

        private static string SafeSubstring(string text, int index)
        {
            return text.Length <= index ? String.Empty : text.Substring(index);
        }

        public override string ToString()
        {
            return hintText.Value;
        }
    }
}