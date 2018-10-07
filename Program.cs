using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace Parser
{
    class Program
    {
        public static List<String> PatternNames;
        public static List<String> GameNames;
        public static List<String> GameCategories;

        static void Main(string[] args)
        {
            #region Load data
            //Get all the patterns names and URLs
            CategoryScraper patternsScraper;
            if (!File.Exists("PatternPages.json")) //if we havn't downloaded them before
            {
                patternsScraper = new CategoryScraper("Patterns");
                patternsScraper.Start();
                File.WriteAllText("PatternPages.json", JsonConvert.SerializeObject(patternsScraper));
            }
            else //if we have already downloaded them
            {
                string text = File.ReadAllText("PatternPages.json");
                patternsScraper = JsonConvert.DeserializeObject<CategoryScraper>(text);
            }
            PatternNames = patternsScraper.PagesDictionary.Keys.ToList();

            var gamesWithCategories = GetGamesWithCategories();
            GameNames = gamesWithCategories.Keys.ToList();
            GameCategories = gamesWithCategories.Values.SelectMany(x => x).ToList();


            List<Pattern> Patterns = new List<Pattern>();
            foreach (var pattern in patternsScraper.PagesDictionary)
            {
                Pattern patternOutput;
                if (!File.Exists(Pattern.GetFileName(pattern.Key)))
                { //if we don't have this pattern file yet
                    PatternPageScraper patternScraper = new PatternPageScraper(pattern.Value);
                    patternScraper.Start();
                    patternOutput = patternScraper.patternObject;
                }
                else //if we already have the pattern file
                {
                    string text = File.ReadAllText(Pattern.GetFileName(pattern.Key));
                    patternOutput = JsonConvert.DeserializeObject<Pattern>(text);
                }
                Patterns.Add(patternOutput);
            } 
            #endregion

            { //block for producing special JSON file
               
                var FilteredGames = gamesWithCategories.Where(game => game.Value.Contains("Social Media Games"));
                var FilteredPatterns = Patterns.Where(pattern => pattern.PatternsLinks.Any(link => FilteredGames.Any(game => game.Key == link.To)));
                String[] FilteredPatternsNames = FilteredPatterns.Select(pattern => pattern.Title).ToArray();

                var nodes = Enumerable.Empty<object>().Select(x => new { id = "", group = 0 }).ToList();
                foreach (Pattern patternObject in FilteredPatterns)
                {
                    
                    nodes.Add(new
                    {
                        id = patternObject.Title,
                        group = new Random().Next(1,4)
                    });
                }
                Console.WriteLine("Added "+ nodes.Count + " nodes");

                var links = Enumerable.Empty<object>().Select(x => new { source = "", target = "", value = 1 }).ToList();


                ConcurrentBag<Pattern.PatternLink> PatternLinks = new ConcurrentBag<Pattern.PatternLink>();
                Parallel.ForEach(FilteredPatterns, (patternObject) =>
                {
                    Console.WriteLine("Pattern: " + patternObject.Title + " contains " + patternObject.PatternsLinks.Count() + " total links, filtering now.");

                    var FilteredLinks = (from link in patternObject.PatternsLinks.AsParallel()
                        where FilteredPatternsNames.Contains(link.To) //enforces that we only link to other patterns
                        select link);
                    Console.WriteLine("Pattern: " + patternObject.Title + " contains " + FilteredLinks.Count() + " links after filtering, " + (patternObject.PatternsLinks.Count() - FilteredLinks.Count()) + " removed.");
                    FilteredLinks.ForAll(x => PatternLinks.Add(x));
                });
                Console.WriteLine("All PattenLinks found.");


                foreach (Pattern.PatternLink link in PatternLinks)
                {
                    links.Add(new
                    {
                        source = link.From,
                        target = link.To,
                        value = new Random().Next(1, 4)
                    });
                }

                File.WriteAllText("nodes.json", JsonConvert.SerializeObject(new {nodes, links}));
            }
        }

        static Dictionary<String, List<String>> GetGamesWithCategories() //this fuction gets a kvp of Games with there Subcategory
        {
            //Create a dictionary to hold games (key) with its subcategories (value)
            Dictionary<String, List<String>> GamesWithCategories = new Dictionary<String, List<String>>();

            if (!File.Exists("games.json")) //if the file doesn't exist already
            {
                Console.WriteLine("Getting games with their categories: Fresh.");

                //Scrape the games category
                CategoryScraper gamesScraper = new CategoryScraper("Games");
                gamesScraper.Start();

                //iterate though all the subcategories
                foreach (string gameSubcategoryName in gamesScraper.SubcategoryDictionary.Keys.ToList())
                {
                    CategoryScraper gamesSubcategoryScraper = new CategoryScraper(gameSubcategoryName);
                    gamesSubcategoryScraper.Start();

                    //iterate though all the games in that subcategory
                    foreach (string gameTitle in gamesSubcategoryScraper.PagesDictionary.Keys)
                    {
                        if (GamesWithCategories.ContainsKey(gameTitle)) //if we already have the game in the dictionary
                        {
                            GamesWithCategories.First(x => x.Key == gameTitle).Value.Add(gameSubcategoryName);
                        }
                        else //if it doesn't exist in the dictionary already
                        {
                            GamesWithCategories.Add(gameTitle, new List<String>() { gameSubcategoryName });
                        }

                    }
                }
                File.WriteAllText("games.json", JsonConvert.SerializeObject(GamesWithCategories));
            }
            else //if the file does exist
            {
                Console.WriteLine("Getting games with their categories: from existing file.");
                GamesWithCategories = JsonConvert.DeserializeObject<Dictionary<String, List<String>>>(File.ReadAllText("games.json"));
            }
            return GamesWithCategories;
        }
    }
}
