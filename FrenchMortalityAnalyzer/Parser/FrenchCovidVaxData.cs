using MortalityAnalyzer.Downloaders;
using MortalityAnalyzer.Model;
using MortalityAnalyzer.Parser;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MortalityAnalyzer.Parser
{
    internal class FrenchCovidVaxData : CsvParser
    {
        public FrenchCovidVaxData()
        {
            Separator = ';';
        }
        public void Extract(string deathsLogFolder)
        {
            FileDownloader fileDownloader = new FileDownloader(deathsLogFolder);
            string fileName = "FrenchCovidVaxData.csv";
            fileDownloader.Download(fileName, "https://www.data.gouv.fr/fr/datasets/r/54dd5f8d-1e2e-4ccb-8fb8-eac68245befd");
            Console.WriteLine("Importing Covid vaccination statistics");
            ImportFromCsvFile(Path.Combine(deathsLogFolder, fileName));
        }

        protected override DataTable CreateDataTable()
        {
            return DatabaseEngine.CreateDataTable(typeof(VaxStatistic));
        }

        protected override object GetEntry(string[] split)
        {
            VaxStatistic vaxStatistic = new VaxStatistic();
            vaxStatistic.Date = Convert.ToDateTime(GetValue("jour", split));
            vaxStatistic.Country = GetValue("fra", split);
            vaxStatistic.D1 = GetIntValue("n_dose1", split);
            vaxStatistic.D2 = GetIntValue("n_complet", split);
            vaxStatistic.D3 = GetIntValue("n_rappel", split);
            int ageGroup = GetIntValue("clage_vacsi", split);
            if (ageGroup == 0)
                return null;
            SetAgeRange(vaxStatistic, ageGroup);

            return vaxStatistic;
        }

        private static void SetAgeRange(VaxStatistic vaxStatistic, int ageGroup)
        {
            switch (ageGroup)
            {
                //case 4:
                //    vaxStatistic.Age = 0;
                //    vaxStatistic.AgeSpan = 5;
                //    break;
                //case 9:
                //    vaxStatistic.Age = 5;
                //    vaxStatistic.AgeSpan = 5;
                //    break;
                case 11:
                    vaxStatistic.Age = 10;
                    vaxStatistic.AgeSpan = 2;
                    break;
                case 17:
                    vaxStatistic.Age = 12;
                    vaxStatistic.AgeSpan = 6;
                    break;
                case 24:
                    vaxStatistic.Age = 18;
                    vaxStatistic.AgeSpan = 7;
                    break;
                //case 29:
                //    vaxStatistic.Age = 25;
                //    vaxStatistic.AgeSpan = 5;
                //    break;
                case 80:
                    vaxStatistic.Age = 80;
                    vaxStatistic.AgeSpan = -1;
                    break;
                default:
                    vaxStatistic.AgeSpan = ageGroup >= 30 & ageGroup < 60 ? 10 : 5;
                    vaxStatistic.Age = ageGroup + 1 - vaxStatistic.AgeSpan;
                    break;
            }
        }

        public bool IsBuilt => DatabaseEngine.DoesTableExist(typeof(VaxStatistic));
    }
}
