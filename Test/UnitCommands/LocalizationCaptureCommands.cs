// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Resources.NetStandard;
using Test.StubClasses;
using TestSupport.Attributes;
using Xunit.Abstractions;
using System.Globalization;
using CsvHelper;

namespace Test.UnitCommands;

public class LocalizationCaptureCommands
{
    private readonly ITestOutputHelper _output;

    public LocalizationCaptureCommands(ITestOutputHelper output)
    {
        _output = output;
    }


    [RunnableInDebugOnly]
    public void WipeLocalizationCaptureDbAndSetToCapture()
    {
        var stub = new StubDefaultLocalizerWithLogging(null, typeof(LocalizationCaptureCommands));
        stub.WipeLocalizationCaptureDb();
    }

    [RunnableInDebugOnly]
    public void DisplayCapturedLocalizations()
    {
        var stub = new StubDefaultLocalizerWithLogging(null, typeof(LocalizationCaptureCommands));

        var entries = stub.ListLocalizationCaptureDb();

        _output.WriteLine($"There are {entries.Count} captured localizations, " +
                          $"with {entries.Count(x => x.PossibleErrors != null)} so problems.");
        foreach (var entry in entries)
        {
            _output.WriteLine($"ResourceClassFullName = {entry.ResourceClassFullName}");
            _output.WriteLine($"     LocalizeKey = {entry.LocalizeKey}, {entry.PossibleErrors ?? ""}");
            _output.WriteLine($"     Actual Message = {entry.ActualMessage}");
            if (entry.MessageFormat != null)
                _output.WriteLine($"     Message Format = {entry.MessageFormat}");
        }
        _output.WriteLine("END ------------------------------------------------------");
    }

    private class CsvInputOfResx
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }

    [RunnableInDebugOnly]
    public void CreateResxFileFromCSV()
    {
        var csvFilePath = "C:\\Users\\JonPSmith\\Desktop\\AuthServices - french.csv";
        var resxFilePath = "C:\\Users\\JonPSmith\\source\\repos\\AuthPermissions.AspNetCore\\" +
                           "Example1.RazorPages.IndividualAccounts\\Resources\\BaseCode.LocalizeResources.NEW.resx";
        
        //see https://joshclose.github.io/CsvHelper/getting-started/#reading-a-csv-file
        using (var reader = new StreamReader(csvFilePath))
        using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
        {
            var records = csv.GetRecords<CsvInputOfResx>();
            //see https://learn.microsoft.com/en-us/dotnet/core/extensions/work-with-resx-files-programmatically#create-a-resx-fil

            using (ResXResourceWriter writer = new ResXResourceWriter(@resxFilePath))
            {
                foreach (var entry in records)
                {
                    writer.AddResource(entry.Name, entry.Value);
                }
            }
        }
    }
}