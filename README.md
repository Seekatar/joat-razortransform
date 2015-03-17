# What is Artie? #
Artie is long for RT which is short for RazorTransform.  Artie uses XML files to describe an object that is editable in Artie's UI.  It will then use template files to generates output from them using values in the object using the Razor view model introduced in ASP.NET 4.0.  Generated files can be ASP.NET web pages, XML, or any other text file, such as a PowerShell script.

![https://joat-razortransform.googlecode.com/git-history/V2/Doc/MainScreenSample1.png](https://joat-razortransform.googlecode.com/git-history/V2/Doc/MainScreenSample1.png)

The need for Artie arose after painfully watching the IT department do a deployment of our product that required editing a myriad of XML and PowerShell files.  Many of the files were very similar, and much of the data between them was redundant creating a very tedious and error-prone process just to create the files needed to begin the deploy.

Artie solves that problem by having graphical user interface for easily entering in all the values once, in one place, then using templates with embedded C# code to generate the output files.  The values are stored so they can be easily edited later for another deploy, or copied to use as the basis of another deploy.

## Features ##
  * GUI for editing an object defined in XML
  * All typical data types supported, such as strings, numbers, file paths, hyperlinks, etc.
  * Custom types supported.  An example custom type of Color is in the source code.
  * Enumerations supported
  * Simple range validation
  * Regular expression validation

# Now with PowerShell #
The -run option now will run a set of PowerShell scripts using the [RtPsHost](https://code.google.com/p/joat-rtpshost/) library.

# .NET 4.5 Required #
My apologies if you don't have it installed.  I use the new async feature quite a bit in Artie, which is in 4.5
