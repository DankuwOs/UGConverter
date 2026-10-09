using System.Collections.Concurrent;
using System.IO;
using System.Reflection;
using System.Windows;
using Microsoft.Win32;
using Newtonsoft.Json;
using Serilog.Core;
using Path = System.IO.Path;

namespace UGConverter;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private static readonly string[] UnityFileExtensions =
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
    
    public MainWindow()
    {
        InitializeComponent();
    }

    
    private void SetupMetadataFromFolder(object sender, RoutedEventArgs e)
    {
        SetupMetadata(false);
    }

    private void SaveMetadataToFile(object sender, RoutedEventArgs e)
    {
        SetupMetadata(false, true);
    }

    
    private void SetupTargetMetadataFromFile(object sender, RoutedEventArgs e)
    {
        LoadMetadataList();
    }

    private void SetupTargetMetadataFromFolder(object sender, RoutedEventArgs e)
    {
        SetupMetadata(true);
    }
    

    private void UpdateFilesFromToFolder(object sender, RoutedEventArgs e)
    {
        UpdateFiles();
    }
    
    

    private void SetupMetadata(bool isTarget, bool saveToFile = false)
    {
        var assetFolderDialog = new OpenFolderDialog
        {
            Title = "Select the Assets folder in your unity project"
        };

        if (assetFolderDialog.ShowDialog() == false)
            return;

        if (isTarget)
        {
            var metadataList = GetMetadataList(assetFolderDialog.FolderName);
            
            GUIDConversion.TargetMetadatas = metadataList;
            App.Logger.Debug($"Found {metadataList.Count} metadata files in target directory.");
        }
        else
        {
            var metadataDict = GetMetadataDict(assetFolderDialog.FolderName);

            GUIDConversion.OriginalMetadata = metadataDict;
            App.Logger.Debug($"Found {metadataDict.Count} metadata files in source directory.");

            if (!saveToFile)
                return;

            var json = JsonConvert.SerializeObject(metadataDict.Values.ToList());

            var saveFileDialog = new SaveFileDialog()
            {
                Title = "Select where to save project metadata",
                InitialDirectory = Path.GetDirectoryName(Assembly.GetCallingAssembly().FullName),
                Filter = "JSON files (*.json)|*.json",
                FileName = "ProjectMetadata"
            };
            if (saveFileDialog.ShowDialog() == true)
            {
                App.Logger.Debug($"Saving to file '{saveFileDialog.FileName}'");
                File.WriteAllText(saveFileDialog.FileName, json);
            }
        }
    }

    private void LoadMetadataList()
    {
        var metadataFileDialog = new OpenFileDialog()
        {
            Title = "Select the metadata file to convert GUIDs to",
            Filter = "JSON files (*.json)|*.json"
        };

        if (metadataFileDialog.ShowDialog() == false)
            return;
        
        GUIDConversion.TargetMetadatas = JsonConvert.DeserializeObject<List<Metadata>>(File.ReadAllText(metadataFileDialog.FileName));
    }


    private void UpdateFiles()
    {
        var assetFolderDialog = new OpenFolderDialog
        {
            Title = "Select the folder containing assets you want to update in your unity project"
        };

        if (assetFolderDialog.ShowDialog() == false)
            return;
        
        
        var targetFolder = new OpenFolderDialog
        {
            Title = "Select the output folder to output files (Assets folder will be placed inside this folder)"
        };

        if (targetFolder.ShowDialog() == false)
            return;
        
        App.Logger.Debug($"Updating files in '{assetFolderDialog.FolderName}' to '{targetFolder.FolderName}'");
        
        GUIDConversion.UpdateFiles(assetFolderDialog.FolderName, targetFolder.FolderName);
    }
    
    

    private Dictionary<string, Metadata> GetMetadataDict(string folderPath)
    {
        var files = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories);

        ConcurrentBag<(string, Metadata)> concurrentBag = new ConcurrentBag<(string, Metadata)>();
        Parallel.ForEach(files.Where(path => Path.GetExtension(path) != ".meta"), filePath =>
        {
            if (String.IsNullOrEmpty(filePath))
                return;
            
            var metadata = GUIDConversion.ParseFile(filePath);
            if (metadata != null)
            {
                App.Logger.Debug($"Successfully parsed metadata from file '{filePath}'");
                concurrentBag.Add((filePath, metadata));
            }
            else
            {
                App.Logger.Warning($"Failed to parse file '{filePath}'");
            }
        });

        return concurrentBag.ToDictionary();
    }

    private List<Metadata> GetMetadataList(string folderPath)
    {
        var files = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories);

        ConcurrentBag<Metadata> concurrentBag = new ConcurrentBag<Metadata>();
        Parallel.ForEach(files.Where(path => Path.GetExtension(path) != ".meta"), filePath =>
        {
            if (String.IsNullOrEmpty(filePath))
                return;
            
            var metadata = GUIDConversion.ParseFile(filePath);
            if (metadata != null)
            {
                App.Logger.Debug($"Successfully parsed metadata from file '{filePath}'");
                concurrentBag.Add(metadata);
            }
            else
            {
                App.Logger.Warning($"Failed to parse file '{filePath}'");
            }
        });

        return concurrentBag.ToList();
    }
}