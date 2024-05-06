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
            Regression regression = new LogRegression();
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
                double rsquared, yintercept, slope;
                LinearRegression(xVals, yVals, 0, xVals.Count, out rsquared, out yintercept, out slope);
                foreach (DataRow dataRow in dataRows)
                {
                    double year = TimeToDouble(dataRow[0]);
                    double baseLine = yintercept + slope * year;
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

        /// <summary>
        /// Fits a line to a collection of (x,y) points.
        /// </summary>
        /// <param name="xVals">The x-axis values.</param>
        /// <param name="yVals">The y-axis values.</param>
        /// <param name="inclusiveStart">The inclusive inclusiveStart index.</param>
        /// <param name="exclusiveEnd">The exclusive exclusiveEnd index.</param>
        /// <param name="rsquared">The r^2 value of the line.</param>
        /// <param name="yintercept">The y-intercept value of the line (i.e. y = ax + b, yintercept is b).</param>
        /// <param name="slope">The slop of the line (i.e. y = ax + b, slope is a).</param>
        public static void LinearRegression(IList<double> xVals, IList<double> yVals,
                                            int inclusiveStart, int exclusiveEnd,
                                            out double rsquared, out double yintercept,
                                            out double slope)
        {
            double sumOfX = 0;
            double sumOfY = 0;
            double sumOfXSq = 0;
            double sumOfYSq = 0;
            double ssX = 0;
            double ssY = 0;
            double sumCodeviates = 0;
            double sCo = 0;
            double count = exclusiveEnd - inclusiveStart;

            for (int ctr = inclusiveStart; ctr < exclusiveEnd; ctr++)
            {
                double x = xVals[ctr];
                double y = yVals[ctr];
                sumCodeviates += x * y;
                sumOfX += x;
                sumOfY += y;
                sumOfXSq += x * x;
                sumOfYSq += y * y;
            }
            ssX = sumOfXSq - ((sumOfX * sumOfX) / count);
            ssY = sumOfYSq - ((sumOfY * sumOfY) / count);
            double RNumerator = (count * sumCodeviates) - (sumOfX * sumOfY);
            double RDenom = (count * sumOfXSq - (sumOfX * sumOfX))
             * (count * sumOfYSq - (sumOfY * sumOfY));
            sCo = sumCodeviates - ((sumOfX * sumOfY) / count);

            double meanX = sumOfX / count;
            double meanY = sumOfY / count;
            double dblR = RNumerator / Math.Sqrt(RDenom);
            rsquared = dblR * dblR;
            yintercept = meanY - ((sCo / ssX) * meanX);
            slope = sCo / ssX;
        }
    }
}
