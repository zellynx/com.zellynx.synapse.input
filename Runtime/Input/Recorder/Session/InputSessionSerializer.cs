using System;
using System.IO;
using UnityEngine.InputSystem;

namespace Synapse.Input.Recorder
{
    public static class InputSessionSerializer
    {
        public const int FormatVersion = 1;
        public const string Magic = "GITS";
        public const int MaxRecords = 10_000_000;
        public const int MaxPayloadSize = 1024;
        
        public static void Serialize(Stream _stream, InputSession _session) {
            if (_stream == null) throw new ArgumentNullException(nameof(_stream));
            if (_session == null) throw new ArgumentNullException(nameof(_session));

            if (!_stream.CanWrite) {
                throw new InvalidOperationException("Stream is not writable.");
            }
            
            using BinaryWriter writer = new(_stream, System.Text.Encoding.UTF8, true);

            // Write Header
            writer.Write(Magic);
            writer.Write(FormatVersion);

            // Write Session Metadata
            writer.Write(_session.Id.ToByteArray());
            writer.Write(_session.Name ?? string.Empty);
            writer.Write(_session.DateTime.ToBinary());
            writer.Write(_session.Duration);

            // Write Session InputRecords
            if (_session.Records == null) {
                throw new ArgumentNullException($"{nameof(_session)}.{nameof(_session.Records)}");
            }
            if (_session.Records.Length > MaxRecords) {
                throw new InvalidDataException("InputRecords count exceeds limit: " + $"{_session.Records.Length}");
            }
            writer.Write(_session.Records.Length);
            foreach (var record in _session.Records) {
                writer.Write(record.Sequence);
                writer.Write(record.ActionId.ToByteArray());
                writer.Write((int)record.Phase);
                writer.Write(record.Time);

                var inputData = record.InputData;
                if(inputData == null) {
                    throw new ArgumentNullException($"{nameof(record)}.{nameof(record.InputData)}");
                }
                if (inputData.Length > MaxPayloadSize) {
                    throw new InvalidDataException("Payload exceeds maximum size: " + $"{inputData.Length}");
                }

                writer.Write(inputData.Length);
                writer.Write(inputData);
            }
        }

        public static InputSession Deserialize(Stream _stream) {
            if (_stream == null) throw new ArgumentNullException(nameof(_stream));

            if (!_stream.CanRead) {
                throw new InvalidOperationException("Stream is not readable.");
            }
            if (!_stream.CanSeek) {
                throw new InvalidOperationException("Stream must support seeking.");
            }

            using BinaryReader reader = new(_stream, System.Text.Encoding.UTF8, true);

            // Read Header
            var magic = reader.ReadString();
            if (magic != Magic) {
                throw new InvalidDataException($"Invalid stream magic value: {magic}");
            }
            EnsureRemainingBytes(_stream, sizeof(int));
            var version = reader.ReadInt32();
            if (version != FormatVersion) {
                throw new InvalidDataException($"Unsupported telemetry format version: {version}");
            }

            // Read Session Metadata
            EnsureRemainingBytes(_stream, 16);
            var sessionId = new Guid(reader.ReadBytes(16));
            var sessionName = reader.ReadString();
            EnsureRemainingBytes(_stream, sizeof(long));
            var dateTime = DateTime.FromBinary(reader.ReadInt64());
            EnsureRemainingBytes(_stream, sizeof(double));
            var duration = reader.ReadDouble();

            // Read Session InputRecords
            EnsureRemainingBytes(_stream, sizeof(int));
            var recordCount = reader.ReadInt32();
            if (recordCount is < 0 or > MaxRecords) {
                throw new InvalidDataException("Invalid Input Record count: " + $"{recordCount}");
            }

            var records = new InputRecord[recordCount];
            for (var i = 0; i < recordCount; i++) {
                EnsureRemainingBytes(_stream, sizeof(uint));
                var sequence = reader.ReadUInt32();
                EnsureRemainingBytes(_stream, 16);
                var actionId = new Guid(reader.ReadBytes(16));
                EnsureRemainingBytes(_stream, sizeof(int));
                var phaseValue = reader.ReadInt32();
                if (!Enum.IsDefined(typeof(InputActionPhase), phaseValue)) {
                    throw new InvalidDataException("Invalid InputActionPhase value: " + $"{phaseValue}");
                }

                EnsureRemainingBytes(_stream, sizeof(double));
                var time = reader.ReadDouble();
                EnsureRemainingBytes(_stream, sizeof(int));
                var payloadLength = reader.ReadInt32();
                if (payloadLength is < 0 or > MaxPayloadSize) {
                    throw new InvalidDataException("Invalid payload length: " + $"{payloadLength}");
                }

                EnsureRemainingBytes(_stream, payloadLength);
                var inputData = reader.ReadBytes(payloadLength);

                records[i] = new InputRecord {
                    Sequence = sequence,
                    ActionId = actionId,
                    Phase = (InputActionPhase)phaseValue,
                    Time = time,
                    InputData = inputData
                };
            }
            
            // Recreate Session from read metadata
            InputSession session = new() {
                Id = sessionId,
                Name = sessionName,
                DateTime = dateTime,
                Duration = duration,
                Records = records
            };

            return session;
        }

        private static void EnsureRemainingBytes(Stream _stream, long _requiredBytes) {
            var remainingBytes = _stream.Length - _stream.Position;
            if (remainingBytes < _requiredBytes) {
                throw new InvalidDataException("Unexpected end of stream.");
            }
        }
    }
}