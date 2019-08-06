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

        static bool CheckForYes()
        {
            try
            {
                if(Console.ReadKey().KeyChar == 'y')
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch(System.InvalidOperationException) //this console can't input characters (run through VS?)
            {
                Console.WriteLine("In a console with no input, auto no.");
                return false;
            }
        }

        static void Main(string[] args)
        {
            #region Load data
            //Get all the patterns names and URLs
            CategoryScraper patternsScraper = null;
            if (File.Exists("PatternPages.json")) //if we have downloaded them before
            {
                Console.WriteLine("To read in existing pattern page values, press y");
                if (CheckForYes())
                {
                    string text = File.ReadAllText("PatternPages.json");
                    patternsScraper = JsonConvert.DeserializeObject<CategoryScraper>(text);
                }
            }
            if(patternsScraper == null) //if we couldn't read them for any reason
            {
                patternsScraper = new CategoryScraper("Patterns");
                patternsScraper.Start();
                File.WriteAllText("PatternPages.json", JsonConvert.SerializeObject(patternsScraper));  
            }
            PatternNames = patternsScraper.PagesDictionary.Keys.ToList();

            var gamesWithCategories = GetGamesWithCategories();
            GameNames = gamesWithCategories.Keys.ToList();
            GameCategories = gamesWithCategories.Values.SelectMany(x => x).ToList();

            if (Directory.Exists("patterns"))
            {
                Console.WriteLine("Some patterns may have been downloaded already. keep these patterns? y/n");
                if (!CheckForYes())
                {
                    new DirectoryInfo("patterns").Delete(true);
                    new DirectoryInfo("patterns").Create();
                }
            }
            else
            {
                new DirectoryInfo("patterns").Create();
            }
            

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

            { //file writing segment
                var PatternsJSON = JsonConvert.SerializeObject(Patterns);
                PatternsJSON = PatternsJSON.Replace("\"Type\":[],", ""); //remove empty Type
                File.WriteAllText("AllPatterns.json", PatternsJSON);

                //Make the JSON more useable by changing its layout
                List<dynamic> reconfigeredList = new List<dynamic>();
                foreach(var gameWithCategories in gamesWithCategories)
                {
                    var data = new
                    {
                        name = gameWithCategories.Key,
                        categories = gameWithCategories.Value
                    };
                    reconfigeredList.Add(data);
                }
                File.WriteAllText("AllGames.json", JsonConvert.SerializeObject(reconfigeredList));
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
