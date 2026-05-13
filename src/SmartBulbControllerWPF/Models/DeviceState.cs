namespace SmartBulbControllerWPF.Models;

public class DeviceState
{
    public bool Power              { get; set; }
    public int  Brightness         { get; set; }  // 0-100
    public int  ColorTemperature   { get; set; }  // 0-100, warm→cool
    public (byte R, byte G, byte B) Color { get; set; }
    public string Mode             { get; set; } = "white";  // "white" | "colour"
}
