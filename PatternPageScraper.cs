using System;
using IronWebScraper;
using System.Linq;
using Newtonsoft.Json;
using System.IO;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using System.Collections.Generic;

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

        private static HtmlAgilityPack.HtmlNode GetNodeReleventPageHeading(HtmlAgilityPack.HtmlNode Node, String NodeName)
        {
            //get to the paragraph level and get its previous sibling
            var headingNode = Node.ParentNode;
            //iterate through the previous nodes until we get
            while (headingNode != null && headingNode.Name != NodeName)
            {
                headingNode = headingNode.PreviousSibling;
            }
            return headingNode;
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

                //get relation links
                if (GetNodeReleventPageHeading(link, "h2") != null 
                    && GetNodeReleventPageHeading(link, "h2").InnerText == "Relations")
                {
                    //get the relation type of this relation and get its inner text
                    HtmlAgilityPack.HtmlNode RelationHeadingNode = GetNodeReleventPageHeading(link, "h3");
                    String RelationName = RelationHeadingNode.InnerText;
                    
                    //if there is a h4 node before the previous h3 node
                    if(GetNodeReleventPageHeading(link, "h4") != null 
                        && RelationHeadingNode.InnerStartIndex < GetNodeReleventPageHeading(link, "h4").InnerStartIndex)
                    {
                        //assume it is a "with x" sub-category of relation for the "Can Instantiate" section
                        RelationName = RelationHeadingNode.InnerText + " " + GetNodeReleventPageHeading(link, "h4").InnerText;
                    }

                    //if the pattern object doesn't contain this relation type already
                    if (!patternObject.Relations.Any(relation => relation.Key == RelationName))
                    {
                        //add a list for a relation of this type
                        patternObject.Relations.Add(RelationName, new List<String>());
                    }
                    //add the links inner text to the relevent relation's list
                    patternObject.Relations[RelationName].Add(link.InnerText);
                }
            }

            string Json = JsonConvert.SerializeObject(patternObject);
            
            File.WriteAllText( Pattern.GetFileName(patternObject.Title) , Json);

        }
    }
}
