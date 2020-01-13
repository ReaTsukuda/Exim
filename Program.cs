using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using OriginTablets.Types;
using Newtonsoft.Json;

namespace Exim
{
  class Program
  {
    /// <summary>
    /// Expresses the two modes that Exim can operate in.
    /// </summary>
    private enum EximModes
    {
      Export,
      Import,
      Invalid
    }

    static void Main(string[] args)
    {
      EximModes mode = CheckArgs(args);
      bool longPointers = args.Contains("-l") || args.Contains("--long");
      if (mode == EximModes.Export)
      {
        HandleExport(args[1], args[2], longPointers);
      }
      else if (mode == EximModes.Import)
      {
        HandleImport(args[1], args[2], longPointers);
      }
    }

    /// <summary>
    /// Checks if the user requested export or import mode, and that the necessary paths are present.
    /// If any of that is false, indicate what happened, print the usage message, then exit.
    /// </summary>
    /// <param name="args">The application's args array.</param>
    /// <returns>What mode the user specified: import or export.</returns>
    private static EximModes CheckArgs(string[] args)
    {
      // If the user supplied no arguments, print the full usage message, and exit.
      if (args.Length == 0)
      {
        PrintUsage(true);
        Environment.Exit(0);
      }
      // If args are not empty, but there's no proper mode argument, give an error,
      // print the shortened usage message, and then exit.
      else if (args.Contains("-e") == false
        && args.Contains("--export") == false
        && args.Contains("-i") == false
        && args.Contains("--import") == false)
      {
        Console.Error.WriteLine("No mode detected.");
        PrintUsage(false);
        Environment.Exit(0);
      }
      // If there's anything besides three or four arguments, the user forgot at least one of the paths, or has
      // some erroneous argument. In either case, give an error, print the shortened usage message, and then exit.
      else if (args.Length != 3 && args.Length != 4)
      {
        Console.Error.WriteLine("Wrong number of arguments.");
        Console.Error.WriteLine("You may be missing one or both of the paths, or have supplied extraneous arguments.");
        PrintUsage(false);
        Environment.Exit(0);
      }
      // If the input path does not exist, indicate that, then exit.
      else if (File.Exists(args[1]) == false)
      {
        Console.Error.WriteLine("The input file does not exist.");
        Environment.Exit(0);
      }
      // If the output path's directory does not exist, create it.
      string outputDirectory = Path.GetDirectoryName(args[2]);
      if (Directory.Exists(outputDirectory) == false)
      {
        Directory.CreateDirectory(outputDirectory);
        Console.WriteLine("Note: Created a directory for the output file.");
      }
      // Finally, return what mode was requested.
      if (args.Contains("-e") || args.Contains("--export")) { return EximModes.Export; }
      else if (args.Contains("-i") || args.Contains("--import")) { return EximModes.Import; }
      // This shouldn't happen.
      Environment.Exit(-1);
      return EximModes.Invalid;
    }

    /// <summary>
    /// Print usage information to the console.
    /// </summary>
    /// <param name="printBasicInfo">Whether or not to print the credit and basic description of the program.</param>
    private static void PrintUsage(bool printBasicInfo)
    {
      if (printBasicInfo == true)
      {
        Console.WriteLine("ExIm for Etrian Odyssey, by Rea");
        Console.WriteLine("For exporting text table/MBM files to JSON, and importing JSON back into those formats.");
        Console.WriteLine("");
      }
      Console.WriteLine("Usage: exim [mode] [input] [output] <-l / --long>");
      Console.WriteLine("");
      Console.WriteLine("[mode] can be:");
      Console.WriteLine("    -e, --export: Export text from a tbl/mbm file to JSON.");
      Console.WriteLine("    -i, --import: Import text from a JSON file to tbl/mbm.");
      Console.WriteLine("[input] is the path to the file you wish to import from.");
      Console.WriteLine("[output] is the path to the file you wish to export to.");
      Console.WriteLine("<-l / --long> is an optional argument, and indicates that the imported/exported tbl uses long pointers. Ignored for mbm files.");
      Console.WriteLine("");
      Console.WriteLine("Note: if [output]'s directory does not exist, it will be created.");
    }

    /// <summary>
    /// Handle exporting a tbl/mbm file to JSON.
    /// </summary>
    /// <param name="inputPath">The path to the tbl/mbm file.</param>
    /// <param name="outputPath">Where to write the JSON to.</param>
    private static void HandleExport(string inputPath, string outputPath, bool longPointers)
    {
      // Check what type of file we're importing, using its extension.
      string extension = Path.GetExtension(inputPath).ToLower();
      object importedFile = null;
      if (extension == ".mbm")
      {
        importedFile = new MBM(inputPath);
      }
      else if (extension == ".tbl")
      {
        importedFile = new Table(inputPath, longPointers);
      }
      // Serialize the file to JSON, and output it.
      File.WriteAllLines(outputPath,
        new string[] { importedFile.GetType().ToString(), JsonConvert.SerializeObject(importedFile, Formatting.Indented) }
      );
    }

    /// <summary>
    /// Handle importing JSON to tbl/mbm.
    /// </summary>
    /// <param name="inputPath">The path to the JSON file.</param>
    /// <param name="outputPath">Where to write the tbl/mbm to.</param>
    private static void HandleImport(string inputPath, string outputPath, bool longPointers)
    {
      var inputLines = File.ReadAllLines(inputPath).ToList();
      // Determine what type of file we're working with.
      bool isMBM = inputLines[0].Contains("MBM");
      // We remove the first so we can concatenate the rest of the lines.
      inputLines.RemoveAt(0);
      string json = string.Join("", inputLines);
      if (isMBM == true)
      {
        var mbmObject = JsonConvert.DeserializeObject(json, typeof(MBM)) as MBM;
        mbmObject.WriteToFile(outputPath);
      }
      else
      {
        var tblObject = JsonConvert.DeserializeObject(json, typeof(Table)) as Table;
        tblObject.WriteToFile(outputPath, longPointers);
      }
    }
  }
}
