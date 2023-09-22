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
            "https://www.insee.fr/fr/statistiques/fichier/6327222/fm_t6.xlsx",
            "https://www.insee.fr/fr/statistiques/fichier/6688661/Pyra2023.xlsx",
            "https://www.insee.fr/fr/statistiques/fichier/6688661/Pyra2022.xlsx",
            "https://www.insee.fr/fr/statistiques/fichier/6688661/Pyra2021.xlsx",
            "https://www.insee.fr/fr/statistiques/fichier/6688661/Pyra2020.xlsx",
        };
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
            }
        }
    }
}
