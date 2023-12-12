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

            //SyllabicateSong(PATH_HERE);
            //CreateSongSkeleton(PATH_HERE);

        }

        /// <summary>
        /// Creates a song file of all notes of length 3 that are middle C's
        /// </summary>
        /// <param name="LyricsTextPath"></param>
        public static void CreateSongSkeleton(string LyricsTextPath)
        {
            int DEFAULT_SYLLABLE_BEATS = 5;

            List<string> Lines = File.ReadAllLines(LyricsTextPath).ToList();
            Glossary = LoadGlossary();

            List<string> Output = new List<string>();
            int SongCounter = 0;

            foreach (string Line in Lines)
            {
                var Words = Line.Split(' ');
                foreach (var W in Words)
                {
                    // Clean up the word
                    string Word = W.Replace(",", string.Empty).Replace("\"", string.Empty).Replace("?", string.Empty).Replace("!", string.Empty).Replace(".", string.Empty).Replace(";", string.Empty);

                    GlossaryItem Syllabicated = Glossary.SingleOrDefault(x => x.Word == Word.Trim ().ToLower ());
                    if (Syllabicated == null)
                    {
                        Console.WriteLine("Attempting to get syllabication for " + Word.Trim().ToLower());
                        Syllabicated = Syllabicator.Syllabicate(Word.Trim().ToLower());
                        Glossary.Add(Syllabicated);
                        SaveGlossary(Glossary);
                    }

                    for (int i = 0; i < Syllabicated.Syllables.Length; i++)
                    {
                        var ThisSongLine = new SongLine();
                        ThisSongLine.Starter = ":";
                        ThisSongLine.StartingTime = SongCounter;
                        ThisSongLine.NoteLength = DEFAULT_SYLLABLE_BEATS;
                        ThisSongLine.NoteValue = 12;
                        ThisSongLine.Syllable = Syllabicated.Syllables[i];

                        if (i == 0 && Word.Substring(0, 1).ToUpper() == Word.Substring(0, 1))
                        {
                            // Capitalize this syllable
                            ThisSongLine.Syllable = ThisSongLine.Syllable.Substring(0, 1).ToUpper() + ThisSongLine.Syllable.Substring(1);
                        }
                        if (i == Syllabicated.Syllables.Length - 1)
                            ThisSongLine.Syllable += " "; // Add a space at the end of the word
                        Output.Add(ThisSongLine.ToString());
                        SongCounter += DEFAULT_SYLLABLE_BEATS+1;
                    }

                }

                SongCounter++;
                Output.Add(new BreakLine(SongCounter).ToString());

                SongCounter += DEFAULT_SYLLABLE_BEATS*2; // Add more beats for the line break?
            }
            Output.Add("E");

            //foreach(var SongLine in Output)
            //{
            //    File.AppendAllText(LyricsTextPath.Replace(".txt", ".song.txt"), SongLine.ToString () + Environment.NewLine);
            //}
            File.WriteAllLines(LyricsTextPath.Replace(".txt", ".song.txt"), Output);
        }

        private static void SyllabicateSong(string SongPath)
        {
            Glossary = LoadGlossary();

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