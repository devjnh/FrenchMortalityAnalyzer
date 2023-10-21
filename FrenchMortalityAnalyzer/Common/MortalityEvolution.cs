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
        public bool DisplayInjections => Injections != VaxDose.None;


        public virtual void Generate()
        {
            if (WholePeriods)
                LastDay = DateTime.MaxValue;
            else
                LastDay = Convert.ToDateTime(DatabaseEngine.GetValue($"SELECT MAX(Date) FROM {DeathStatistic.StatisticsTableName}")).AddDays(-ToDateDelay);

            AdjustMinYearRegression();

            string toDate = WholePeriods ? "" : " to date";
            Console.WriteLine($"Generating mortality evolution");
            StringBuilder conditionBuilder = new StringBuilder();
            AddConditions(conditionBuilder);
            string query = string.Format(GetQueryTemplate(), conditionBuilder, TimeField, GenderTablePostFix);
            DataTable = DatabaseEngine.GetDataTable(query);
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
        }

        protected string GenderTablePostFix => GenderMode != GenderFilter.All ? $"_{GenderMode}" : string.Empty;

        protected const string Query_Vaccination = @"SELECT {1}, SUM({2}) AS Injections FROM VaxStatistics{0}
GROUP BY {1}
ORDER BY {1}";
        void BuildVaccinationStatistics()
        {
            StringBuilder conditionBuilder = new StringBuilder();
            AddConditions(conditionBuilder);
            string query = string.Format(Query_Vaccination, conditionBuilder, TimeField, InjectionsField);
            DataTable vaccinationStatistics = DatabaseEngine.GetDataTable(query);

            LeftJoin(DataTable, vaccinationStatistics);
        }
        protected void LeftJoin(DataTable deathStatistics, DataTable vaccinationStatistics)
        {
            DataColumn injectionsColumn = new DataColumn("Injections", typeof(int)) { DefaultValue = 0 };
            deathStatistics.Columns.Add(injectionsColumn);
            deathStatistics.PrimaryKey = new DataColumn[] { deathStatistics.Columns[0] };

            foreach (DataRow dataRow in vaccinationStatistics.Rows)
            {
                string filter = $"{TimeField}={TimeValueToText(dataRow[0])}";
                DataRow[] rows = deathStatistics.Select(filter);
                if (rows.Length >= 1)
                    rows[0][injectionsColumn] = dataRow[injectionsColumn.ColumnName];
            }
        }

        protected virtual string TimeValueToText(object timeValue)
        {
            return (Convert.ToDouble(timeValue)).ToString(CultureInfo.InvariantCulture);
        }

        public double MaxExcess { get; protected set; }
        public double MinExcess { get; protected set; }
        public double MaxInjections { get; protected set; } = 0;

        protected string InjectionsField => Injections == VaxDose.All ? $"{VaxDose.D1} + {VaxDose.D2} + {VaxDose.D3}" : Injections.ToString();

        private int PeriodsInYear
        {
            get
            {
                switch (TimeMode)
                {
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
            if (!WholePeriods)
                AddCondition($"DayOfYear <= {LastDay.DayOfYear}", conditionBuilder);
            if (MinAge > 0)
                AddCondition($"Age >= {MinAge}", conditionBuilder);
            if (MaxAge > 0)
                AddCondition($"Age < {MaxAge}", conditionBuilder);
            AddCondition($"Year >= {MinYearRegression}", conditionBuilder);
            AddCondition(GetCountryCondition(), conditionBuilder);
        }

        public double StandardizedPeriodLength => 365 * PeriodInFractionOfYear;

        private double PeriodInFractionOfYear
        {
            get
            {
                return TimeMode switch
                {
                    TimeMode.Semester => 0.5,
                    TimeMode.Quarter => 0.25,
                    _ => 1.0,
                };
            }
        }

        private int PeriodInMonths
        {
            get
            {
                return TimeMode switch
                {
                    TimeMode.Semester => 6,
                    TimeMode.Quarter => 3,
                    _ => 12
                };
            }
        }

        public double GetPeriodLength(double period)
        {
            int year = (int)period;
            int month = TimeMode == TimeMode.DeltaYear ? 7 : (int)((period - year) * 12) + 1;
            DateTime periodStart = new DateTime(year, month, 1);
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
                    case TimeMode.YearToDate:
                    case TimeMode.Year: return nameof(DeathStatistic.Year);
                    case TimeMode.Week: return nameof(DeathStatistic.Week);
                    case TimeMode.Day: return nameof(DeathStatistic.Date);
                    default: throw new ArgumentOutOfRangeException($"The time mode {TimeMode} is  not supported!");
                }
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


        double DeathRate { get; set; }

        void BuildExcessHistogram()
        {
            var values = DataTable.AsEnumerable().Where(r => Convert.ToDouble(r.Field<object>(TimeField)) > MinYearRegression).Select(r => r.Field<double>("Excess")).ToArray();
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
            double[] standardizedDeaths = DataTable.AsEnumerable().Where(r => Convert.ToDouble(r.Field<object>(TimeField)) > MinYearRegression).Select(r => r.Field<double>("Standardized")).ToArray();
            double averageDeaths = standardizedDeaths.Average();
            DeathRate = averageDeaths / Population;
            StandardDeviation = Math.Sqrt(DeathRate * (1 - DeathRate) * Population);
            double sum = standardizedDeaths.Sum();
            for (int i = 0; i < frequencies.Length; i++)
            {
                DataRow dataRow = ExcessHistogram.NewRow();
                double x = lowerBound + i * resolution;
                dataRow["Excess"] = x;
                dataRow["Frequency"] = frequencies[i];

                double y = Math.Exp(-0.5 * Math.Pow(x / StandardDeviation, 2)) / (Math.Sqrt(2 * Math.PI) * StandardDeviation) * resolution * values.Count();
                dataRow["Normal"] = y;
                ExcessHistogram.Rows.Add(dataRow);
            }
        }
        protected void MinMax()
        {
            EnumerableRowCollection<double> values = DataTable.AsEnumerable().Select(r => r.Field<double>("RelativeExcess"));
            MaxExcess = values.Max();
            MinExcess = values.Min();
            if (DisplayInjections)
                MaxInjections = DataTable.AsEnumerable().Select(r => Convert.ToDouble(r.Field<object>("Injections"))).Max();
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
    public enum TimeMode { Year, DeltaYear, Semester, Quarter, YearToDate, Week, Day }
    public enum VaxDose { None, D1, D2, D3, All}
}
