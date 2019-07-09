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

            //remove the "toc" and "jump" and "siteSub" sections to save space and later client-side processing time
            if (ContentNode.SelectSingleNode("//*[@id=\"toc\"]") != null)
            {
                ContentNode.SelectSingleNode("//*[@id=\"toc\"]").Remove();
            }
            if (ContentNode.SelectSingleNode("//*[@id=\"jump-to-nav\"]") != null) {
                ContentNode.SelectSingleNode("//*[@id=\"jump-to-nav\"]").Remove();
            }
            if (ContentNode.SelectSingleNode("//*[@id=\"siteSub\"]") != null)
            {
                ContentNode.SelectSingleNode("//*[@id=\"siteSub\"]").Remove();
            }


            foreach(var node in ContentNode.SelectNodes("//comment()"))
            {
                node.Remove();
            }

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

                //if any of the links ancestor nodes is the "category links" part of the page
                if(link.Ancestors().Any(node => node.Id == "catlinks"))
                {
                    //add it to the patterns list of categories
                    patternObject.Categories.Add(link.InnerText);
                }
                else //assume its a normal text-body link
                {
                    //check if we don't already know about this link
                    patternObject.CreateOrGetPatternLink(link.InnerText);
                }

                //add relation info if this is a relation link
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

                    //add the relevent relation to this link
                    patternObject.CreateOrGetPatternLink(link.InnerText).AssociatedRelations.Add(RelationName);
                }
            }

            string Json = JsonConvert.SerializeObject(patternObject);
            
            File.WriteAllText( Pattern.GetFileName(patternObject.Title) , Json);

        }
    }
}
