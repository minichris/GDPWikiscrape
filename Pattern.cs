using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Text;

namespace Parser
{
    class Pattern
    {
        public String Title;
        public Dictionary<String, String> Relations = new Dictionary<string, string>();
        public List<PatternLink> PatternsLinks = new List<PatternLink>();

        public class PatternLink
        {
            [JsonConverter(typeof(StringEnumConverter))]
            public enum LinkType { Pattern, Game, GameCategory, Unknown };
            public LinkType Type = LinkType.Unknown;
            public String From, To;
            public String RelatingParagraph;
        }
    }
}
