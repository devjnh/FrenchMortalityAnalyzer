# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

A C# console app that downloads French death logs (INSEE, published on data.gouv.fr) and INSEE age-structure files, loads them into a SQLite database, builds age-standardized daily death statistics, and generates Excel spreadsheets with charts (via EPPlus) showing mortality evolution, excess mortality against a linear-regression baseline, and Z-scores.

## Build and run

The single project `FrenchMortalityAnalyzer/FrenchMortalityAnalyzer.csproj` multi-targets `net472` (the main Windows build) and `net6.0` (untested on Linux and macOS). There is no test project and no linter configured.

```bash
dotnet build FrenchMortalityAnalyzer.sln
dotnet run --project FrenchMortalityAnalyzer -f net6.0 -- evolution --MinAge 5 --MaxAge 40 --TimeMode Semester
dotnet run --project FrenchMortalityAnalyzer -f net6.0 -- help
```

CLI verbs (see `Options.cs`): `init`, `evolution`, `fullevolution` (the default when there are no args; it generates every time mode into one workbook), `show` (opens the xlsx), and `vaxevolution` (declared, but it has no handler in `Program.Main`'s `MapResult`). Shared options are in `Common/Options.cs`: `MortalityEvolutionBase` and `MortalityTimeEvolution`.

Runtime data goes into `./Data` under the current working directory (`--folder` to override). The first run is slow because it downloads everything and populates `Data/FrenchMortality.db`. Death log `.txt` files are moved into `Data/Done/` once inserted, so only new files get re-inserted. If any files were inserted, the `DeathStatistics` table is fully rebuilt.

## Architecture

**Pipeline (`Program.Init`)**: downloaders (`Downloaders/`) → parsers (`Parser/`) insert raw rows into SQLite → `DeathStatistics.BuildStatistics()` aggregates `Deaths` by date and age into the `DeathStatistics` table. It runs once for each gender filter (All/Male/Female) and computes `StandardizedDeaths = Deaths * RefPopulation / Population`, using `AgeStructure.ReferenceYear`. Overseas departments (97/98/99) are excluded.

**`DatabaseEngine`** is a reflection-based mini ORM. A model class (`Common/Model/*`, `Model/DeathEntry.cs`) maps to a table named `{TypeName}s` (unless a name is given explicitly). Columns come from public properties, base class first, and enums are stored as int. Typical use is `Prepare(dataTable)` → many `Insert(obj)` calls (batched by 2000 inside transactions) → `FinishInsertion()`. `Prepare` drops and recreates the table unless `deleteOld: false`. `IsBuilt` checks use `DoesTableExist(type)`.

**Analysis engines**: `MortalityEvolution` (abstract, in `Common/`) inherits from the options classes, so CLI options are copied straight onto the engine via `CopyTo`. `Generate()` builds a SQL query from a template and runs it into a `DataTable`. It then normalizes values by period length, builds the regression projection (`Projection`/`Regression`, fitted between `MinYearRegression` and `MaxYearRegression`), computes excess, Z-scores and optional vaccination data, and finally computes totals since `ExcessSince`.
- `TimeMode <= Month` (Year, DeltaYear, Semester, Quarter, Month) uses `MortalityEvolution` with `MortalityEvolutionView`.
- `YearToDate`, `Week` and `Day` use `RollingEvolution` (rolling average over `RollingPeriod`) with `RollingEvolutionView`. The ordering of the `TimeMode` enum matters for this check.

**Country abstraction**: the code under `Common/` is country-agnostic. Country-specific behaviour (the query template, the country code and condition, data cleanup) is injected through a `SpecificImplementation` subclass. Here that is `FrenchImplementation`, wired up by `FrenchMortalityEvolution` and `FrenchRollingEvolution`. Keep France-specific logic out of `Common/`.

**Views** (`Common/Views/`) write one worksheet per run, plus charts, into the output xlsx using EPPlus 6 (license context `NonCommercial`, set in `BaseEvolutionView`'s static constructor). The sheet name is built from the country, time mode, age range, gender and injections.

**External URLs** are hard-coded: the death log list is scraped by regex from the data.gouv.fr dataset page (`LogFileDownloader`), the age-structure xlsx URLs are listed in `AgeStructureDownloader`, and the vaccination CSV URL is in `FrenchCovidVaxData`. When the upstream sources change, update these (see recent commits).
