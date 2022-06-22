using System.ComponentModel;

namespace Notion.Unity
{
    public enum Metrics
    {
        Status,
        Awareness,
        Kinesis,
        Brainwaves,
        Accelerometer,
        [Description("signalQuality")]
        SignalQuality
    }
}