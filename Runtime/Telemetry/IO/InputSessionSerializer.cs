using System;
using System.IO;
using Synapse.Input.Telemetry.Telemetry.Data;

namespace Synapse.Input.Telemetry.Telemetry.IO
{
    public static class InputSessionSerializer
    {
        public static void Serialize(Stream _stream, InputSession _session) {
            if (_stream == null) { throw new ArgumentNullException(nameof(_stream)); }
            if (!_stream.CanWrite) { throw new InvalidOperationException("Stream is not writable."); }
            if (_session == null) { throw new ArgumentNullException(nameof(_session)); }

            using BinaryWriter writer = new(_stream, System.Text.Encoding.UTF8, true);

            // Write Header
            writer.Write(TelemetryInputProtocol.Magic);
            writer.Write(TelemetryInputProtocol.FormatVersion);

            // Write Session Metadata
            writer.Write(_session.SessionId.ToByteArray());
            writer.Write(_session.SessionName ?? string.Empty);
            writer.Write(_session.RecordedAtUtc.ToBinary());
            writer.Write(_session.Duration);

            // Write Session TrackedActionIds
            _session.TrackedActionIds ??= Array.Empty<Guid>(); //Check if null or not and fix
            _session.Records ??= Array.Empty<InputRecord>(); //Check if null or not and fix
            if (_session.TrackedActionIds.Length > TelemetryInputProtocol.MaxTrackedActions) {
                throw new InvalidDataException($"Tracked action count exceeds limit: " + $"{_session.TrackedActionIds.Length}");
            }
            writer.Write(_session.TrackedActionIds.Length);
            foreach (var actionId in _session.TrackedActionIds) {
                writer.Write(actionId.ToByteArray());
            }
            
            // Write Session InputRecords
            if (_session.Records.Length > TelemetryInputProtocol.MaxRecords) {
                throw new InvalidDataException($"Input Record count exceeds limit: " + $"{_session.Records.Length}");
            }
            writer.Write(_session.Records.Length);
            foreach (var record in _session.Records) {
                writer.Write(record.Sequence);
                writer.Write(record.TrackedActionId.ToByteArray());
                writer.Write((int)record.Phase);
                writer.Write(record.Time);

                var inputData = record.InputData ?? Array.Empty<byte>();
                if (inputData.Length > TelemetryInputProtocol.MaxPayloadSize) {
                    throw new InvalidDataException($"Payload exceeds maximum size: " + $"{inputData.Length}");
                }

                writer.Write(inputData.Length);
                writer.Write(inputData);
            }
        }
    }
}