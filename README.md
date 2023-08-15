# FrenchMortalityAnalyzer
This program allows to generate statistics with the French death logs published by the INSEE
## How to use the executable
You can download the already built executable with the following link:  
[Download FrenchMortality.zip](https://github.com/devjnh/FrenchMortalityAnalyzer/releases/latest/download/FrenchMortality.zip)  
Extract all files in the zip file in a new folder. You can then launch the batch file *FrenchMortality.bat* that will launch the executable with a few command line examples.
The first execution will automatically:

- Create in the working folder a subfolder named *Data* were all the files will be stored
- Download the death log files from the web site data.gouv.fr
- Download the age structure files from the INSEE web site 
- Insert all the death entries in a SQLite database named *FrenchMortality.db*
- Insert the age structure of every year in the same database
- Calculate daily death statistics by age standardized according to the age structure

The first execution will take some time to download and insert all the data in the database and then build the death statistics. So, you need to be patient.

Then at every execution, the program will generate a MS Excel spreadsheet named *FrenchMortality.xlsx* according to the options specified in the command line. The example batch file will generate the French mortality by year since 2001 for all ages and the mortality by semester since 2010 for the age from 12 to 40 and then the spreadsheet is displayed.

Here is what the example batch file looks like :

    .\Bin\FrenchMortalityAnalyzer.exe evolution
    .\Bin\FrenchMortalityAnalyzer.exe evolution --MinAge 12 --MaxAge 40 --TimeMode Semester
    .\Bin\FrenchMortalityAnalyzer.exe show

You can run your own examples by changing various parameters. For more information on what you can specify as arguments launch:

    FrenchMortalityAnalyzer.exe help

## How to build the executable
If you want to build the executable by yourself and review or change the code, you need to download the code from this repository and you need to download and install Visual Studio 2022. You can use the free [community edition](https://visualstudio.microsoft.com/vs/community/).  
With Visual Studio open the solution *FrenchMortalityAnalyzer.sln* and build it.
The executable should be generated in the *FrenchMortalityAnalyzer\bin\Debug\net72* subfolder.
## How to distribute the executable
Copy the executable *FrenchMortalityAnalyzer.exe* along with all the dll files from the mentioned folder.
