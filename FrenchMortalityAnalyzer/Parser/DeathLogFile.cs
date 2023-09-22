using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace MortalityAnalyzer
{
    class DeathLogFile
    {
        public DeathLogFile(string url, string logFolder)
        {
            Url = url;
            Uri uri = new Uri(url);
            FileName = Path.GetFileName(uri.LocalPath);
            FilePath = Path.Combine(logFolder, Path.GetFileName(uri.LocalPath));
            CalculateTimeFrame();
        }
        public DeathLogFile(string filePath)
        {
            FilePath = filePath;
            FileName = Path.GetFileName(filePath);
            CalculateTimeFrame();
        }

        private void CalculateTimeFrame()
        {
            StartDate = DateTime.MinValue;
            EndDate = DateTime.MinValue;
            Regex regex = new Regex("deces-([0-9][0-9][0-9][0-9]).txt");
            var result = regex.Match(FileName);
            if (result.Success)
            {
                int year = int.Parse(result.Groups[1].Value);
                StartDate = new DateTime(year, 1, 1);
                EndDate = StartDate.AddYears(1);
            }

            regex = new Regex("deces-([0-9][0-9][0-9][0-9])-m([0-9][0-9]).txt");
            result = regex.Match(FileName);
            if (result.Success)
            {
                int year = int.Parse(result.Groups[1].Value);
                int month = int.Parse(result.Groups[2].Value);
                StartDate = new DateTime(year, month, 1);
                EndDate = StartDate.AddMonths(1);
            }

            regex = new Regex("deces-([0-9][0-9][0-9][0-9])-t([0-9]).txt");
            result = regex.Match(FileName);
            if (result.Success)
            {
                int year = int.Parse(result.Groups[1].Value);
                int month = (int.Parse(result.Groups[2].Value) - 1) * 3 + 1;
                StartDate = new DateTime(year, month, 1);
                EndDate = StartDate.AddMonths(3);
            }
        }

        public string FilePath { get; }
        public string FileName { get; }
        public string Url { get; }
        public DateTime StartDate { get; private set; }
        public DateTime EndDate { get; private set; }
        public bool IsValid => StartDate > DateTime.MinValue;

        bool isDownloaded;

        public bool IsDownloaded => File.Exists(FilePath);
        public override string ToString()
        {
            return FileName;
        }

    }
}
