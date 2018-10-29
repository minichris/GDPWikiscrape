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
        public List<String> Categories = new List<String>();

        public static String GetFileName(string Title)
        {
            string regexSearch = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            Regex r = new Regex(string.Format("[{0}]", Regex.Escape(regexSearch)));
            return "patterns/" + r.Replace(Title + ".json", "");
        }

        public class PatternLink
        {
            [JsonConverter(typeof(StringEnumConverter))]
            public enum LinkType { Pattern, Game, GameCategory, PatternCategory, Unknown };
            public String To;
            public String RelatingParagraph;
            public virtual LinkType getLinkType()
            {
                if (Program.PatternNames.Contains(this.To)) //is the page we are linking to a pattern?
                {
                    return Pattern.PatternLink.LinkType.Pattern;
                }
                else if (Program.GameNames.Contains(this.To)) //is the page we are linking to a game?
                {
                    return Pattern.PatternLink.LinkType.Game;
                }
                else if (Program.GameCategories.Contains(this.To)) //is the page we are linking to a game?
                {
                    return Pattern.PatternLink.LinkType.GameCategory;
                }
                else
                {
                    return Pattern.PatternLink.LinkType.Unknown;
                }
            }
        }
    }
}
