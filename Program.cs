using System;
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
                    PatternPageScraper patternScraper = new PatternPageScraper(pattern.Value);
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

                File.WriteAllText("nodes.json", JsonConvert.SerializeObject(new {nodes, links}));
            }
        }
    }
}
