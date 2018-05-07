# FAIMS MzXML Generator

## Getting Started

MaxQuant currently does not process FAIMS data correctly if multiple correction voltages are used through the experiment's duration. The FAIMS MzXML Generator was developed as a workaround to this issue by splitting a FAIMS Raw file into a set of MaxQuant compliant MzXML files, each containing only scans collected using a single correction voltage. These resulting MzXML files can then be processed via MaxQuant as usual.

Written in C#, the FAIMS MzXML Generator accepts Thermo .raw files collected from FAIMS experiments. To begin using the application, download the repository and extract it from the resulting zipped folder. 

The executable *FAIMS MzXML Generator.exe* is used to launch the application. 

### Prerequisites

- A Windows-based operating system.<br>
- A MSFileReader or Xcalibur install. 

### Known Issues

#### Unhandled Exception: System.Runtime.InteropServices.COMException
<h1 align="center">
  <a><img src="https://github.com/coongroup/FAIMS-MzXML-Generator/blob/master/Images/XRawFileNotRegistered.png" width="500"></a>
</h1>

This particular error is thrown when the 32-bit version of XRawFile2.dll from either MsFileReader or Thermo Foundation is not installed, or the dependency is not correctly registered with Windows.

This error can be resolved by installing the 32-bit version of MsFileReader or Xcalibur if it is not already installed or manually registering the required dependencies using Windows Command Prompt/Microsoft Register Server the using the following directions if XRawFile.dll already exists on your system.


1. Find the paths to the following locations/files on your computer
```
// Path to the SysWOW64 folder
C:\Windows\SysWOW64

// Path to the 32-bit XRawFile2.dll. This should be in the Program Files (x86) folder
C:\Program Files (x86)\Thermo\MSFileReader\XRawfile2.dll
```
2. Open command prompt as an administrator
3. Navigate to the SysWoW64 directory using the following command
```
\\ Example
\\ The exact file path may change depending on your computer's configuration

cd C:\Windows\SysWOW64
```
4. Run the Microsoft Register Server executable with the XRawFile2.dll as an argument
```
\\ Example
\\ The exact file path to XRawFile2.dlll may change depending on your computer's configuration

regsvr32.exe C:\Program Files (x86)\Thermo\MSFileReader\XRawfile2.dll
```

The 32-bit version of XRawFile2.dll should now be correct registered and the COMException error should no longer occur.

#### The program didn't return any MzXMLs from my non-FAIMS .raw file
This tool was specifically developed to split FAIMS .raw files into MzXMLs. If your file doesn't contain correction voltages, the FAIMS MzXML generator will not create any MzXMLs by design. Other converters such as ReAdW or MsConvert can already have this functionality.

If you encounter any other issues with this software tool please contact me at brademan@wisc.edu.
