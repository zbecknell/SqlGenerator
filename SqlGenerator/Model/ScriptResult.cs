using System;
using System.Text.RegularExpressions;

namespace SqlGenerator.Model
{
    public class ScriptResult
    {
        public override string ToString()
        {
            if (string.IsNullOrWhiteSpace(InputScript)) return string.Empty;
            if (InputScript.ToUpper().Contains("EXEC")) return InputScript;

            // Check for a new line in the script
            int lastSelect = InputScript.LastIndexOf("SELECT", StringComparison.Ordinal);

            // if there is no new line, return the whole input script
            if (lastSelect == -1) return InputScript;

            var finalSelect = InputScript.Substring(lastSelect);

            Regex trimmer = new Regex(@"\s\s+");

            return trimmer.Replace(finalSelect, " ");
            // else
        }

        public string Server { get; set; }
        public DateTime ScriptTime { get; set; }
        public string InputScript { get; set; }
        public string OutputSql { get; set; }
        public string OutputCSharp { get; set; }
    }
}
