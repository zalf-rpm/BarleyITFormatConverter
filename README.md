# BarleyITFormatConverter
Convert file from BarleyIT DB in ICASA standard to monica input files

This is a commandline tool to convert a .json file, exported from BarleyIT DB (https://app.cybeletech.com/barleyit)
The input file uses ICASA standard to define parameters. (See
https://docs.google.com/spreadsheets/d/1MYx1ukUsCAM1pcixbVQSu49NU-LfXg-Dtt-ncLBzGAM/pub?output=html# )

Executable:

AgMIPToMonicaConverter.exe 

Parameter:

-filename <path + filename>  --- input file path

-out <path>                  --- (optional) output path folder - default = %userprofile%/documents/AgMIPToMonicaOut


Dependency:

Converter uses Newtonsoft.Json.dll

To compile it you need NuGet package Newtonsoft.Json

What you get:

Three output files will be generated into the output folder. 
Filenames are hardcoded.

climate-min.csv

site-min.json

crop-min.json

Note:

"include-file-base-path" in sim.json must be configured to find the includes from monica-parameters project  
