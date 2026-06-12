namespace Synapse.Input.Telemetry.Telemetry.IO
{
    public static class TelemetryInputProtocol
    {
        public const int FormatVersion = 1;
        public const string Magic = "GITS";
        public const int MaxTrackedActions = 4096;
        public const int MaxRecords = 10_000_000;
        public const int MaxPayloadSize = 1024;
    }
}