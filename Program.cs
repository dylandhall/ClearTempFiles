
using System.Text.Json;
using ClearTempFiles;

const string configFile = "files-to-delete.json";
var configPath = $"{AppContext.BaseDirectory}\\{configFile}";
if (File.Exists(configPath))
{
    await using (var stream = File.OpenRead(configPath))
    {
        var config = await JsonSerializer.DeserializeAsync<List<FilesToDelete>>(stream);
        if (config == null) throw new Exception("Config not correct");
     
        await Task.WhenAll(config.Select(f => f.ClearFolder()));
        
        return;
    }
}

var example = new List<FilesToDelete>
{
    new FilesToDelete
    {
        FileMask = @".*\.zip",
        Path = @"C:\temp-zips",
        Recurse = false,
        RetentionInDays = 2
    },
    new FilesToDelete
    {
        Path = @"C:\temp",
        Recurse = true
    },
};

await File.WriteAllTextAsync(configPath, JsonSerializer.Serialize(example));

