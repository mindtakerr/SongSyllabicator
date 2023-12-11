using Newtonsoft.Json;
using System.Net;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;

namespace SongSyllabicator
{
    internal class Program
    {
        private static List<GlossaryItem> Glossary;

        static void Main(string[] args)
        {
            Glossary = LoadGlossary();

            string SongPath = "PATH_TO_TXT_FILE_HERE";

            List<string> Output = new List<string>();
            var Lines = File.ReadAllLines(SongPath, System.Text.Encoding.UTF8);

            foreach (string Line in Lines)
            {
                if (Line.StartsWith(":") || Line.StartsWith("*") || Line.StartsWith("R") || Line.StartsWith("G"))
                {
                    // Do Something
                    var songLine = new SongLine(Line);

                    // See if the syllable word is already in our Glossary
                    GlossaryItem GItem = Glossary.SingleOrDefault(x => x.Word == songLine.SyllableWord);

                    if (GItem == null)
                    {
                        Console.WriteLine("Attempting to get syllabication for " + songLine.SyllableWord);
                        GItem = Syllabicator.Syllabicate(songLine.SyllableWord);
                        Glossary.Add(GItem);
                        SaveGlossary(Glossary);
                    }

                    // Now syllabicate using GItem
                    if (GItem.Syllables.Length == 1)
                        // Just add the line as is
                        Output.Add(Line);
                    else
                    {
                        // Split up the line into syllables and add them all
                        int noteLength = songLine.NoteLength / GItem.Syllables.Length;
                        int noteRemainder = songLine.NoteLength % GItem.Syllables.Length;

                        int currentNote = songLine.StartingTime;
                        for (int i = 0; i < GItem.Syllables.Length; i++)
                        {
                            var NewSongLine = new SongLine
                            {
                                Starter = songLine.Starter,
                                StartingTime = currentNote,
                                NoteLength = noteLength,
                                NoteValue = songLine.NoteValue,
                                Syllable = GItem.Syllables[i]
                            };
                            if (i == 0)
                                NewSongLine.NoteLength += noteRemainder;
                            currentNote += NewSongLine.NoteLength;

                            // If the word starts with a capital letter, then make sure that the output is capitalized
                            if (songLine.IsCapital && i == 0)
                                NewSongLine.Syllable = NewSongLine.Syllable.Substring(0, 1).ToUpper() + NewSongLine.Syllable.Substring(1);

                            // If the line ends in a space, then make sure to add that space to the end
                            if (i == GItem.Syllables.Length - 1 && Line.EndsWith(" "))
                                NewSongLine.Syllable += " ";

                            if (songLine.HasLeadingSpace && i == 0)
                                NewSongLine.Syllable = " " + NewSongLine.Syllable;

                            Output.Add(NewSongLine.ToString());

                        }
                    }
                }
                else
                {
                    // Just add the line as is
                    Output.Add(Line);
                }
            }

            // Sort the Glossary
            Glossary = Glossary.OrderBy(x => x.Word).ToList();
            SaveGlossary(Glossary);

            // Try to fix apostrophes?
            for (int i = 0; i < Output.Count; i++)
            {
                Output[i] = Output[i].Replace("'", "’");
            }


            // Save the Output
            File.WriteAllLines(SongPath.Replace(".txt", "-syl.txt"), Output, System.Text.Encoding.UTF8);
        }

        //private static GlossaryItem? GetSyllables(SongLine songLine)
        //{
        //    GlossaryItem GItem;

        //    // Look up the word and ascertain it
        //    if (songLine.SyllableWord.Contains("'"))
        //    {
        //        // Just manually ask the syllables because the website seems to do poorly with contractions
        //        Console.WriteLine("Please type out the syllables for the word: " + songLine.SyllableWord + " below, using spaces between each syllable.");
        //        string Syls = Console.ReadLine();
        //        GItem = new GlossaryItem
        //        {
        //            Word = songLine.SyllableWord,
        //            Syllables = Syls.Split(' ')
        //        };
        //        Glossary.Add(GItem);
        //        SaveGlossary(Glossary);
        //    }
        //    else
        //    {
        //        // Check howmanysyllables.com
        //        try
        //        {
        //            Console.WriteLine("Getting syllables for word: " + songLine.SyllableWord);

        //            GItem = Syllabicator.Syllabicate(songLine.SyllableWord);
        //            Glossary.Add(GItem);
        //            SaveGlossary(Glossary);
        //        }
        //        catch
        //        {
        //            // If we don't get an answer from the website, then it's probably already been syllabicated. Just add it and move on.
        //            GItem = new GlossaryItem
        //            {
        //                Word = songLine.SyllableWord,
        //                Syllables = new string[] { songLine.SyllableWord }
        //            };
        //            Glossary.Add(GItem);
        //            SaveGlossary(Glossary);
        //        }
        //        finally
        //        {
        //            Thread.Sleep(502); // Sleep so that we don't slam the how many syllables website
        //        }
        //    }
        //}

        private static void SaveGlossary(List<GlossaryItem> Glossary)
        {
            File.WriteAllText("C:\\Users\\graph\\source\\repos\\SongSyllabicator\\SongSyllabicator\\glossary-en.json", JsonConvert.SerializeObject(Glossary, Formatting.Indented), System.Text.Encoding.UTF8);
        }

        private static List<GlossaryItem> LoadGlossary()
        {
            try
            {
                return JsonConvert.DeserializeObject<List<GlossaryItem>>(File.ReadAllText("C:\\Users\\graph\\source\\repos\\SongSyllabicator\\SongSyllabicator\\glossary-en.json", System.Text.Encoding.UTF8));
            }
            catch
            {
                return new List<GlossaryItem>();
            }
        }
    }
}