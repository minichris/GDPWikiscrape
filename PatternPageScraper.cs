using System;
using IronWebScraper;
using System.Linq;
using Newtonsoft.Json;
using System.IO;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

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

        public static string ProcessPageContentToString(HtmlAgilityPack.HtmlNode ContentNode)
        {
            //remove the "toc" section to save space and later client-side processing time
            ContentNode.SelectSingleNode("//*[@id=\"toc\"]").Remove();
            //remove all the tabs and newlines
            String output = Regex.Replace(ContentNode.InnerHtml, @"\t|\n|\r", "");
            return output;
        }

        public override void Parse(Response response)
        {
            //Create a new HTMLAglityPack document
            HtmlDocument ContentDocument = new HtmlDocument();
            //load the #content of the page into the document
            ContentDocument.LoadHtml(response.Css("#content").First().OuterHtml);
            HtmlAgilityPack.HtmlNode ContentNode = ContentDocument.DocumentNode;

            //set the patternObject's title
            patternObject.Title = ContentNode.SelectSingleNode("//*[@id=\"firstHeading\"]").InnerHtml;
            //get a cleaned copy of the #content HTML for giving in the JSON data
            patternObject.Content = ProcessPageContentToString(ContentNode);
            

            foreach (var link in ContentNode.SelectNodes("//a/@href"))
            {
                //skip if this is a redlink (page doesn't exist).
                if (link.Attributes["href"].Value.Contains("redlink=1")) continue;
                //skip if this links to this page
                if (link.Attributes["href"].Value.Split('#').First() == response.FinalUrl) continue;

                //generate a new patternlink with the inner text of this link, then add it to this pattern objects list of links
                patternObject.PatternsLinks.Add( new Pattern.PatternLink(link.InnerText) );

                //if any of the links ancestor nodes is the "category links" part of the page
                if(link.Ancestors().Any(node => node.Id == "catlinks"))
                {
                    //add it to the patterns list of categories
                    patternObject.Categories.Add(link.InnerText);
                }
            }

            string Json = JsonConvert.SerializeObject(patternObject);
            
            File.WriteAllText( Pattern.GetFileName(patternObject.Title) , Json);

        }
    }
}
