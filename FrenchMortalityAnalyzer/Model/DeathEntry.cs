﻿using MortalityAnalyzer.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MortalityAnalyzer
{
    public class DeathEntry
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime BirthDate { get; set; }
        public DateTime DeathDate { get; set; }
        public int? Age
        {
            get
            {
                int? ageInDays = AgeInDays;
                return ageInDays != null ? (int)(ageInDays.Value / 365.25) : null;
            }
        }
        public int? AgeInDays
        {
            get
            {
                if (BirthDate != DateTime.MinValue)
                    return (int)((DeathDate - BirthDate).TotalDays);
                else
                    return null;
            }
        }
        public Gender Gender { get; set; }
        public string BirthPlace { get; set; }
        public string BirthCountry { get; set; }
        public string BirthPlaceId { get; set; }
        public string DeathPlaceId { get; set; }
        public string BirthDepartement { get; set; }
        public string DeathDepartement { get; set; }
        public string DeathId { get; set; }
        public string LogFile { get; set; }
        static Regex _NameRegex = new Regex(@"([^*]*)\*([^/]*)/", RegexOptions.Compiled);
        static Regex _BirthRegex = new Regex(@"(\d)(\d{4})(\d{2})(\d{2})(.{5})", RegexOptions.Compiled);
        static Regex _DeathRegex = new Regex(@"(\d{4})(\d{2})(\d{2})(.{5})(.{9})", RegexOptions.Compiled);
        public bool Parse(string line)
        {
            DateTime birthDate = DateTime.MinValue;

            string name = line.Substring(0, 80);
            Match match = _NameRegex.Match(name);
            if (match.Success)
            {
                LastName = match.Groups[1].Value;
                FirstName = match.Groups[2].Value;
            }
            else
            {
                Trace.WriteLine($"Invalid INSEE log line:\n{line}");
                return false;
            }

            string birth = line.Substring(80, 14);
            match = _BirthRegex.Match(birth);
            if (match.Success)
            {
                Gender = (Gender)int.Parse(match.Groups[1].Value);
                BirthDate = GetDate(match, 2);
                BirthDepartement = match.Groups[5].Value.Substring(0, 2);
                BirthPlaceId = match.Groups[5].Value;
            }
            else
            {
                Gender = Gender.Unknown;
                BirthDepartement = null;
                BirthPlaceId = null;
                Trace.WriteLine($"Invalid INSEE log line (birth info):\n{line}");
            }

            BirthPlace = line.Substring(94, 30).Trim();
            BirthCountry = line.Substring(124, 30).Trim();

            string death = line.Substring(154);
            match = _DeathRegex.Match(death);
            if (match.Success)
            {
                DeathDate = GetDate(match, 1);

                DeathDepartement = match.Groups[4].Value.Substring(0, 2);
                DeathPlaceId = match.Groups[4].Value;
                DeathId = match.Groups[5].Value.Trim();
            }
            else
                return false;

            return true;
        }
        private DateTime GetDate(Match match, int iDate)
        {
            int year = int.Parse(match.Groups[iDate].Value);
            if (year == 0)
                return DateTime.MinValue;
            int month = int.Parse(match.Groups[iDate + 1].Value);
            var day = int.Parse(match.Groups[iDate + 2].Value);
            if (month == 0)
                month = 1;
            if (day == 0)
                day = 1;
            try
            {
                DateTime dateTime = new DateTime(year, month, day);
                return dateTime;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex);
            }

            return DateTime.MinValue;
        }
    }
    public enum Gender { Unknown = 0, Male = 1, Female = 2 }
}
