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
        public String Content;
        [JsonProperty]
        private List<PatternLink> PatternsLinks = new List<PatternLink>();
        public List<String> Categories = new List<String>();

        public PatternLink CreateOrGetPatternLink(String Destination)
        {
            //check if the link already exists
            if (this.PatternsLinks.Find(pLink => pLink.To == Destination) == null)
            {
                //generate a new patternlink with the inner text of this link, then add it to this pattern objects list of links
                this.PatternsLinks.Add(new Pattern.PatternLink(Destination));
            }
            return this.PatternsLinks.Find(pLink => pLink.To == Destination); //return it
        }

        public static String GetFileName(string Title)
        {
            string regexSearch = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            Regex r = new Regex(string.Format("[{0}]", Regex.Escape(regexSearch)));
            return "patterns/" + r.Replace(Title + ".json", "");
        }

        public class PatternLink
        {
            public List<String> AssociatedRelations = new List<String>();
            public String To;
            public PatternLink(String ToString)
            {
                this.To = ToString;
            }

            //Enum Stuff
            [JsonConverter(typeof(StringEnumConverter))]
            public enum LinkType { Pattern, Game, GameCategory, PatternCategory, Unknown };
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
