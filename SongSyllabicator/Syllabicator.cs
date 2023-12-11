using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SongSyllabicator
{
    public class Syllabicator
    {
        private static string[] DictLines=null;


        public static GlossaryItem Syllabicate (string Word)
        {
            if(Word.Contains("'"))
            {
                // Just manually ask the syllables because the website seems to do poorly with contractions
                Console.WriteLine("Please type out the syllables for the word: ** " + Word  + " ** below, using spaces between each syllable.");
                string Syls = Console.ReadLine();
                return new GlossaryItem
                {
                    Word =  Word,
                    Syllables = Syls.Split(' ')
                };
            }
            
            else 
            {
                // Try to get syllables from file
                string[] syls = SyllablesForWordFromFile(Word);
                Thread.Sleep(100); // Sleep so that we don't slam the how many syllables website

                if (syls!=null)
                    return new GlossaryItem {Word=Word, Syllables = syls };

                // Try to get the syllables from howmanysyllables.com
                string url = "https://www.howmanysyllables.com/syllables/" + Word;
                string html = new WebClient().DownloadString(url);
                string SyllableInfoCheckString = "into syllables: &nbsp; <span class=\"Answer_Red\" data-nosnippet>";
                Thread.Sleep(502); // Sleep so that we don't slam the how many syllables website

                if (html.Contains(SyllableInfoCheckString))
                {

                    var parts = html.Replace (SyllableInfoCheckString, "☯") .Split('☯');
                    string syllables = parts[1].Split('<')[0];
                    return new GlossaryItem
                    {
                        Word =Word,
                        Syllables = syllables.Split('-')
                    };
                }               
                else
                {
                    // https://dictionaryapi.dev/ ?
                    //return new GlossaryItem
                    //{
                    //    Word = Word,
                    //    Syllables = new string[] { Word }
                    //};

                    // Just manually ask the syllables because there is no syllable info available
                    Console.WriteLine("Please type out the syllables for the word: ** " + Word + " ** below, using spaces between each syllable.");
                    string Syls = Console.ReadLine();
                    return new GlossaryItem
                    {
                        Word = Word,
                        Syllables = Syls.Split(' ')
                    };
                }

            }

        }

        private static string[] SyllablesForWordFromFile(string word)
        {
            if (DictLines == null)
                DictLines = File.ReadAllLines("dict-en.txt");

            var Matches = DictLines.Where(x=>x.StartsWith (word+"|")).ToList();

            if (Matches.Count != 1)
                return null;

            return Matches[0].Split('|')[1].Split('-');
        }
    }
}
