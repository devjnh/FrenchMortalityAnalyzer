using MortalityAnalyzer.Downloaders;
using MortalityAnalyzer.Model;
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
    internal abstract class CsvParser
    {
        public DatabaseEngine DatabaseEngine { get; set; }

        public char Separator { get; set; } = ',';

        Dictionary<string, int> _Fields = new Dictionary<string, int>();
        protected void ImportFromCsvFile(string filePath)
        {
            DatabaseEngine.Prepare(CreateDataTable(), false);
            using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (StreamReader textReader = new StreamReader(fileStream))
                {
                    string line = textReader.ReadLine();
                    string[] fields = line.Split(Separator);
                    for (int i = 0; i < fields.Length; i++)
                        this._Fields[fields[i]] = i;
                    while (!textReader.EndOfStream)
                    {
                        line = textReader.ReadLine();
                        object entry = GetEntry(line.Split(Separator));
                        if (entry != null)
                            DatabaseEngine.Insert(entry);
                    }
                }
            }
            DatabaseEngine.FinishInsertion();
        }
        protected string GetValue(string fieldName, string[] values)
        {
            return values[_Fields[fieldName]];
        }
        protected int GetIntValue(string fieldName, string[] values)
        {
            string textValue = GetValue(fieldName, values);
            int value;
            if (int.TryParse(textValue, out value))
                return value;
            else
                return 0;
        }
        protected abstract DataTable CreateDataTable();
        protected abstract object GetEntry(string[] split);
        public static DateTime FirstDateOfWeekISO8601(int year, int weekOfYear)
        {
            DateTime jan1 = new DateTime(year, 1, 1);
            int daysOffset = DayOfWeek.Thursday - jan1.DayOfWeek;

            // Use first Thursday in January to get first week of the year as
            // it will never be in Week 52/53
            DateTime firstThursday = jan1.AddDays(daysOffset);
            var cal = CultureInfo.CurrentCulture.Calendar;
            int firstWeek = cal.GetWeekOfYear(firstThursday, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);

            var weekNum = weekOfYear;
            // As we're adding days to a date in Week 1,
            // we need to subtract 1 in order to get the right date for week #1
            if (firstWeek == 1)
            {
                weekNum -= 1;
            }

            // Using the first Thursday as starting week ensures that we are starting in the right year
            // then we add number of weeks multiplied with days
            var result = firstThursday.AddDays(weekNum * 7);

            // Subtract 3 days from Thursday to get Monday, which is the first weekday in ISO8601
            return result.AddDays(-3);
        }

    }

}
