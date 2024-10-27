using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MortalityAnalyzer.Common
{
    public class Projection
    {
        public static void BuildProjection2(DataTable dataTable, double minYearRegression, double maxYearRegression, int yearFractions)
        {
            List<double> xVals = new List<double>();
            List<double> yVals = new List<double>();
            foreach (DataRow dataRow in dataTable.Rows)
            {
                double year = TimeToDouble(dataRow[0]);
                if (year < minYearRegression || year >= maxYearRegression)
                    continue;
                xVals.Add(year);
                yVals.Add(Convert.ToDouble(dataRow[1]));
            }
            Regression regression = yVals.Contains(0) ? new Regression() : new LogRegression();
            regression.Calculate(xVals, yVals);
            double[] deltas = new double[yearFractions];
            double[] counts = new double[yearFractions];
            foreach (DataRow dataRow in dataTable.Rows)
            {
                double year = TimeToDouble(dataRow[0]);
                int yearFraction = GetYearFraction(dataRow[0], yearFractions);
                double baseLine = regression.Y(year);
                deltas[yearFraction] += Convert.ToDouble(dataRow[1]) - baseLine;
                counts[yearFraction]++;
            }
            for (int i = 0; i < deltas.Length; i++)
                deltas[i] = deltas[i] / counts[i];
            double averageDelta = deltas.Average();

            dataTable.Columns.Add("BaseLine", typeof(double));
            dataTable.Columns.Add("Excess", typeof(double));
            dataTable.Columns.Add("RelativeExcess", typeof(double));
            foreach (DataRow dataRow in dataTable.Rows)
            {
                double year = TimeToDouble(dataRow[0]);
                int yearFraction = GetYearFraction(dataRow[0], yearFractions);
                double baseLine = regression.Y(year) + deltas[yearFraction] - averageDelta;
                dataRow["BaseLine"] = baseLine;
                double excess = Convert.ToDouble(dataRow[1]) - baseLine;
                dataRow["Excess"] = excess;
                dataRow["RelativeExcess"] = excess / baseLine;
            }
        }
        public static void BuildProjection(DataTable dataTable, int minYearRegression, int maxYearRegression, int yearFractions)
        {
            if (dataTable.Columns[0].DataType != typeof(DateTime))
                BuildProjection2(dataTable, (double)minYearRegression, (double)maxYearRegression, yearFractions);
            else
                BuildProjection2(dataTable, new DateTime(minYearRegression, 1, 1).ToOADate(), new DateTime(maxYearRegression, 1, 1).ToOADate(), yearFractions);
        }

        private static void BuildProjection(DataTable dataTable, double minDate, double maxDate, int yearFractions)
        {
            dataTable.Columns.Add("BaseLine", typeof(double));
            dataTable.Columns.Add("Excess", typeof(double));
            dataTable.Columns.Add("RelativeExcess", typeof(double));
            for (int i = 0; i < yearFractions; i++)
            {
                List<double> xVals = new List<double>();
                List<double> yVals = new List<double>();
                List<DataRow> dataRows = new List<DataRow>();
                foreach (DataRow dataRow in dataTable.Rows)
                {
                    double time = TimeToDouble(dataRow[0]);
                    int yearFraction = GetYearFraction(dataRow[0], yearFractions);
                    if (yearFraction != i)
                        continue;
                    if (time >= minDate && time < maxDate)
                    {
                        xVals.Add(time);
                        yVals.Add(Convert.ToDouble(dataRow[1]));
                    }
                    dataRows.Add(dataRow);
                }
                Regression regression = new LogRegression();
                regression.Calculate(xVals, yVals);
                foreach (DataRow dataRow in dataRows)
                {
                    double year = TimeToDouble(dataRow[0]);
                    double baseLine = regression.Y(year);
                    dataRow["BaseLine"] = baseLine;
                    double excess = Convert.ToDouble(dataRow[1]) - baseLine;
                    dataRow["Excess"] = excess;
                    dataRow["RelativeExcess"] = excess / baseLine;
                }

            }
        }

        private static double TimeToDouble(object value)
        {
            return value is DateTime ? ((DateTime)value).ToOADate() : Convert.ToDouble(value);
        }

        static int GetYearFraction(object value, int yearFractions)
        {
            if (value is DateTime)
            {
                DateTime dateTime = (DateTime)value;
                DateTime year = new DateTime(dateTime.Year, 1, 1);
                if (yearFractions == 12)
                    return dateTime.Month - 1;
                int totalDays = (int)(year.AddYears(1) - year).TotalDays;
                int fraction = (int)((double)(dateTime.DayOfYear - 1) / totalDays * yearFractions);
                if (fraction >= yearFractions)
                    throw new Exception("Invalid fraction");
                return fraction;
            }
            else
            {
                double year = Convert.ToDouble(value);
                return (int)((year - Math.Floor(year)) * yearFractions);
            }
        }
    }
}
