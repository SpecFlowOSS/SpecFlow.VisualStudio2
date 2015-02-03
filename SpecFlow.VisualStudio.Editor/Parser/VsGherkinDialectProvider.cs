using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gherkin;

namespace SpecFlow.VisualStudio.Editor.Parser
{
    class VsGherkinDialectProvider : GherkinDialectProvider
    {
        static internal readonly IGherkinDialectProvider Instance = new VsGherkinDialectProvider();

        protected override Dictionary<string, GherkinLanguageSetting> LoadLanguageSettings()
        {
            string languagesFile = Path.GetFullPath("i18n.json");
            string languagesFileContent;
            if (File.Exists(languagesFile))
                languagesFileContent = File.ReadAllText(languagesFile);
            else
            {
                var resourceStream = typeof(VsGherkinDialectProvider).Assembly.GetManifestResourceStream("SpecFlow.VisualStudio.Editor.i18n.json");
                if (resourceStream == null)
                    throw new InvalidOperationException("Gherkin language settings file not found: " + languagesFile);
                languagesFileContent = new StreamReader(resourceStream).ReadToEnd();
            }

            return ParseJsonContent(languagesFileContent);
        }
    }
}
