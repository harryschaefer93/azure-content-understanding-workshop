namespace CU_TestHarness.Models;

public class DetectedField
{
    public bool Include { get; set; } = true;
    public string Name { get; set; } = "";
    public string Method { get; set; } = "extract";
    public int Frequency { get; set; }
    public string? SampleValue { get; set; }
}
