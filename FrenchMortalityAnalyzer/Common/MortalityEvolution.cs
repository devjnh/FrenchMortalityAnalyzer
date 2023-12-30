using MortalityAnalyzer.Common;
using MortalityAnalyzer.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MortalityAnalyzer
{
    public abstract class MortalityEvolution : MortalityEvolutionBase
    {
        public DateTime LastDay { get; private set; } = DateTime.MaxValue;
        internal DatabaseEngine DatabaseEngine { get; set; }
        public DataTable DataTable { get; protected set; }
        public bool WholePeriods => TimeMode != TimeMode.YearToDate;
        public double TotalExcess { get; set; }
        public double ExcessPerYear { get; set; }
        public double RelativeExcess { get; set; }
        public double ExcessRate { get; set; }


        public virtual void Generate()
        {
            Console.WriteLine($"Generating mortality evolution");
            RetrieveLastDay();
            AdjustMinYearRegression();

            StringBuilder conditionBuilder = new StringBuilder();
            AddConditions(conditionBuilder);
            string query = string.Format(GetQueryTemplate(), conditionBuilder, SqlTimeField, TimeField);
            DataTable = GetDataTable(query);
            if (TimeMode == TimeMode.DeltaYear)
                DataTable.Rows.Remove(DataTable.Rows[0]);
            if (WholePeriods)
                DataTable.Rows.Remove(DataTable.Rows[DataTable.Rows.Count - 1]);
            if (WholePeriods)
                foreach (DataRow dataRow in DataTable.Rows)
                {
                    double days = GetPeriodLength(dataRow);
                    dataRow[1] = Convert.ToDouble(dataRow[1]) * StandardizedPeriodLength / days; // Standardize according to period length
                }
            CleanDataTable();
            Projection.BuildProjection(DataTable, MinYearRegression, MaxYearRegression, PeriodsInYear);
            BuildExcessHistogram();
            if (DisplayInjections)
                BuildVaccinationStatistics();
            MinMax();
            DataRow[] dataRows = DataTable.AsEnumerable().Where(r => ToYear(r.Field<object>(TimeField)) >= ToYear(ExcessSince)).ToArray();
            double periodLength = dataRows.Length / PeriodsInYear;
            TotalExcess = dataRows.Select(r => r.Field<double>("Excess")).Sum();
            ExcessPerYear = TotalExcess / periodLength;
            ExcessRate = dataRows.Select(r => r.Field<double>("Excess")).Average() / Population * PeriodsInYear;
            RelativeExcess = TotalExcess / dataRows.Select(r => r.Field<double>("Standardized")).Sum();
        }

        private DataTable GetDataTable(string query)
        {
            if (TimeMode != TimeMode.Month)
                return DatabaseEngine.GetDataTable(query);

            // Force time field to DateTime
            DataTable dataTable = new DataTable();
            dataTable.Columns.Add(TimeField, typeof(DateTime));
            DatabaseEngine.FillDataTable(query, dataTable);

            return dataTable;
        }

        protected void RetrieveLastDay()
        {
            LastDay = Convert.ToDateTime(DatabaseEngine.GetValue($"SELECT MAX(Date) FROM {DatabaseEngine.GetTableName(typeof(DeathStatistic))}")).AddDays(-ToDateDelay);
        }


        protected const string Query_Vaccination = @"SELECT {1}, {2} FROM VaxStatistics{0}
GROUP BY {3}
ORDER BY {3}";
        void BuildVaccinationStatistics()
        {
            StringBuilder conditionBuilder = new StringBuilder();
            AddConditions(conditionBuilder);
            string query = string.Format(Query_Vaccination, conditionBuilder, SqlTimeField, InjectionsFields, TimeField);
            DataTable vaccinationStatistics = GetDataTable(query);

            LeftJoin(DataTable, vaccinationStatistics);
        }
        protected void LeftJoin(DataTable deathStatistics, DataTable vaccinationStatistics)
        {
            foreach (VaxDose vaxDose in InjectionsDoses)
                deathStatistics.Columns.Add(new DataColumn(vaxDose.ToString(), typeof(int)) { DefaultValue = 0 });
            deathStatistics.PrimaryKey = new DataColumn[] { deathStatistics.Columns[0] };

            foreach (DataRow dataRow in vaccinationStatistics.Rows)
            {
                string filter = $"{TimeField}={TimeValueToText(dataRow[0])}";
                DataRow[] rows = deathStatistics.Select(filter);
                if (rows.Length >= 1)
                    foreach (VaxDose vaxDose in InjectionsDoses)
                        rows[0][vaxDose.ToString()] = dataRow[vaxDose.ToString()];
            }
        }

        protected virtual string TimeValueToText(object timeValue)
        {
            if (timeValue is DateTime)
                return $"#{((DateTime)timeValue).ToString(CultureInfo.InvariantCulture)}#";
            else
                return (Convert.ToDouble(timeValue)).ToString(CultureInfo.InvariantCulture);
        }

        public double MaxExcess { get; protected set; }
        public double MinExcess { get; protected set; }
        public Dictionary<VaxDose, double> MaxInjections { get; protected set; } = new Dictionary<VaxDose, double>();

        public VaxDose[] InjectionsDoses
        {
            get
            {
                if (Injections == VaxDose.All)
                    return new VaxDose[] { VaxDose.D1, VaxDose.D2, VaxDose.D3 };
                if (Injections == VaxDose.None)
                    return new VaxDose[0];
                return new VaxDose[] { Injections };
            }
        }
        string DoseField(VaxDose dose) => $"SUM({dose}) AS {dose}";

        protected string InjectionsFields
        {
            get
            {
                return Injections == VaxDose.All ? $"{DoseField(VaxDose.D1)}, {DoseField(VaxDose.D2)}, {DoseField(VaxDose.D3)}" : DoseField(Injections);
            }
        }

        private int PeriodsInYear
        {
            get
            {
                switch (TimeMode)
                {
                    case TimeMode.Month:
                        return 12;
                    case TimeMode.Quarter:
                        return 4;
                    case TimeMode.Semester:
                        return 2;
                    case TimeMode.Year:
                    case TimeMode.DeltaYear:
                    case TimeMode.YearToDate:
                        return 1;
                    default:
                        throw new ArgumentOutOfRangeException($"The time mode {TimeMode} is  not supported!");
                }
            }
        }

        protected void AddConditions(StringBuilder conditionBuilder)
        {
            if (MinAge > 0)
                AddCondition($"Age >= {MinAge}", conditionBuilder);
            if (MaxAge > 0)
                AddCondition($"Age < {MaxAge}", conditionBuilder);
            AddCondition($"Year >= {MinYearRegression}", conditionBuilder);
            AddCondition(WholePeriods ? $"Date <= '{LastDay:yyyy-MM-dd HH:mm:ss}'" : $"DayOfYear <= {LastDay.DayOfYear}", conditionBuilder);
            AddCondition(GetCountryCondition(), conditionBuilder);
            AddCondition($"Gender = {(int)GenderMode}", conditionBuilder);
        }

        public double StandardizedPeriodLength => 365.0 / PeriodsInYear;

        private int PeriodInMonths => 12 / PeriodsInYear;

        public double GetPeriodLength(object time)
        {
            DateTime periodStart;
            if (time is DateTime)
                periodStart = (DateTime)time;
            else
            {
                double period = Convert.ToDouble(time);
                int year = (int)period;
                int month = (int)((period - year) * 12) + 1;
                periodStart = new DateTime(year, month, 1);
            }
            DateTime periodEnd = periodStart.AddMonths(PeriodInMonths);
            double days = (periodEnd - periodStart).TotalDays;
            return days;
        }
        public double GetPeriodLength(DateTime periodStart, DateTime periodEnd)
        {

            double days = (periodEnd - periodStart).TotalDays;
            return days;
        }

        protected void AddCondition(string condition, StringBuilder conditionsBuilder)
        {
            if (string.IsNullOrWhiteSpace(condition))
                return;

            conditionsBuilder.Append(conditionsBuilder.Length > 0 ? " AND " : " WHERE ");
            conditionsBuilder.Append(condition);
        }

        public string TimeField
        {
            get
            {
                switch (TimeMode)
                {
                    case TimeMode.DeltaYear: return nameof(DeathStatistic.DeltaYear);
                    case TimeMode.Semester: return nameof(DeathStatistic.Semester);
                    case TimeMode.Quarter: return nameof(DeathStatistic.Quarter);
                    case TimeMode.Month: return nameof(TimeMode.Month);
                    case TimeMode.YearToDate:
                    case TimeMode.Year: return nameof(DeathStatistic.Year);
                    case TimeMode.Week: return nameof(DeathStatistic.Week);
                    case TimeMode.Day: return nameof(DeathStatistic.Date);
                    default: throw new ArgumentOutOfRangeException($"The time mode {TimeMode} is  not supported!");
                }
            }
        }
        public string SqlTimeField
        {
            get
            {
                if (TimeMode == TimeMode.Month)
                    return $"strftime('%Y-%m-1 00:00:00', {nameof(DeathStatistic.Date)}) AS {TimeField}";
                else
                    return TimeField;
            }
        }

        public static double GetHistogramResolution(double rangeLength, int approximateBarCount, bool allowLessThanOne = false)
        {
            if (rangeLength == 0.0)
                return 1.0;
            double histogramResolution = rangeLength / approximateBarCount;
            if (!allowLessThanOne && histogramResolution < 1.0)
                return 1.0; //  Less than 1 range length not supported for now
            double logResolution = Math.Log10(histogramResolution);
            double log10 = Math.Floor(logResolution);
            double restLogResolution = logResolution - log10;
            double baseResolution = Math.Pow(10, log10);
            if (restLogResolution < Math.Log10(2))
                histogramResolution = baseResolution;
            else if (restLogResolution < Math.Log10(5))
                histogramResolution = baseResolution * 2;
            else
                histogramResolution = baseResolution * 5;
            return histogramResolution;
        }
        public DataTable ExcessHistogram { get; private set; }
        public double StatisticalStandardDeviation { get; private set; }
        public double StandardDeviation { get; private set; }
        public int Population
        {
            get
            {
                string sqlCommand = $"SELECT SUM(Population) FROM AgeStructure WHERE Year = {AgeStructure.ReferenceYear} AND Gender = {(int)GenderMode}";
                string countryCondition = GetCountryCondition();
                if (!string.IsNullOrEmpty(countryCondition))
                    sqlCommand += $" AND {countryCondition}";
                if (MinAge >= 0)
                    sqlCommand += $" AND Age >= {MinAge}";
                if (MaxAge >= 0)
                    sqlCommand += $" AND Age < {MaxAge}";
                return Convert.ToInt32(DatabaseEngine.GetValue(sqlCommand));
            }
        }


        public double DeathRate { get; set; }

        void BuildExcessHistogram()
        {
            var values = DataTable.AsEnumerable().Where(r => ToYear(r.Field<object>(TimeField)) > MinYearRegression).Select(r => r.Field<double>("Excess")).ToArray();
            double max = values.Max();
            double min = values.Min();
            double resolution = GetHistogramResolution(max - min, 15);
            double upperBound = (Math.Floor(max / resolution) * resolution);
            double lowerBound = (Math.Floor(min / resolution) * resolution);
            int bars = (int)((upperBound - lowerBound) / resolution) + 1;
            int[] frequencies = new int[bars];
            foreach (double item in values)
            {
                int iBar = (int)((item - lowerBound) / resolution);
                frequencies[iBar]++;
            }
            ExcessHistogram = new DataTable();
            ExcessHistogram.Columns.Add("Excess", typeof(double));
            ExcessHistogram.Columns.Add("Frequency", typeof(double));
            ExcessHistogram.Columns.Add("Normal", typeof(double));
            double[] standardizedDeaths = DataTable.AsEnumerable().Where(r => ToYear(r.Field<object>(TimeField)) > MinYearRegression).Select(r => r.Field<double>("Standardized")).ToArray();
            double averageDeaths = standardizedDeaths.Average();
            DeathRate = averageDeaths / Population * PeriodsInYear;
            StatisticalStandardDeviation = Math.Sqrt(DeathRate * (1 - DeathRate) * Population);
            double[] standardizedDeathsInRegression = DataTable.AsEnumerable().Where(r => ToYear(r.Field<object>(TimeField)) > MinYearRegression && ToYear(r.Field<object>(TimeField)) < MaxYearRegression).Select(r => r.Field<double>("Standardized")).ToArray();
            StandardDeviation = Math.Sqrt(standardizedDeathsInRegression.Average(z => z * z) - Math.Pow(standardizedDeathsInRegression.Average(), 2));
            for (int i = 0; i < frequencies.Length; i++)
            {
                DataRow dataRow = ExcessHistogram.NewRow();
                double x = lowerBound + i * resolution;
                dataRow["Excess"] = x;
                dataRow["Frequency"] = frequencies[i];

                double y = Math.Exp(-0.5 * Math.Pow(x / StatisticalStandardDeviation, 2)) / (Math.Sqrt(2 * Math.PI) * StatisticalStandardDeviation) * resolution * values.Count();
                dataRow["Normal"] = y;
                ExcessHistogram.Rows.Add(dataRow);
            }
        }

        double ToYear(object time)
        {
            if (time is DateTime)
                return ((DateTime)time).Year + (((DateTime)time).Month - 1) / 12.0;
            return Convert.ToDouble(time);
        }
        protected void MinMax()
        {
            EnumerableRowCollection<double> values = DataTable.AsEnumerable().Select(r => r.Field<double>("RelativeExcess"));
            MaxExcess = values.Max();
            MinExcess = values.Min();
            foreach (VaxDose vaxDose in InjectionsDoses)
                MaxInjections[vaxDose] = DataTable.AsEnumerable().Select(r => Convert.ToDouble(r.Field<object>(vaxDose.ToString()))).Max();
        }

        protected virtual string GetQueryTemplate() => _Implementation.GetQueryTemplate();

        protected virtual void CleanDataTable() => _Implementation.CleanDataTable(DataTable);

        protected virtual void AdjustMinYearRegression() => _Implementation.AdjustMinYearRegression(GetCountryCondition());

        protected virtual double GetPeriodLength(DataRow dataRow) => _Implementation.GetPeriodLength(dataRow);

        protected virtual string GetCountryCondition() => _Implementation.GetCountryCondition();
        public virtual string CountryName => _Implementation.GetCountryDisplayName();
        public virtual string CountryCode => _Implementation.GetCountryInternalName();
        protected SpecificImplementation _Implementation;
        public SpecificImplementation Implementation => _Implementation;

    }
    public enum TimeMode { Year, DeltaYear, Semester, Quarter, Month, YearToDate, Week, Day }
    public enum VaxDose { None, D1, D2, D3, All}
}
