using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;



//i want to see the all items in the catalog
string token = "";
if (File.Exists("./main.env"))
{
    var lines = File.ReadAllLines("./main.env");
    foreach (var line in lines)    {
        if (line.StartsWith("TOKEN="))
        {
            token = line.Substring("TOKEN=".Length).Trim();
            break;
        }
    }
}
PrintifyClient client = new PrintifyClient(token);

//list all items in the catalog
var items = await client.GetBlueprintsAsync();
foreach (var item in items)
{
    Console.WriteLine(item);
}