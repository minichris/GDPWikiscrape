using System;
using IronWebScraper;
using System.Linq;
using Newtonsoft.Json;
using System.IO;

namespace Parser
{
    class PatternPageScraper : WebScraper
    {
        public Pattern patternObject = new Pattern();
        String UrlToParse;
        public PatternPageScraper(String InputUrl)
        {
            UrlToParse = InputUrl;
        }
        public override void Init()
        {
            this.LoggingLevel = WebScraper.LogLevel.All;
            this.Request(UrlToParse, Parse);
        }

        public override void Parse(Response response)
        {
            //Get the page title
            patternObject.Title = response.Css("#firstHeading").First().InnerText;

            //get all the links in the content
            foreach(var link in response.Css("#bodyContent").First().Css("a[href]"))
            {
                Pattern.PatternLink linkObject = new Pattern.PatternLink();
                linkObject.From = patternObject.Title;
                linkObject.To = link.InnerText;
                linkObject.RelatingParagraph = link.ParentNode.InnerTextClean;

                if (Program.PatternNames.Contains(linkObject.To)) //is the page we are linking to a pattern?
                {
                    linkObject.Type = Pattern.PatternLink.LinkType.Pattern;
                }
                else if (Program.GameNames.Contains(linkObject.To)) //is the page we are linking to a game?
                {
                    linkObject.Type = Pattern.PatternLink.LinkType.Game;
                }
                else if (Program.GameCategories.Contains(linkObject.To)) //is the page we are linking to a game?
                {
                    linkObject.Type = Pattern.PatternLink.LinkType.GameCategory;
                }

                patternObject.PatternsLinks.Add(linkObject);
            }

            string Json = JsonConvert.SerializeObject(patternObject);
            
            File.WriteAllText( Pattern.GetFileName(patternObject.Title) , Json);

        }
    }
}
