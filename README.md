# FrenchMortalityAnalyzer

FrenchMortalityAnalyzer builds mortality statistics for France from the death logs published by the INSEE. It produces Excel spreadsheets with charts. They show the evolution of age-standardized mortality, the excess mortality compared with a projection based on the previous years, and the Z-score of each period.

## How it works

- **Data:** the program uses the deaths in metropolitan France since 2010. Deaths in the overseas departments are excluded.
- **Age standardization:** each day and each age, the number of deaths is rescaled to the age structure of a reference year (2022). This makes periods with different population sizes and ages comparable.
- **Projection:** a linear regression over the standardized mortality of a reference period (2012–2020 by default) gives the expected mortality of every period.
- **Excess mortality:** the difference between the observed and the expected mortality. It is totalled since a given date (2021-07-01 by default).
- **Z-score:** the deviation of each period from the projection, measured in standard deviations.

## How to use the executable

You can download the already built executable for Windows with the following link:  
[Download FrenchMortality.zip](https://github.com/devjnh/FrenchMortalityAnalyzer/releases/latest/download/FrenchMortality.zip)

Extract all the files of the zip file into a new folder. You can then launch the batch file *FrenchMortality.bat*, which runs the executable with a few command line examples.

The first execution will automatically:

- Create a subfolder named *Data* in the working folder, where all the files are stored
- Download the death log files from [data.gouv.fr](https://www.data.gouv.fr/datasets/fichier-des-personnes-decedees)
- Download the age structure (population by age on January 1st of each year) from the [INSEE web site](https://www.insee.fr/fr/statistiques/8721185?sommaire=8721209)
- Insert all the death entries and the age structure in a SQLite database named *FrenchMortality.db*
- Calculate the daily death statistics by age, standardized according to the age structure

The first execution takes a while, because it downloads all the data, inserts it in the database and builds the death statistics. Please be patient.

At every following execution, the program first downloads and inserts the death logs published since the last run, and the statistics are updated. It then generates an Excel spreadsheet named *FrenchMortality.xlsx* according to the options given on the command line. Each command adds or replaces one worksheet in the spreadsheet.

The example batch file generates the French mortality by year for all ages, then the mortality by semester for the ages from 5 to 40, and finally displays the spreadsheet:

    .\Bin\FrenchMortalityAnalyzer.exe evolution
    .\Bin\FrenchMortalityAnalyzer.exe evolution --MinAge 5 --MaxAge 40 --TimeMode Semester
    .\Bin\FrenchMortalityAnalyzer.exe show

Launched without arguments, the executable generates the mortality evolution for all the time modes and displays the spreadsheet (the same as `fullevolution --Show`).

### Commands

| Command | Description |
|---|---|
| `evolution` | Mortality evolution for one time mode (by year by default) |
| `fullevolution` | Mortality evolution for all the time modes, in a single spreadsheet |
| `init` | Only download the data and update the database |
| `show` | Open the Excel spreadsheet |
| `help` | List the commands, or the options of a command with `help <command>` |

### Main options

| Option | Description | Default |
|---|---|---|
| `-m`, `--TimeMode` | `Year`, `DeltaYear`, `Semester`, `Quarter`, `Month`, `YearToDate`, `Week` or `Day` | `Year` |
| `--MinAge`, `--MaxAge` | Age range included in the analysis | All ages |
| `-g`, `--Gender` | `All`, `Male` or `Female` | `All` |
| `--MinYearRegression`, `--MaxYearRegression` | Years used for the linear regression of the projection | 2012–2020 |
| `--ExcessSince` | Start date of the excess mortality total | 2021-07-01 |
| `--RollingPeriod` | Number of periods of the rolling average (`Week` and `Day` modes) | 8 |
| `--ZoomMinDate`, `--ZoomMaxDate` | Zoomed time range of the charts (`Week` and `Day` modes) | 2020-01-01 to 2022-07-01 |
| `-r`, `--Raw` | Also display the raw (not standardized) deaths | |
| `-i`, `--Injections` | Display the Covid 19 vaccine injections | |
| `-o`, `--OutputFile` | Name of the output spreadsheet | *FrenchMortality.xlsx* |
| `-f`, `--folder` | Data folder | *Data* in the working folder |
| `--Show` | Open the spreadsheet once generated | |

For example, to get the weekly mortality of women aged 65 and over:

    FrenchMortalityAnalyzer.exe evolution --TimeMode Week --MinAge 65 --Gender Female --Show

For the complete list of options run:

    FrenchMortalityAnalyzer.exe help evolution

## How to build the executable

To build the executable yourself and review or change the code, clone this repository. Then either:

- Open the solution *FrenchMortalityAnalyzer.sln* with Visual Studio 2022 and build it. You can use the free [community edition](https://visualstudio.microsoft.com/vs/community/).
- Or, with the [.NET SDK](https://dotnet.microsoft.com/download), run `dotnet build FrenchMortalityAnalyzer.sln` from the repository folder.

The project targets both the .NET Framework 4.7.2 and .NET 6:

- The Windows executable is generated in the *FrenchMortalityAnalyzer\bin\Debug\net472* subfolder (or *bin\Release\net472* for a release build).
- The .NET 6 version is generated in the *net6.0* subfolder.

## How to distribute the executable

For Windows computers, copy the executable *FrenchMortalityAnalyzer.exe* along with all the dll files from the *net472* output folder.

For Linux or macOS you can try the .NET 6 version of the executable, but it has not been tested yet.

## Data sources

- Death logs: [Fichier des personnes décédées](https://www.data.gouv.fr/datasets/fichier-des-personnes-decedees), INSEE, on data.gouv.fr
- Age structure: [Bilan démographique](https://www.insee.fr/fr/statistiques/8721185?sommaire=8721209), INSEE, population on January 1st by gender and age
- Covid 19 vaccine injections: data.gouv.fr

## License

This project is licensed under the MIT License. See [LICENSE](LICENSE).
