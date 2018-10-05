using System;
using IronWebScraper;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.IO;
using System.Text.RegularExpressions;

namespace Parser
{
    class Program
    {
        public static List<String> PatternNames = new List<string>();
        public static List<String> GameNames = new List<string>();
        public static List<String> GameCategories = new List<string>();

        static void Main(string[] args)
        {
            //Get all the patterns names and URLs
            CategoryScraper patternsScraper = new CategoryScraper("Patterns");
            patternsScraper.Start();
            PatternNames = patternsScraper.PagesDictionary.Keys.ToList();

            //Get all the games names and URLs
            CategoryScraper gamesScraper = new CategoryScraper("Games");
            gamesScraper.Start();
            GameNames = gamesScraper.PagesDictionary.Keys.ToList();
            GameCategories = gamesScraper.SubcategoryDictionary.Keys.ToList();

            List<Pattern> Patterns = new List<Pattern>();
            foreach (var pattern in patternsScraper.PagesDictionary)
            {
                Pattern patternOutput;
                if (!File.Exists(Pattern.GetFileName(pattern.Key))){
                    PatternScraper patternScraper = new PatternScraper(pattern.Value);
                    patternScraper.Start();
                    patternOutput = patternScraper.patternObject;
                }
                else
                {
                    string text = File.ReadAllText(Pattern.GetFileName(pattern.Key));
                    patternOutput = JsonConvert.DeserializeObject<Pattern>(text);
                }
                Patterns.Add(patternOutput);
            }

            { //block for producing special JSON file
                var nodes = Enumerable.Empty<object>().Select(x => new { id = "", group = 0 }).ToList();
                foreach (Pattern patternObject in Patterns)
                {

                    nodes.Add(new
                    {
                        id = patternObject.Title,
                        group = 1
                    });
                }

                var links = Enumerable.Empty<object>().Select(x => new { source = "", target = "", value = 1 }).ToList();
                foreach (Pattern patternObject in Patterns)
                {
                    foreach(Pattern.PatternLink link in patternObject.PatternsLinks)
                    {
                        if (link.Type == Pattern.PatternLink.LinkType.Pattern)
                        {
                            links.Add(new
                            {
                                source = link.From,
                                target = link.To,
                                value = 1
                            });
                        }
                    }
                }

                File.WriteAllText("nodes.json", JsonConvert.SerializeObject(new {
                    nodes,
                    links
                }));
            }
        }
    }

    class PatternScraper : WebScraper
    {
        public Pattern patternObject = new Pattern();
        String UrlToParse;
        public PatternScraper(String InputUrl)
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

    class CategoryScraper : WebScraper
    {
        public Dictionary<String, String> SubcategoryDictionary = new Dictionary<String, String>(); //This stores the name of each subcategory, and its URL
        public Dictionary<String, String> PagesDictionary = new Dictionary<String, String>(); //This stores the name of each page, and its URL
        string URL;
        public CategoryScraper(string Category)
        {
            URL = "http://virt10.itu.chalmers.se/index.php/Category:" + Category;
        }

        public override void Init()
        {
            this.LoggingLevel = WebScraper.LogLevel.All;
            this.Request(URL, Parse);
        }

        public override void Parse(Response response)
        {

            //get each page Title and URL
            foreach (var titletext in response.Css("#mw-pages").CSS("ul > li > a"))
            {
                PagesDictionary.Add(titletext.TextContent, titletext.Attributes["href"]);
            }

            //get each subcategory Title and URL
            foreach (var titletext in response.Css("#mw-subcategories").CSS("a[href]"))
            {
                SubcategoryDictionary.TryAdd(titletext.TextContent, titletext.Attributes["href"]);
            }

            //get the link to the next page of pages and follow it
            var NextPageLink = response.Css("a").Where(x => x.InnerText == "next page").FirstOrDefault();
            if(NextPageLink != null)
            {
                this.Request(NextPageLink.Attributes["href"], Parse);
            }
            else //we are done
            {
                Console.WriteLine("Collected " + PagesDictionary.Count + " pages.");
            }
        }
    }
}
