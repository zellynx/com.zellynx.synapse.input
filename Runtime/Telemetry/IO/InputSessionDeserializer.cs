using System;
using System.IO;
using Synapse.Runtime.Telemetry.Data;
using UnityEngine.InputSystem;

namespace Synapse.Runtime.Telemetry.IO
{
    public static class InputSessionDeserializer
    {
        public static InputSession Deserialize(Stream _stream) {
            if (_stream == null) { throw new ArgumentNullException(nameof(_stream)); }
            if (!_stream.CanRead) { throw new InvalidOperationException("Stream is not readable."); }
            if (!_stream.CanSeek) { throw new InvalidOperationException("Stream must support seeking."); }

            using BinaryReader reader = new(_stream, System.Text.Encoding.UTF8, true);

            // Read Header
            var magic = reader.ReadString();
            if (magic != FormatProtocol.Magic) { throw new InvalidDataException($"Invalid telemetry stream magic value: {magic}"); }
            EnsureRemainingBytes(_stream, sizeof(int));
            var version = reader.ReadInt32();
            if (version != FormatProtocol.FormatVersion) { throw new InvalidDataException($"Unsupported telemetry format version: {version}"); }

            // Read Session Metadata
            EnsureRemainingBytes(_stream, 16);
            var sessionId = new Guid(reader.ReadBytes(16));
            var sessionName = reader.ReadString();
            EnsureRemainingBytes(_stream, sizeof(long));
            var recordedAtUtc = DateTime.FromBinary(reader.ReadInt64());
            EnsureRemainingBytes(_stream, sizeof(double));
            var duration = reader.ReadDouble();

            // Recreate Session from read metadata
            InputSession session = new() {
                SessionId = sessionId,
                SessionName = sessionName,
                RecordedAtUtc = recordedAtUtc,
                Duration = duration
            };

            // Read Session TrackedActionIds
            EnsureRemainingBytes(_stream, sizeof(int));
            var trackedActionCount = reader.ReadInt32();
            if (trackedActionCount is < 0 or > FormatProtocol.MaxTrackedActions) {
                throw new InvalidDataException("Invalid tracked action count: " + $"{trackedActionCount}");
            }
            session.TrackedActionIds = new Guid[trackedActionCount];
            for (var i = 0; i < trackedActionCount; i++) {
                EnsureRemainingBytes(_stream, 16);
                session.TrackedActionIds[i] = new Guid(reader.ReadBytes(16));
            }

            // Read Session InputRecords
            EnsureRemainingBytes(_stream, sizeof(int));
            var recordCount = reader.ReadInt32();
            if (recordCount is < 0 or > FormatProtocol.MaxRecords) { throw new InvalidDataException($"Invalid Input Record count: " + $"{recordCount}"); }
            session.Records = new InputRecord[recordCount];
            for (var i = 0; i < recordCount; i++) {
                EnsureRemainingBytes(_stream, sizeof(uint));
                var sequence = reader.ReadUInt32();
                EnsureRemainingBytes(_stream, 16);
                var actionId = new Guid(reader.ReadBytes(16));
                EnsureRemainingBytes(_stream, sizeof(int));
                var phaseValue = reader.ReadInt32();
                if (!Enum.IsDefined(typeof(InputActionPhase), phaseValue)) {
                    throw new InvalidDataException($"Invalid InputActionPhase value: " + $"{phaseValue}");
                }

                EnsureRemainingBytes(_stream, sizeof(double));
                var time = reader.ReadDouble();
                EnsureRemainingBytes(_stream, sizeof(int));
                var payloadLength = reader.ReadInt32();
                if (payloadLength is < 0 or > FormatProtocol.MaxPayloadSize) {
                    throw new InvalidDataException($"Invalid payload length: " + $"{payloadLength}");
                }

                byte[] inputData;
                if (payloadLength > 0) {
                    EnsureRemainingBytes(_stream, payloadLength);
                    inputData = reader.ReadBytes(payloadLength);
                }
                else {
                    inputData = Array.Empty<byte>();
                }

                session.Records[i] = new InputRecord {
                    Sequence = sequence,
                    TrackedActionId = actionId,
                    Phase = (InputActionPhase)phaseValue,
                    Time = time,
                    InputData = inputData
                };
            }

            return session;
        }

        private static void EnsureRemainingBytes(Stream _stream, long _requiredBytes) {
            var remainingBytes = _stream.Length - _stream.Position;
            if (remainingBytes < _requiredBytes) { throw new InvalidDataException("Unexpected end of telemetry stream."); }
        }
    }
}