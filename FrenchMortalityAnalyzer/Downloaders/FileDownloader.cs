using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MortalityAnalyzer.Downloaders
{
    internal class FileDownloader
    {
        public FileDownloader(string logFolder)
        {
            LogFolder = logFolder;
        }
        public string LogFolder { get; }
        public string Download(string url)
        {
            Uri uri = new Uri(url);
            string fileName = System.IO.Path.GetFileName(uri.LocalPath);
            Download(fileName, url);
            return fileName;
        }
        public void Download(string fileName, string url)
        {
            Console.WriteLine($"Downloading file {fileName} {url}");
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            using (HttpWebResponse reponse = (HttpWebResponse)request.GetResponse())
            {
                WriteToFile(fileName, reponse.GetResponseStream());
            }
        }
        protected virtual void WriteToFile(string fileName, Stream responseStream)
        {
            using (FileStream fOutStream = new FileStream(Path.Combine(LogFolder, fileName), FileMode.Create, FileAccess.Write))
            {
                responseStream.CopyTo(fOutStream);
            }
        }
    }
}
