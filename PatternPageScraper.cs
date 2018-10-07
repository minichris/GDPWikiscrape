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
            this.LoggingLevel = WebScraper.LogLevel.Critical;
            this.Request(UrlToParse, Parse);
        }

        public override void Parse(Response response)
        {
            //Get the page title
            patternObject.Title = response.Css("#firstHeading").First().InnerText;

            //get all the links in the content
            foreach(var link in response.Css("#bodyContent").First().Css("a[href]"))
            {
                if (link.Attributes["href"].Contains("redlink=1")) continue; //skip if this is a redlink (page doesn't exist).

                Pattern.PatternLink linkObject = new Pattern.PatternLink();
                linkObject.From = patternObject.Title;
                linkObject.To = link.InnerText;
                linkObject.RelatingParagraph = link.ParentNode.InnerTextClean;

                patternObject.PatternsLinks.Add(linkObject);
            }

            string Json = JsonConvert.SerializeObject(patternObject);
            
            File.WriteAllText( Pattern.GetFileName(patternObject.Title) , Json);

        }
    }
}
