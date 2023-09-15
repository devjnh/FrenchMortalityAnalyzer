using MortalityAnalyzer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrenchMortalityAnalyzer
{
    class DeathLogs
    {
        public DatabaseEngine DatabaseEngine { get; set; }
        public bool FilesInserted { get; set; } = false;
        public void Extract(string deathsLogFolder)
        {
            string doneFolder = Path.Combine(deathsLogFolder, "Done");
            if (!Directory.Exists(doneFolder))
                Directory.CreateDirectory(doneFolder);
            DatabaseEngine.Prepare(DeathEntry.CreateDataTable(), false);
            IEnumerable<DeathLogFile> logFiles = Directory.EnumerateFiles(deathsLogFolder, "*.txt").Select(f => new DeathLogFile(f)).Where(f => f.IsValid).OrderBy(f => f.StartDate);
            foreach (DeathLogFile item in logFiles)
            {
                using (FileStream fileStream = new FileStream(item.FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    using (StreamReader textReader = new StreamReader(fileStream))
                    {
                        Console.WriteLine($"Inserting file {item} [{item.StartDate}, {item.EndDate}[");
                        DateTime endDate = item.EndDate.AddDays(2);
                        int errors = 0;
                        while (!textReader.EndOfStream)
                        {
                            string line = textReader.ReadLine();
                            DeathEntry deathEntry = new DeathEntry { LogFile = item.FileName };
                            if (deathEntry.Parse(line))
                            {
                                errors = 0;
                                if (deathEntry.DeathDate < endDate)
                                    DatabaseEngine.Insert(deathEntry);
                                else
                                    Console.WriteLine($"Invalid death date found ({deathEntry.DeathDate}). Ignoring entry.\n{line}");
                            }
                            else if (++errors > 5)
                            {
                                Console.WriteLine($"File {item} is corrupted!");
                                break;
                            }
                        }
                    }
                }
                File.Move(item.FilePath, Path.Combine(doneFolder, item.FileName));
                FilesInserted = true;
            }
            DatabaseEngine.FinishInsertion();
        }
    }
}
