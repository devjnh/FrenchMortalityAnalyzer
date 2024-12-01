using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MortalityAnalyzer;
using MortalityAnalyzer.Views;
using OfficeOpenXml;
using CommandLine;
using System.Diagnostics;
using MortalityAnalyzer.Parser;

class Program
{
    static int Main(string[] args)
    {
        if (args.Length == 0)
            return FullMortalityEvolution(new FullMortalityEvolutionOptions { Show = true});

        return Parser.Default.ParseArguments<MortalityEvolutionOptions, FullMortalityEvolutionOptions, VaccinationEvolutionOptions, InitOptions, ShowOptions>(args)
            .MapResult(
              (MortalityEvolutionOptions opts) => MortalityEvolution(opts),
              (FullMortalityEvolutionOptions opts) => FullMortalityEvolution(opts),
              (InitOptions opts) => Init(opts),
              (ShowOptions opts) => Show(opts),
              errs => 1);
    }
    static int FullMortalityEvolution(FullMortalityEvolutionOptions mortalityEvolutionOptions)
    {
        Init(mortalityEvolutionOptions);
        DatabaseEngine databaseEngine = GetDatabaseEngine(mortalityEvolutionOptions.Folder);
        GenerateTimeMode(databaseEngine, mortalityEvolutionOptions, TimeMode.Year);
        GenerateTimeMode(databaseEngine, mortalityEvolutionOptions, TimeMode.DeltaYear);
        GenerateTimeMode(databaseEngine, mortalityEvolutionOptions, TimeMode.Semester);
        GenerateTimeMode(databaseEngine, mortalityEvolutionOptions, TimeMode.Quarter);
        GenerateTimeMode(databaseEngine, mortalityEvolutionOptions, TimeMode.Month);
        GenerateTimeMode(databaseEngine, mortalityEvolutionOptions, TimeMode.Week, 8);
        GenerateTimeMode(databaseEngine, mortalityEvolutionOptions, TimeMode.Week, 4);

        if (mortalityEvolutionOptions.Show)
            Show(mortalityEvolutionOptions);
        return 0;
    }
    static void GenerateTimeMode(DatabaseEngine databaseEngine, FullMortalityEvolutionOptions mortalityEvolutionOptions, TimeMode timeMode, int rollingPeriod = 7)
    {
        MortalityEvolution engine = mortalityEvolutionOptions.GetEvolutionEngine(timeMode, rollingPeriod);
        engine.DatabaseEngine = databaseEngine;
        engine.Generate();
        BaseEvolutionView mortalityEvolutionView = engine.TimeMode <= TimeMode.Month ? new MortalityEvolutionView() : new RollingEvolutionView();
        mortalityEvolutionView.MortalityEvolution = engine;
        mortalityEvolutionView.Save();
    }
    static int MortalityEvolution(MortalityEvolutionOptions mortalityEvolutionOptions)
    {
        MortalityEvolution mortalityEvolution = mortalityEvolutionOptions.GetEvolutionEngine();
        Init(mortalityEvolution);
        using (DatabaseEngine databaseEngine = GetDatabaseEngine(mortalityEvolution.Folder))
        {
            mortalityEvolution.DatabaseEngine = databaseEngine;
            mortalityEvolution.Generate();
            BaseEvolutionView mortalityEvolutionView = mortalityEvolutionOptions.GetView();
            mortalityEvolutionView.MortalityEvolution = mortalityEvolution;
            mortalityEvolutionView.Save();
        }
        if (mortalityEvolution.Show)
            Show(mortalityEvolution);
        return 0;
    }

    private static DatabaseEngine GetDatabaseEngine(string dataFolder)
    {
        string databaseFile = Path.Combine(dataFolder, "FrenchMortality.db");
        DatabaseEngine databaseEngine = new DatabaseEngine($"data source={databaseFile};Journal Mode=Off;Synchronous=Off", System.Data.SQLite.SQLiteFactory.Instance);
        databaseEngine.Connect();
        return databaseEngine;
    }

    private static int Init(Options initOptions)
    {
        if (!Directory.Exists(initOptions.Folder))
            Directory.CreateDirectory(initOptions.Folder);
        LogFileDownloader downloader = new LogFileDownloader(initOptions.Folder);
        downloader.DownloadMissingFiles();
        AgeStructureDownloader ageStructureDownloader = new AgeStructureDownloader(initOptions.Folder);
        ageStructureDownloader.DownloadMissingFiles();

        using (DatabaseEngine databaseEngine = GetDatabaseEngine(initOptions.Folder))
        {
            Init(initOptions.Folder, databaseEngine);
        }
        return 0;
    }
    private static void Init(string dataFolder, DatabaseEngine databaseEngine)
    {
        FrenchCovidVaxData owidCovidVaxData = new FrenchCovidVaxData { DatabaseEngine = databaseEngine };
        if (!owidCovidVaxData.IsBuilt)
            owidCovidVaxData.Extract(dataFolder);

        DeathLogs deathLogs = new DeathLogs { DatabaseEngine = databaseEngine };
        deathLogs.Extract(dataFolder);

        AgeStructureLoader ageStructureLoader = new AgeStructureLoader { DatabaseEngine = databaseEngine };
        ageStructureLoader.Load(dataFolder);
        DeathStatistics deathStatistics = new DeathStatistics { DatabaseEngine = databaseEngine, AgeStructure = ageStructureLoader.AgeStructure };
        if (deathLogs.FilesInserted || !deathStatistics.IsBuilt)
            deathStatistics.BuildStatistics();
    }
    private static int Show(Options initOptions)
    {
        string filePath = Path.Combine(initOptions.Folder, initOptions.ActualOutputFile);
        if (File.Exists(filePath))
            Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
        else
            Console.WriteLine("The file was not found!");
        return 0;
    }
}
