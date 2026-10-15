using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MortalityAnalyzer
{
    internal class AgeStructureDownloader
    {
        string DataFolder { get; set; }
        public AgeStructureDownloader(string rootFolder)
        {
            DataFolder = Path.Combine(rootFolder, "AgeStructure");
        }
        string[] _Urls = new string[]
        {
            // Bilan démographique 2025: population at January 1st by gender and age, one sheet per year
            "https://www.insee.fr/fr/statistiques/fichier/8721185/" + AgeStructureLoader.PopulationByAgeFile,
        };
        public bool FilesDownloaded { get; private set; }
        public void DownloadMissingFiles()
        {
            if (!Directory.Exists(DataFolder))
                Directory.CreateDirectory(DataFolder);
            foreach (string url in _Urls)
            {
                Uri uri = new Uri(url);
                string fileName = Path.GetFileName(uri.LocalPath);
                string filePath = Path.Combine(DataFolder, fileName);
                if (File.Exists(filePath))
                    continue;
                Console.Write($"Downloading {url} ");
                using (WebClient wc = new WebClient())
                {
                    wc.DownloadFile(url, filePath);
                }
                Console.WriteLine("- Done");
                FilesDownloaded = true;
            }
        }
    }
}
