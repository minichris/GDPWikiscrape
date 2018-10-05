using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Parser
{
    class Pattern
    {
        public String Title;
        public Dictionary<String, String> Relations = new Dictionary<string, string>();
        public List<PatternLink> PatternsLinks = new List<PatternLink>();

        public static String GetFileName(string Title)
        {
            string regexSearch = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            Regex r = new Regex(string.Format("[{0}]", Regex.Escape(regexSearch)));
            return "files/" + r.Replace(Title + ".json", "");
        }

        public class PatternLink
        {
            [JsonConverter(typeof(StringEnumConverter))]
            public enum LinkType { Pattern, Game, GameCategory, Unknown };
            public LinkType Type = LinkType.Unknown;
            public String From, To;
            public String RelatingParagraph;
        }

        internal static string GetFileName(object filename)
        {
            throw new NotImplementedException();
        }
    }
}
