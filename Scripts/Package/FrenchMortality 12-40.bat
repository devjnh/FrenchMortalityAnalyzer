SET MinAge=12
SET MaxAge=40
SET OutputFile="FrenchMortality %MinAge%-%MaxAge%.xlsx"
FrenchMortalityAnalyzer.exe evolution --OutputFile %OutputFile% --MinAge %MinAge% --MaxAge %MaxAge% --Injections --TimeMode Semester
FrenchMortalityAnalyzer.exe evolution --OutputFile %OutputFile% --MinAge %MinAge% --MaxAge %MaxAge% --Injections --TimeMode Quarter
FrenchMortalityAnalyzer.exe evolution --OutputFile %OutputFile% --MinAge %MinAge% --MaxAge %MaxAge% --Injections --TimeMode Week --RollingPeriod 8
FrenchMortalityAnalyzer.exe show --OutputFile %OutputFile%
PAUSE