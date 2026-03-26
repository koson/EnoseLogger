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
    public decimal Temp { get; set; }  // Stored as int*10, display as decimal
    public decimal RH { get; set; }    // Stored as int*10, display as decimal
}
