namespace Trading.Infrastructure.Persistence.FileStorage;

using System.Text.Json;

internal static class FilePersistence
{
    private static readonly string RootDirectory = Environment.GetEnvironmentVariable("APP_DATA_PATH")
                                                   ?? Path.Combine(AppContext.BaseDirectory, "data");
    
    public static async Task SaveAsync<T>(T obj, string relativeFilePath)
    {
        var filePath = Path.Combine(RootDirectory, relativeFilePath);
        var json = JsonSerializer.Serialize(obj, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        
        //Make sure the directory exists:
        Directory.CreateDirectory(RootDirectory);
        
        await File.WriteAllTextAsync(filePath, json);
    }

    public static async Task<T?> LoadAsync<T>(string relativeFilePath)
    {
        var filePath = Path.Combine(RootDirectory, relativeFilePath);
        if (!File.Exists(filePath))
            return default;

        var json = await File.ReadAllTextAsync(filePath);
        return JsonSerializer.Deserialize<T>(json);
    }
}
