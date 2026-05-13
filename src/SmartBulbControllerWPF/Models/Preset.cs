namespace SmartBulbControllerWPF.Models;

public class Preset
{
    public string Name            { get; set; } = string.Empty;
    public bool   IsWhiteMode     { get; set; } = true;
    public int    Brightness      { get; set; } = 100;
    public int    ColorTemperature{ get; set; } = 50;
    public byte   R               { get; set; } = 255;
    public byte   G               { get; set; } = 255;
    public byte   B               { get; set; } = 255;
    public bool   IsBuiltIn       { get; set; } = false;
}
