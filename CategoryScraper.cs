using System;
using IronWebScraper;
using System.Linq;
using System.Collections.Generic;

namespace Parser
{
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
            HtmlNode NextPageLink = response.Css("a[href]").Where(x => x.InnerText == "next page").FirstOrDefault();
            if(NextPageLink != null)
            {
                //the following section removes the hash and everything after, 
                //because it confuses the scraper library for some reason
                string input = NextPageLink.Attributes["href"];
                int index = input.IndexOf("#");
                if (index > 0)
                    input = input.Substring(0, index);

                this.Request(input, Parse);
            }
            else //we are done
            {
                Console.WriteLine("Collected " + PagesDictionary.Count + " pages.");
            }
        }
    }
}
