using Microsoft.Data.Sqlite;

public class ListingRepository
{
    private readonly string _dbPath = "ebay.db";

    public ListingRepository()
    {
        using var con = new SqliteConnection($"Data Source={_dbPath}");
        con.Open();

        var cmd = con.CreateCommand();
        cmd.CommandText =
        @"
        CREATE TABLE IF NOT EXISTS Listings (
            Url TEXT PRIMARY KEY,
            SearchTerm TEXT,
            Name TEXT,
            Price TEXT,
            Shipping TEXT,
            Image TEXT,
            Sponsored INTEGER,
            Sales TEXT,
            ScrapedAt TEXT
        )";
        cmd.ExecuteNonQuery();
    }

    // public void Save(List<EbayListing> items)
    // {
    //     using var con = new SqliteConnection($"Data Source={_dbPath}");
    //     con.Open();

    //     foreach (var i in items)
    //     {
    //         var cmd = con.CreateCommand();
    //         cmd.CommandText =
    //         @"
    //         INSERT OR REPLACE INTO Listings
    //         VALUES ($url,$search,$name,$price,$ship,$img,$spon,$sales,$date)
    //         ";

    //         cmd.Parameters.AddWithValue("$url", i.Url);
    //         cmd.Parameters.AddWithValue("$search", i.SearchTerm);
    //         cmd.Parameters.AddWithValue("$name", i.Name);
    //         cmd.Parameters.AddWithValue("$price", i.Price);
    //         cmd.Parameters.AddWithValue("$ship", i.Shipping);
    //         cmd.Parameters.AddWithValue("$img", i.Image);
    //         cmd.Parameters.AddWithValue("$spon", i.Sponsored ? 1 : 0);
    //         cmd.Parameters.AddWithValue("$sales", i.Sales);
    //         cmd.Parameters.AddWithValue("$date", DateTime.UtcNow.ToString("o"));

    //         cmd.ExecuteNonQuery();
    //     }
    // }
}