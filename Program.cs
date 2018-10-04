using System;
using IronWebScraper;
using System.Linq;
using System.Collections.Generic;

namespace Parser
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            var scraper = new PatternScraper();
            scraper.Start();
            Console.ReadLine();
        }
    }

    class PatternScraper : WebScraper
    {
        Dictionary<String, String> Patterns = new Dictionary<String, String>(); //This stores the name of each pattern, and its URL
        public override void Init()
        {
            this.LoggingLevel = WebScraper.LogLevel.All;
            this.Request("http://virt10.itu.chalmers.se/index.php/Category:Patterns", Parse);
        }

        public override void Parse(Response response)
        {
            var Categories = response.Css("#mw-pages");
            
            //get each pattern Title and URL
            foreach (var titletext in Categories.CSS("ul > li > a"))
            {
                Patterns.Add(titletext.TextContent, titletext.Attributes["href"]);
            }

            //get the link to the next page of pages and follow it
            var NextPageLink = response.Css("a").Where(x => x.InnerText == "next page").FirstOrDefault();
            if(NextPageLink != null)
            {
                this.Request(NextPageLink.Attributes["href"], Parse);
            }
            else //we are done
            {
                Console.WriteLine("Collected " + Patterns.Count + " pattens.");
            }
        }
    }
}
