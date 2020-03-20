# FAIMS MzXML Generator

## Overview

The FAIMS MzXML Generator converts a Thermo .raw file with FAIMS scans into a series of .mzXML
files, creating one .mzXML file for each FAIMS compensation voltage (CV) value in
the .raw file.

### Background

MaxQuant currently does not process FAIMS data correctly if multiple correction voltages are used through the experiment's duration. The FAIMS MzXML Generator was developed as a workaround to this issue by splitting a FAIMS Raw file into a set of MaxQuant compliant .mzXML files, each containing only scans collected using a single correction voltage. These resulting .mzXML files can then be processed via MaxQuant as usual.

## Getting Started

Written in C#, the FAIMS MzXML Generator accepts Thermo .raw files collected from FAIMS experiments. To begin using the application, download the latest release from the 
[FAIMS-MzXML-Generator Release Page](https://github.com/PNNL-Comp-Mass-Spec/FAIMS-MzXML-Generator/releases) on GitHub and decompress the .zip file.

There are two executables available, a GUI and a command line interface.  Both use the ThermoFisher.CommonCore DLLs, so you do not need to have MSFileReader or Xcalibur installed.

### GUI

File `FAIMS_MzXML_Generator.exe` provides a Graphical User Interface (GUI) for converting .raw files.  Select your input files, optionally define the output directory, then click `Create MzXMLs`
* Status messages are shown while the mzXML files are created

### Console Program
File `WriteFaimsXMLFromRawFile.exe` provides a command line interface (CLI) console application.
* The CLI works on both Windows and on Linux
  * To invoke on Linux, use [Mono](https://www.mono-project.com/]):
  * `mono WriteFaimsXMLFromRawFile.exe Datafile.raw`

### Command Line Interface Syntax

```
WriteFaimsXMLFromRawFile.exe InstrumentFile.raw [Output_Directory_Path]
```

The first command line argument specifies the .raw file to convert
* It can be a filename, or a full path to a file
* The name supports wildcards, for example `*.raw`

The second command line argument defines the output directory path
* This parameter is optional

## Known Issues

### The program didn't return any MzXMLs from my non-FAIMS .raw file

This tool was specifically developed to split FAIMS .raw files into MzXMLs. 
* If your file doesn't contain compensation voltages, the FAIMS MzXML generator will not create any MzXMLs, by design
* Instead, use MsConvert, which is available with [ProteoWizard](http://proteowizard.sourceforge.net/download.html)

## Contacts

Written by Dain Brademan for the Joshua Coon Research Group (University of Wisconsin) in 2018\
Converted to use ThermoFisher.CommonCore DLLs by Matthew Monroe for PNNL (Richland, WA) in 2020\
E-mail: brademan@wisc.edu or matthew.monroe@pnnl.gov or proteomics@pnnl.gov\
Website: https://github.com/PNNL-Comp-Mass-Spec/FAIMS-MzXML-Generator/releases or https://github.com/coongroup/FAIMS-MzXML-Generator

## License

The FAIMS MzXML Generator is licensed under the MIT license; 
you may not use this file except in compliance with the License. 
You may obtain a copy of the License at https://opensource.org/licenses/MIT

Copyright (c) 2018 Coon Group

RawFileReader reading tool. Copyright © 2016 by Thermo Fisher Scientific, Inc. All rights reserved.
