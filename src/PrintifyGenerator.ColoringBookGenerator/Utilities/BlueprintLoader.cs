using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using PrintifyGenerator.ColoringBookGenerator.Models;

namespace PrintifyGenerator.ColoringBookGenerator.Utilities
{
    public static class BlueprintLoader
    {
        public static async Task<BlueprintDetail> LoadAsync(string path)
        {
            var bytes = await File.ReadAllBytesAsync(path);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            return JsonSerializer.Deserialize<BlueprintDetail>(bytes, options);
        }
    }
}
