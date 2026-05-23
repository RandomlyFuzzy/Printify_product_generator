namespace PrintifyGenerator.Library.VectorStorage;

public class VectorRecord
{
    public float[] Embedding { get; set; } = [];
    public float Score { get; set; }
    public string Concept { get; set; } = "";
    public string Prompt { get; set; } = "";
    public string Source { get; set; } = "";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
