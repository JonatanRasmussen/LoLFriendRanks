// This following is the content of file: BigHappyFile.cs
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
namespace GlobalNameSpace;

public class LoLProfile(string gameName, string tagLine)
{
    public string GameName { get; set; } = gameName;
    public string TagLine { get; set; } = tagLine;
    public string IrlName { get; set; } = "???";
    public AccountDTO Account { get; set; } = new();
    public SummonerDTO Summoner { get; set; }  = new();
    public LeagueEntryDTO SoloQueueRank { get; set; } = new();
    public LeagueEntryDTO FlexQueueRank { get; set; } = new();

    public string FormatName()
    {
        string name = $"{IrlName} ({GameName} #{TagLine})";
        int exactLength = 33;
        return EnsureStringLength(name, exactLength);
    }

    public string DisplayStats(LeagueEntryDTO rankedQueue)
    {
        int lp = rankedQueue.CountLP();
        string ladderString = rankedQueue.SummarizeRankAndPlayed();
        return $"{FormatName()}{lp} LP - {ladderString}";
    }

    public string DisplayLevel()
    {
        int profileLevel = Summoner.SummonerLevel;
        double hoursPerLevel = 7.0;
        double totalHours = profileLevel * hoursPerLevel;
        double weeksElapedSinceDec2017 = 340.0;
        double hoursPerWeek = totalHours / weeksElapedSinceDec2017;
        string roundedHoursPerWeek = hoursPerWeek.ToString("0.0");
        return $"{FormatName()}{totalHours} hours, {roundedHoursPerWeek} h/week (Lv. {profileLevel})";
    }

    public static List<LoLProfile> SortByLadderPosition(List<LoLProfile> profiles, bool useFlexRank)
    {
        List<LoLProfile> sorted;
        if (useFlexRank)
        {
            sorted = [.. profiles.OrderBy(profile => profile.FlexQueueRank.LadderPosition())];
        }
        else
        {
            sorted = [.. profiles.OrderBy(profile => profile.SoloQueueRank.LadderPosition())];
        }
        sorted.Reverse();
        return sorted;
    }

    public static List<LoLProfile> SortBySummonerLevel(List<LoLProfile> profiles)
    {
        List<LoLProfile> sorted;
        sorted = [.. profiles.OrderBy(profile => profile.Summoner.SummonerLevel)];
        sorted.Reverse();
        return sorted;
    }

    public async Task FetchProfileData()
    {
        int sleepDelayInMs = 3300; // Sleep to avoid server error 429 (too many requests)
        await FetchAccount();
        await Task.Delay(sleepDelayInMs); // My api key is limited to 20 server requests per second
        await FetchSummoner();
        await Task.Delay(sleepDelayInMs); // My api key is limited to 20 server requests per second
        await FetchRanked();
    }

    private async Task FetchAccount()
    {
        Account = await ApiCaller.RequestAccountData(GameName, TagLine);
    }

    private async Task FetchSummoner()
    {
        if (Account.Puuid != null)
        {
            Summoner = await ApiCaller.RequestSummonerData(Account.Puuid);
        }
        else
        {
            Console.WriteLine("MyError: Account.Puuid is null");
        }
    }

    private async Task FetchRanked()
    {
        if (Summoner.Id != null)
        {
            List<LeagueEntryDTO> ranks = await ApiCaller.RequestRankedData(Summoner.Id);
            ParseRankedQueues(ranks);
        }
        else
        {
            Console.WriteLine("MyError: Summoner.Id is null");
        }
    }

    private void ParseRankedQueues(List<LeagueEntryDTO> ranks)
    {
        foreach (var rank in ranks)
        {
            if (rank.QueueType == LeagueEntryDTO.SOLOQUEUE)
            {
                SoloQueueRank = rank;
            }
            else if (rank.QueueType == LeagueEntryDTO.FLEXQUEUE)
            {
                FlexQueueRank = rank;
            }
        }
    }

    private static string EnsureStringLength(string input, int exactLength)
    {
        if (input.Length >= exactLength)
        {
            return input[..exactLength];
        }
        else
        {
            return input.PadRight(exactLength);
        }
    }
}

public static class ApiCaller
{
    private static readonly HttpClient _client = new();
    private static readonly string _apiKey = BudosApiKey.ReadSecretApiKey();
    private static readonly string _baseRegionUri = "https://europe.api.riotgames.com";
    private static readonly string _basePlatformUri = "https://euw1.api.riotgames.com";

    public static void Print(string description, string obj)
    {
        Console.WriteLine();
        Console.WriteLine(description);
        Console.WriteLine(obj);
        Console.WriteLine();
    }

    public static async Task<AccountDTO> RequestAccountData(string gameName, string tagLine)
    {
        // https://developer.riotgames.com/apis#account-v1/GET_getByRiotId
        string path = "riot/account/v1/accounts/by-riot-id";
        string uri = $"{_baseRegionUri}/{path}/{gameName}/{tagLine}?api_key={_apiKey}";
        return await RequestInfo<AccountDTO>(uri) ?? new();
    }

    public static async Task<SummonerDTO> RequestSummonerData(string puuid)
    {
        // https://developer.riotgames.com/apis#summoner-v4/GET_getByPUUID
        string path = "lol/summoner/v4/summoners/by-puuid";
        string uri = $"{_basePlatformUri}/{path}/{puuid}?api_key={_apiKey}";
        return await RequestInfo<SummonerDTO>(uri) ?? new();
    }

    public static async Task<List<LeagueEntryDTO>> RequestRankedData(string summonerId)
    {
        // https://developer.riotgames.com/apis#league-v4/GET_getLeagueEntriesForSummoner
        string path = "lol/league/v4/entries/by-summoner";
        string uri = $"{_basePlatformUri}/{path}/{summonerId}?api_key={_apiKey}";
        return await RequestInfo<List<LeagueEntryDTO>>(uri) ?? [];
    }

    public static async Task<T?> RequestInfo<T>(string uri)
    {
        Print("Request sent to:", uri);
        try
        {
            var response = await _client.GetStringAsync(uri);
            var deserializedObj = JsonSerializer.Deserialize<T>(response);
            Print("Server response:", response.ToString());
            if (deserializedObj != null)
            {
                return deserializedObj;
            }
            Console.WriteLine("MyError: deserializedObj is null");
            return default;
        }
        catch (Exception e)
        {
            Print("Error during request:", e.Message);
            Console.WriteLine("MyInfo: If you get 'Response status code 403 (Forbidden)', you need a new API key.");
            // Get your API key here -> https://developer.riotgames.com/docs/lol
            return default;
        }
    }
}

public static class Utils
{
    public const string DISCORD_FORMAT_TEXT_AS_CODE = "```";
    public static async Task CallApiForProfileData(List<LoLProfile> profiles)
    {
        var tasks = new List<Task>();
        foreach (var profile in profiles)
        {
            Task task = profile.FetchProfileData();
            tasks.Add(task);
        }
        await Task.WhenAll(tasks);
    }

    public static void PrintProfileList(List<LoLProfile> profiles, bool useFlexData)
    {
        Console.WriteLine(DISCORD_FORMAT_TEXT_AS_CODE);
        if (useFlexData)
        {
            Console.WriteLine("<<< LEADERBOARDS: FLEXQUEUE >>>");
        }
        else
        {
            Console.WriteLine("<<< LEADERBOARDS: SOLOQUEUE >>>");
        }
        var sorted = LoLProfile.SortByLadderPosition(profiles, useFlexData);
        foreach (var sortedProfile in sorted)
        {
            LeagueEntryDTO rankedQueue;
            if (useFlexData)
            {
                rankedQueue = sortedProfile.FlexQueueRank;
            }
            else
            {
                rankedQueue = sortedProfile.SoloQueueRank;
            }
            Console.WriteLine($"{sortedProfile.DisplayStats(rankedQueue)}");
        }
        Console.WriteLine(DISCORD_FORMAT_TEXT_AS_CODE);
        Console.WriteLine();
    }

    public static void PrintProfileLevels(List<LoLProfile> profiles)
    {
        Console.WriteLine(DISCORD_FORMAT_TEXT_AS_CODE);
        Console.WriteLine("<<< LEADERBOARDS: Playtime (since Dec. 2017) >>>");
        var sorted = LoLProfile.SortBySummonerLevel(profiles);
        foreach (var sortedProfile in sorted)
        {
            Console.WriteLine($"{sortedProfile.DisplayLevel()}");
        }
        Console.WriteLine(DISCORD_FORMAT_TEXT_AS_CODE);
        Console.WriteLine();
    }

    public static List<LoLProfile> ReadAccountsJson()
    {
        string fileNameAndExtension = "accounts.json";
        string jsonContent;
        try
        {
            jsonContent = ReadFile(fileNameAndExtension);
        }
        catch (System.IO.FileNotFoundException)
        {
            Console.WriteLine($"File not found: {FilePath(fileNameAndExtension)}");
            Console.WriteLine($"Instead of reading the account list from {fileNameAndExtension}, test data will be used instead.");
            jsonContent = TestData.FRIENDS;
        }
        var accounts = JsonSerializer.Deserialize<List<AccountDTO>>(jsonContent) ?? [];
        List<LoLProfile> profiles = [];
        foreach (var account in accounts)
        {
            LoLProfile profile = new(account.GameName ?? "???", account.TagLine ?? "???")
            {
                IrlName = account.IrlName ?? "???"
            };
            profiles.Add(profile);
        }
        return profiles;
    }

    public static string FilePath(string fileNameAndExtension)
    {
        string currentDirectory = BudosDirectoryPath.FULL_DIR_PATH;
        return Path.Combine(currentDirectory, fileNameAndExtension);
    }

    public static string ReadFile(string fileNameAndExtension)
    {
        string filePath = FilePath(fileNameAndExtension);
        using StreamReader reader = new(filePath);
        return reader.ReadToEnd();
    }

    public static void WriteFile(string fileNameAndExtension, string content)
    {
        string filePath = FilePath(fileNameAndExtension);
        using StreamWriter writer = new(filePath);
        writer.Write(content);
    }
}

public class AccountDTO
{
    [JsonPropertyName("puuid")]
    public string? Puuid { get; set; }

    [JsonPropertyName("gameName")]
    public string? GameName { get; set; }

    [JsonPropertyName("tagLine")]
    public string? TagLine { get; set; }

    [JsonPropertyName("irlName")]
    public string? IrlName { get; set; }
}

public class SummonerDTO
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("accountId")]
    public string? AccountId { get; set; }

    [JsonPropertyName("puuid")]
    public string? Puuid { get; set; }

    [JsonPropertyName("profileIconId")]
    public int ProfileIconId { get; set; }

    [JsonPropertyName("revisionDate")]
    public long RevisionDate { get; set; }

    [JsonPropertyName("summonerLevel")]
    public int SummonerLevel { get; set; }
}

public class LeagueEntryDTO
{
    public const string SOLOQUEUE = "RANKED_SOLO_5x5";
    public const string FLEXQUEUE = "RANKED_FLEX_SR";

    [JsonPropertyName("leagueId")]
    public string? LeagueId { get; set; }

    [JsonPropertyName("queueType")]
    public string? QueueType { get; set; }

    [JsonPropertyName("tier")]
    public string? Tier { get; set; }

    [JsonPropertyName("rank")]
    public string? Rank { get; set; }

    [JsonPropertyName("summonerId")]
    public string? SummonerId { get; set; }

    [JsonPropertyName("leaguePoints")]
    public int LeaguePoints { get; set; }

    [JsonPropertyName("wins")]
    public int Wins { get; set; }

    [JsonPropertyName("losses")]
    public int Losses { get; set; }

    [JsonPropertyName("veteran")]
    public bool Veteran { get; set; }

    [JsonPropertyName("inactive")]
    public bool Inactive { get; set; }

    [JsonPropertyName("freshBlood")]
    public bool FreshBlood { get; set; }

    [JsonPropertyName("hotStreak")]
    public bool HotStreak { get; set; }

    public string SummarizeRank()
    {
        return $"{Capitalize(Tier)}{ConvertRankToInt()} {LeaguePoints}lp";
    }

    public string SummarizePlayed()
    {
        return $"{Wins}W {Losses}L";
    }

    public string SummarizeRankAndPlayed()
    {
        return $"{SummarizeRank()} ({SummarizePlayed()})";
    }

    public int CountGamesPlayed()
    {
        return Wins + Losses;
    }

    public double LadderPosition()
    {
        int tierNumeric = 100_000 * ConvertTierToInt();
        double rankNumeric = 5_000 / ConvertRankToInt();
        return tierNumeric + rankNumeric + LeaguePoints;
    }

    public int CountLP()
    {
        int tierLP = 400 * ConvertTierToInt();
        int rankLP = 400 - (100 * ConvertRankToInt());
        if (Tier == "MASTER" || Tier == "GRANDMASTER" || Tier == "CHALLENGER")
        {
            rankLP = 100 - (100 * ConvertRankToInt());
        }
        int totalLP = tierLP + rankLP + LeaguePoints;
        if (totalLP > 100)
        {
            return totalLP;
        }
        return 0;
    }

    private int ConvertTierToInt()
    {
        return Tier switch
        {
            null => -1,
            "" => -1,
            "CHALLENGER" => 9,
            "GRANDMASTER" => 8,
            "MASTER" => 7,
            "DIAMOND" => 6,
            "EMERALD" => 5,
            "PLATINUM" => 4,
            "GOLD" => 3,
            "SILVER" => 2,
            "BRONZE" => 1,
            "IRON" => 0,
            _ => -1,
        };
    }

    private int ConvertRankToInt()
    {
        return Rank switch
        {
            null => -1,
            "" => -1,
            "I" => 1,
            "II" => 2,
            "III" => 3,
            "IV" => 4,
            _ => 5,
        };
    }

    private static string Capitalize(string? input)
    {
        return input switch
        {
            null => "Unranked",
            "" => "Unranked",
            _ => string.Concat(input[0].ToString().ToUpper(), input.Substring(1).ToLower())
        };
    }
}
