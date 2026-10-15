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

**Age structure**: comes from a single INSEE file, `3_Pop1janv_age.xlsx` (from the "Bilan démographique" tables). It has one sheet per year, and `AgeStructureLoader` reads the years from 2000 until a sheet is missing. It uses columns C–E (metropolitan France: total, men, women), which matches the deaths excluding overseas departments. The table is cached in the DB and only re-extracted when a new file is downloaded; re-extraction then triggers a `DeathStatistics` rebuild. INSEE publishes a new edition every January under a new page id, so the URL in `AgeStructureDownloader` has to be updated yearly. Because files are skipped when one with the same name already exists locally, the old file must be removed from `Data/AgeStructure` for the new one to download.

**External URLs** are hard-coded:
- The death log list comes from the data.gouv.fr API (`https://www.data.gouv.fr/api/1/datasets/fichier-des-personnes-decedees/`, in `LogFileDownloader`), and the URLs are extracted with a regex. The HTML dataset page is not usable: it only embeds recent resources, and their URLs are JSON-escaped.
- The age-structure URL is in `AgeStructureDownloader`.
- The vaccination CSV URL is in `FrenchCovidVaxData`.

When the upstream sources change, update these (see recent commits).
