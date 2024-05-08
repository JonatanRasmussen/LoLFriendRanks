// This following is the content of file: Program.cs
using System;
using System.Collections.Generic;
namespace GlobalNameSpace;

public class Program
{
    public static async Task Main(string[] args)
    {
        List<LoLProfile> profiles = Utils.ReadAccountsJson();
        await Utils.CallApiForProfileData(profiles);
        bool useFlexData = false;
        Utils.PrintProfileList(profiles, useFlexData);
        Utils.PrintProfileLevels(profiles);
        useFlexData = true;
        Utils.PrintProfileList(profiles, useFlexData);
    }
}

//#############################
// PROGRAM CONFIGURATION BELOW
//#############################

public static class BudosDirectoryPath
{
    // The full path to this project on Budo's PC.
    public static readonly string FULL_DIR_PATH = @"C:\Users\BudoB\OneDrive\Dokumenter Tekst\Programmering\LoLFriendRanks\Program\";
}

public static class BudosApiKey
{
    // This API key belongs to budo. It expires after 24hours. You can use it but don't abuse :)
    public static readonly string SECRET_API_KEY = "INSERT_YOUR_API_KEY_HERE";

    public static string ReadSecretApiKey()
    {
        string fileNameAndExtension = "secretkey.txt";
        string secretKey;
        try
        {
            secretKey = Utils.ReadFile(fileNameAndExtension);
        }
        catch (System.IO.FileNotFoundException)
        {
            Console.WriteLine($"File not found: {Utils.FilePath(fileNameAndExtension)}");
            Console.WriteLine($"Due to file not found, the following hardcoded key is used instead: {SECRET_API_KEY}.");
            secretKey = SECRET_API_KEY;
        }
        return secretKey;
    }
}

public static class TestData
{
    // TestData for whoever I share this code with so that they don't have to read accounts.json
    // This is only used if the program fails to find accounts.json (need to hardcode its path on your machine)
    public static readonly string FRIENDS = @"[
    {""irlName"": ""Tombom"", ""gameName"": ""Tombom"", ""tagLine"": ""EUW""},
    {""irlName"": ""Marco"", ""gameName"": ""Dog"", ""tagLine"": ""Rteon""},
    {""irlName"": ""Zimon"", ""gameName"": ""Fandersay"", ""tagLine"": ""EUW""},
    {""irlName"": ""Budo"", ""gameName"": ""Juél"", ""tagLine"": ""0000""},
    {""irlName"": ""Hoppe"", ""gameName"": ""HankyBoy"", ""tagLine"": ""EUW""},
    {""irlName"": ""Mads"", ""gameName"": ""MKmads"", ""tagLine"": ""EUW""},
    {""irlName"": ""August"", ""gameName"": ""Maximaeus"", ""tagLine"": ""EUW""},
    {""irlName"": ""Bølle"", ""gameName"": ""Kogalee"", ""tagLine"": ""EUW""},
    {""irlName"": ""Nicolai"", ""gameName"": ""NicoWhuuuutxD"", ""tagLine"": ""EUW""},
    {""irlName"": ""Jonas"", ""gameName"": ""Jay Link"", ""tagLine"": ""EUW""},
    {""irlName"": ""Frederikpop"", ""gameName"": ""Dog"", ""tagLine"": ""Pop""},
    {""irlName"": ""DavidCock"", ""gameName"": ""Dog"", ""tagLine"": ""miauw""},
    {""irlName"": ""DavidSnawer"", ""gameName"": ""Snawer"", ""tagLine"": ""EUW""},
    {""irlName"": ""Nico"", ""gameName"": ""Lexybang"", ""tagLine"": ""EUW""},
    {""irlName"": ""Andreas"", ""gameName"": ""SnacksToHuggi"", ""tagLine"": ""EUW""},
    {""irlName"": ""Noah"", ""gameName"": ""CrowexTheGodOne"", ""tagLine"": ""EUW""},
    {""irlName"": ""Neermark"", ""gameName"": ""Neermark"", ""tagLine"": ""EUW""},
    {""irlName"": ""ThomasLegend"", ""gameName"": ""A Dumb Drunk"", ""tagLine"": ""EUW""}
    ]";
}