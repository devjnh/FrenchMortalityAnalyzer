using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MortalityAnalyzer
{
    internal class LogFileDownloader
    {
        public string LogFolder { get; }
        const string _BaseUrl = "https://www.data.gouv.fr/fr/datasets/fichier-des-personnes-decedees/";
        public int MinYear { get; set; } = 2001;

        public LogFileDownloader(string logFolder)
        {
            LogFolder = logFolder;
        }
        public static IEnumerable<string> GetLogfileUrls()
        {
            string page;
            using (WebClient wc = new WebClient())
            {
                page = wc.DownloadString(_BaseUrl);
            }
            Regex regex = new Regex("\"(https://static.data.gouv.fr/resources/fichier-des-personnes-decedees/[0-9]{8}-[0-9]{6}/deces-[0-9]{4}-?[mt]?[0-9]*.txt)\"");
            var matches = regex.Matches(page);
            SortedSet<string> urls = new SortedSet<string>();
            foreach (Match match in matches)
                if (match.Success)
                    urls.Add(match.Groups[1].Value);

            return urls;
        }
        public IEnumerable<DeathLogFile> GetLogFiles(DateTime minDate)
        {
            IOrderedEnumerable<DeathLogFile> deathLogFiles = GetLogfileUrls().Select(url => new DeathLogFile(url, LogFolder)).Where(f => f.StartDate >= minDate).OrderBy(f => f.StartDate).ThenByDescending(f => f.EndDate);
            DateTime upperDate = DateTime.MinValue;
            foreach (DeathLogFile deathLogFile in deathLogFiles)
            {
                if (deathLogFile.StartDate >= upperDate)
                {
                    upperDate = deathLogFile.EndDate;
                    yield return deathLogFile;
                }
            }
        }
        public IEnumerable<DeathLogFile> GetLogFiles()
        {
            return GetLogFiles(GetMaxDateDone());
        }

        DateTime GetMaxDateDone()
        {
            DateTime maxDate = new DateTime(MinYear, 1, 1);
            string doneFolder = Path.Combine(LogFolder, "Done");
            if (Directory.Exists(doneFolder))
            {
                DeathLogFile[] logFilesDone = Directory.EnumerateFiles(doneFolder, "*.txt").Select(f => new DeathLogFile(f)).Where(f => f.IsValid).ToArray();

                if (logFilesDone.Length > 0)
                    maxDate = logFilesDone.Max(f => f.EndDate);

            }
            return maxDate;
        }

        public void DownloadMissingFiles()
        {
            var logfiles = GetLogFiles().Where(f => !f.IsDownloaded);
            foreach (DeathLogFile deathLogFile in logfiles)
            {
                Console.Write($"Downloading {deathLogFile.Url} ");
                using (WebClient wc = new WebClient())
                {
                    wc.DownloadFile(deathLogFile.Url, deathLogFile.FilePath);
                }
                Console.WriteLine("- Done");
            }
        }
    }
}
