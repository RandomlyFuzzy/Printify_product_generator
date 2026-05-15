/*
 you will return a json and only a json to explain this
     {
     ""suitability"":3,
     ""DoesViolateLaw"":false,
     ""DoesViolateIPRights"":false,
     ""IsNSFW"":false,
     ""CommercialAppeal"":7,
     ""PrintQuality"":8,
     ""Issues"":[""NSFW"",""Blury"",""Deformed"",""watermark"",""text overlay""],
     ""Recommendations"":[""Increase contrast"",""Remove watermark"",""Enhance colors""],
     ""TargetDemographic"":""General audience"",
     ""EstimatedSalesViability"":0.75
     }
*/

using System.Text.Json;

public record ImageSuitability
{
    public string imageURL { get; set; } = "";
    public float suitability { get; set; } = 10.0f; 
    public bool DoesViolateLaw { get; set; } = false;
    public bool DoesViolateIPRights { get; set; } = false;
    public bool IsNSFW { get; set; } = false; 
    public List<string> Issues { get; set; } = new List<string>(){"None","blurry","deformed","watermark","text overlay","bad anatomy","extra limbs","low quality","artifical", "poor composition","unoriginal","lacking creativity"};
    public Scoring Scoring { get; set; } = new Scoring();

    public bool isValid()
    {
        return suitability >= 0.0f && suitability <= 10.0f && Issues is not null && Scoring is not null;
    }
    public bool HasIssues()
    {
        return Issues != null && Issues.Count > 0;
    }
    public bool IsSuitableForPrint()
    {
        return !DoesViolateLaw && !DoesViolateIPRights && !IsNSFW;
    }
    public float OverallScore()
    {
        return Scoring.OverallScore();
    }

    public string PrettyJsonString()
    {
        return JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
    }
    public string ToJsonString()
    {
        return JsonSerializer.Serialize(this);  
    }

}

public class Scoring
{
    public float commercialAppeal { get; set; } = 0.0f;
    public float printQuality { get; set; } = 0.0f;
    public float estimatedSalesViability { get; set; } = 0.0f;
    public float uniqueness { get; set; } = 0.0f;
    public float technicalSkill { get; set; } = 0.0f;
    public float creativity { get; set; } = 0.0f;
    public float composition { get; set; } = 0.0f;
    public float technique { get; set; } = 0.0f;
    public float originality { get; set; } = 0.0f;

    public float OverallScore()
    {
        // A simple average of all scores, can be weighted as needed
        return (commercialAppeal + printQuality + estimatedSalesViability + uniqueness + technicalSkill + creativity + composition + technique + originality) / 9.0f;
    }



}