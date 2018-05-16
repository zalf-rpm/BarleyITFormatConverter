# BarleyITFormatConverter
Convert file from BarleyIT DB in ICASA standard to monica input files

This is a commandline tool to convert a .json file, exported from BarleyIT DB (https://app.cybeletech.com/barleyit)
The input file uses ICASA standard to define parameters. (See
https://docs.google.com/spreadsheets/d/1MYx1ukUsCAM1pcixbVQSu49NU-LfXg-Dtt-ncLBzGAM/pub?output=html# )

Executable:
AgMIPToMonicaConverter.exe 

Parameter:
-filename <path + filename>  --- input file path
-out <path>                  --- output path folder

Dependency:
Converter uses Newtonsoft.Json.dll

To compile it you need NuGet package Newtonsoft.Json
