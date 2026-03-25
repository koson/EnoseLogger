namespace EnoseLogger.Models;

public class SensorData
{
    public DateTime Timestamp { get; set; }
    public int SO2 { get; set; }
    public int NO2 { get; set; }
    public int NO { get; set; }
    public int CO { get; set; }
    public int VOC1 { get; set; }
    public int VOC2 { get; set; }
    public int Temp { get; set; }
    public int RH { get; set; }
}
