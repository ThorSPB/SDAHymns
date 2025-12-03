#r "src/SDAHymns.Core/bin/Debug/net10.0/SDAHymns.Core.dll"
#r "nuget: DocumentFormat.OpenXml, 3.3.0"

using SDAHymns.Core.Services;

var parser = new PowerPointParserService();
var pptFile = @"Imnuri Azs\Resurse\Imnuri crestine\ppt\737.PPT";

Console.WriteLine($"Testing parser on: {pptFile}");
Console.WriteLine();

var result = await parser.ExtractHymnInfoAsync(pptFile);

if (result == null)
{
    Console.WriteLine("FAILED: Could not parse the file");
}
else
{
    Console.WriteLine($"SUCCESS: #{result.Value.hymnNumber} - {result.Value.title}");
}
