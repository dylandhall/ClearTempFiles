using System.Text.RegularExpressions;

namespace ClearTempFiles;

public class FilesToDelete
{
    public FilesToDelete()
    {
        FileMask = string.Empty;
        Path = string.Empty;
    }

    public FilesToDelete(string path, FilesToDelete parent)
    {
        RetentionInDays = parent.RetentionInDays;
        FileMask = parent.FileMask;
        Recurse = true;
        Path = path;
    }

    public int RetentionInDays { get; set; }
    public string FileMask { get; set; }
    public bool Recurse { get; set; }
    public string Path { get; set; }

    
}

public static class FilesToDeleteMethods
{

    public static Task ClearFolder(this FilesToDelete f, DateTimeOffset? now = null) =>
        ClearFolder(f.Path, f.Recurse, isSubFolder: false, f.RetentionInDays, f.FileMask, now ?? DateTimeOffset.UtcNow);

    public static Task ClearFolder(string path, bool recurse, bool isSubFolder, int retentionInDays, string fileMask,
        DateTimeOffset now)
    {
        var tasks = new List<Task>();
        try
        {
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path)) return Task.CompletedTask;

            if (recurse)
            {
                tasks.AddRange(Directory.GetDirectories(path).Select(subPath =>
                    ClearFolder(subPath, true, true, retentionInDays, fileMask, now)));
            }

            var files = Directory.GetFiles(path);

            if (retentionInDays > 0)
            {
                var cutOff = now - TimeSpan.FromDays(retentionInDays);
                files = files.Where(f => new FileInfo(f).LastWriteTimeUtc < cutOff).ToArray();
            }

            if (!string.IsNullOrEmpty(fileMask))
            {
                var regex = new Regex(fileMask, RegexOptions.IgnoreCase);
                files = files.Where(f => regex.IsMatch(f)).ToArray();
            }

            tasks.Add(Task.Run(() =>
            {
                foreach (var file in files)
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Error deleting file: {file}, {e.Message}");
                    }
                }
            }));

            return isSubFolder
                ? Task.WhenAll(tasks).ContinueWith(r =>
                {
                    try
                    {
                        var hasFilesOrFolders = Directory.EnumerateFiles(path).Any() || Directory.EnumerateDirectories(path).Any();
                        if (!hasFilesOrFolders) Directory.Delete(path);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error removing {path}: {ex.Message}");
                    }
                }, TaskContinuationOptions.OnlyOnRanToCompletion)
                : Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error cleaning {path}: {ex.Message}");
            return tasks.Any() ? Task.WhenAll(tasks) : Task.CompletedTask;
        }
    }
}