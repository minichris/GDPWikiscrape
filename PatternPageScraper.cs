using System;
using IronWebScraper;
using System.Linq;
using Newtonsoft.Json;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml.Linq;
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

        public static string ProcessPageContent(IronWebScraper.HtmlNode ContentNode)
        {
            //Load the node into HtmlAgilityPack document
            HtmlDocument ContentObject = new HtmlDocument();
            ContentObject.LoadHtml(ContentNode.OuterHtml);
            //remove the "toc" section to save space and later client-side processing time
            ContentObject.DocumentNode.SelectSingleNode("//*[@id=\"toc\"]").Remove();
            //remove all the tabs and newlines
            String output = Regex.Replace(ContentObject.DocumentNode.InnerHtml, @"\t|\n|\r", "");
            return output;
        }

        public override void Parse(Response response)
        {
            //Get the page title
            patternObject.Title = response.Css("#firstHeading").First().InnerText;
            IronWebScraper.HtmlNode PageContentNode = response.Css("#content").First(); //get the inner page content
            patternObject.Content = ProcessPageContent(PageContentNode); //save the page HTML
            
            //get all the links in the content
            foreach (var link in response.Css("#bodyContent").First().Css("a[href]"))
            {
                if (link.Attributes["href"].Contains("redlink=1")) continue; //skip if this is a redlink (page doesn't exist).
                if (link.Attributes["href"].Split('#').First() == response.FinalUrl) continue; //skip if this links to this page
                Pattern.PatternLink linkObject = new Pattern.PatternLink();
                linkObject.To = link.InnerText;
                //linkObject.RelatingParagraph = link.ParentNode.InnerTextClean;

                patternObject.PatternsLinks.Add(linkObject);
            }

            //get all the links in the bottom category box (guaranteed to be this pages categories)
            foreach (var link in response.Css("#catlinks").First().Css("a[href]"))
            {
                if (link.Attributes["href"].Contains("redlink=1")) continue; //skip if this is a redlink (page doesn't exist).
                if (link.InnerText.Contains("Patterns")) //check if its a pattern category
                {
                    patternObject.Categories.Add(link.InnerText);
                }
            }

            string Json = JsonConvert.SerializeObject(patternObject);
            
            File.WriteAllText( Pattern.GetFileName(patternObject.Title) , Json);

        }
    }
}
