using System.Collections.Concurrent;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using VYaml.Serialization;
using static UGConverter.App;

namespace UGConverter;

public class GUIDConversion
{
    public static Dictionary<string, Metadata> OriginalMetadata = new();
    public static List<Metadata> TargetMetadatas = new();

    public static Dictionary<string, string> MatchedGUIDs = new();

    private static readonly string[] UnityExtensions =
    [
        ".mat",
        ".prefab",
        ".asset",
        ".unity",
        ".anim",
        ".controller",
        ".mixer",
        ".physicMaterial",
        ".renderTexture"
    ];

    public static Metadata? ParseFile(string filePath)
    {
        string directoryPath = Path.GetDirectoryName(filePath) ?? string.Empty;

        DirectoryInfo? directoryInfo = new DirectoryInfo(directoryPath);
        while (directoryInfo != null)
        {
            if (directoryInfo.Name == "Assets")
                break;
            directoryInfo = directoryInfo.Parent;
        }

        if (directoryInfo == null)
            throw new NullReferenceException();

        string relativePath = Path.GetRelativePath(directoryInfo.FullName, filePath);


        CalculateMD5(filePath, out string md5Hash, out long fileSize);

        string metaFilePath = Path.Combine(directoryPath, filePath + ".meta");

        try
        {
            Logger.Debug($"Parsing {Path.GetFileName(metaFilePath)}");
            var yamlParser = YamlSerializer.Deserialize<UnityMetaObject>(File.ReadAllBytes(metaFilePath));

            var metaData = new Metadata(relativePath, yamlParser.guid, fileSize, md5Hash);
            return metaData;
        }
        catch (Exception e)
        {
            Logger.Error($"{Path.GetFileName(filePath)}:\n{e}");
        }

        return null;
    }

    public static void UpdateFiles(string inputDirectory, string outputDirectory)
    {
        MatchGUIDs();

        var files = Directory.GetFiles(inputDirectory, "*.*", SearchOption.AllDirectories);

        Parallel.ForEach(files, filePath =>
        {
            if (Path.GetExtension(filePath) == ".meta")
                return;
            
            Logger.Debug($"Updating File: {Path.GetFileName(filePath)}");
            var metadata = OriginalMetadata[filePath];

            if (!UnityExtensions.Contains(Path.GetExtension(filePath)))
                ConvertFile(outputDirectory, filePath, metadata, false);
            else
                ConvertFile(outputDirectory, filePath, metadata);
        });
    }


    private static void MatchGUIDs()
    {
        MatchedGUIDs.Clear();

        ConcurrentBag<(string, string)> matchedGUIDsBag = new ConcurrentBag<(string, string)>();

        Parallel.ForEach(OriginalMetadata.Values, metadata =>
        {
            var matchingMetadata = GetMatchingMetadata(metadata);
            if (matchingMetadata != null)
            {
                matchedGUIDsBag.Add((metadata.guid, matchingMetadata.guid));
            }
        });
        MatchedGUIDs = matchedGUIDsBag.ToDictionary();
    }

    private static void ConvertFile(string outputDirectory, string filePath, Metadata metaData, bool updateFile = true)
    {
        if (OriginalMetadata.Count == 0 || TargetMetadatas.Count == 0)
            return;
        
        try
        {
            var metaFilePath = filePath + ".meta";
            
            string fileOutputDirectory = Path.Combine(Path.Combine(outputDirectory, "Assets"), metaData.relativePath);
            if (!Directory.Exists(Path.GetDirectoryName(fileOutputDirectory)))
                Directory.CreateDirectory(Path.GetDirectoryName(fileOutputDirectory));

            if (updateFile)
            {
                var fileLines = File.ReadAllLines(filePath);
                
                UpdateGUIDsInFile(ref fileLines);

                Logger.Debug($"Writing file to '{fileOutputDirectory}'");

                File.WriteAllLines(fileOutputDirectory, fileLines);
            }
            else
            {
                
                Logger.Debug($"Copying file to '{fileOutputDirectory}'");
                File.Copy(filePath, fileOutputDirectory, true);
            }



            var metaLines = File.ReadAllLines(metaFilePath);
            UpdateGUIDsInFile(ref metaLines);


            string metaFileOutputDirectory =
                Path.Combine(Path.Combine(outputDirectory, "Assets"), metaData.relativePath + ".meta");
            if (!Directory.Exists(Path.GetDirectoryName(metaFileOutputDirectory)))
                Directory.CreateDirectory(Path.GetDirectoryName(metaFileOutputDirectory));


            Logger.Debug($"Writing meta file to '{metaFileOutputDirectory}'");

            File.WriteAllLines(metaFileOutputDirectory, metaLines);
        }
        catch (Exception e)
        {
            Logger.Error($"{e}");
        }
    }

    private static void UpdateGUIDsInFile(ref string[] fileLines)
    {
        for (var index = 0; index < fileLines.Length; index++)
        {
            var fileLine = fileLines[index];
            if (!fileLine.Contains("guid:"))
                continue;

            var start = fileLine.IndexOf("guid: ");
            var guid = "";
            for (int i = start + 6; i < fileLine.Length; i++)
            {
                if (fileLine.Substring(i, 1).Equals(",") || fileLine.Substring(i, 1).Equals(" "))
                    break;
                guid += fileLine[i];
            }

            if (MatchedGUIDs.TryGetValue(guid, out var newGUID))
                fileLines[index] = fileLine.Replace(guid, newGUID);
        }
    }

    private static void CalculateMD5(string filename, out string md5Hash, out long fileSize)
    {
        using var md5 = MD5.Create();
        using var stream = File.OpenRead(filename);

        fileSize = stream.Length;

        byte[] hash;

        if (filename.EndsWith(
                ".asset")) // Asset files from ripped games might use different names, so try using just the data maybe?
        {
            string data = "";
            using var streamReader = new StreamReader(stream);
            while (streamReader.ReadLine() is { } line)
            {
                if (line.TrimStart().StartsWith("_typelessdata:"))
                {
                    data = line;
                    break;
                }
            }

            hash = md5.ComputeHash(Encoding.UTF8.GetBytes(data));
        }
        else
        {
            hash = md5.ComputeHash(stream);
        }


        md5Hash = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        Logger.Debug($"Hash for '{filename}': {md5Hash}");
    }

    private static Metadata? GetMatchingMetadata(Metadata metadata)
    {
        var exactMatch = TargetMetadatas.AsParallel().FirstOrDefault(m => m.Matches(metadata));
        

        if (exactMatch != null)
            return exactMatch;

        return TargetMetadatas.AsParallel().FirstOrDefault(m => m.relativePath == metadata.relativePath);
    }

    public static int CalculateLevenshteinDistance(string s, string t)
    {

        int n = String.IsNullOrWhiteSpace(s) ? 0 : s.Length;
        int m = String.IsNullOrWhiteSpace(t) ? 0 : t.Length;
        int[,] d = new int[n + 1, m + 1];

        if (n == 0 && m == 0)
            return -1;

        if (n == 0) return m;
        if (m == 0) return n;

        for (int i = 0; i <= n; d[i, 0] = i++) ;
        for (int j = 0; j <= m; d[0, j] = j++) ;

        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= m; j++)
            {
                int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }

        return d[n, m];
    }

    public static int FindClosestStringByLevenshtein(string target, List<string> stringList)
    {
        if (stringList == null || stringList.Count == 0)
            return -1;

        return stringList
            .AsParallel()
            .Select((s, index) => new
            {
                Index = index,
                Distance = CalculateLevenshteinDistance(target, s)
            })
            .OrderBy(x => x.Distance)
            .First()
            .Index;
    }

    public static Metadata FindClosestMetadataByLevenshtein(string target, List<Metadata> metadatas)
    {
        if (metadatas == null || metadatas.Count == 0)
            return null;

        return metadatas
            .AsParallel()
            .Select(m => new
            {
                Metadata = m,
                Distance = m.relativePath == target
                    ? 0
                    : CalculateLevenshteinDistance(target, m.relativePath)
            })
            .OrderBy(x => x.Distance)
            .First()
            .Metadata;
    }
}