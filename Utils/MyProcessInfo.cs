using System.Diagnostics;

namespace WbtGuardService.Utils;

/// <summary>
/// Process线程敏感，这里解耦下
/// </summary>
public class MyProcessInfo
{
    public MyProcessInfo(Process p)
    {
        Id = p?.Id.ToString();
        ProcessName = p?.ProcessName;
        StartTime = p?.StartTime ?? DateTime.Now;
    }
    public string Id { get; set; }
    public string ProcessName { get; set; }
    public DateTime StartTime { get; set; }
}
