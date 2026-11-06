using System;
using System.Buffers.Binary;
using System.IO;
using System.Text;
using System.Text.Json;

namespace BilliardsGame.Network.Protocol
{
    /// <summary>
    /// Implements compact binary message framing over raw network byte streams.
    /// Prefixes each payload with a 4-byte big-endian integer length field followed by a 1-byte packet type discriminator,
    /// followed by UTF-8 encoded JSON payload content. Guarantees deterministic TCP framing against fragmentation.
    /// </summary>
    public static class PacketSerializer
    {
        // Standard shared JSON options optimized for compact transmission.
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        /// <summary>
        /// Serializes a high-level typed packet into a framed network byte array ready for direct TCP transmission.
        /// </summary>
        /// <typeparam name="T">Concrete packet payload type.</typeparam>
        /// <param name="type">Explicit packet type discriminator.</param>
        /// <param name="payload">Packet data contract instance.</param>
        /// <returns>Framed byte array containing length header, type byte, and UTF-8 payload.</returns>
        public static byte[] Serialize<T>(NetworkPacketType type, T payload)
        {
            // Convert strongly typed payload contract into compact UTF-8 JSON bytes.
            byte[] payloadBytes = JsonSerializer.SerializeToUtf8Bytes(payload, JsonOptions);
            
            // Frame length covers the 1-byte type identifier plus the full payload content length.
            int frameDataLength = 1 + payloadBytes.Length;
            
            // Total buffer equals 4 bytes length header + frameDataLength.
            byte[] output = new byte[4 + frameDataLength];
            
            // Write length header in network byte order (Big Endian).
            BinaryPrimitives.WriteInt32BigEndian(output.AsSpan(0, 4), frameDataLength);
            
            // Write packet type discriminator byte.
            output[4] = (byte)type;
            
            // Copy payload bytes into the framed packet structure.
            Buffer.BlockCopy(payloadBytes, 0, output, 5, payloadBytes.Length);
            
            return output;
        }

        /// <summary>
        /// Attempts to parse and deserialize a complete packet frame from a raw accumulating stream buffer.
        /// Returns false if the buffer contains fewer bytes than indicated by the leading frame length header.
        /// </summary>
        /// <param name="buffer">Accumulated receive byte buffer.</param>
        /// <param name="bytesAvailable">Number of valid bytes currently present in the buffer.</param>
        /// <param name="packetType">Parsed packet type discriminator.</param>
        /// <param name="packet">Deserialized packet object, or null on failure.</param>
        /// <param name="bytesConsumed">Total bytes consumed from buffer including the 4-byte header.</param>
        /// <returns>True if a complete valid frame was parsed; otherwise false.</returns>
        public static bool TryDeserialize(
            byte[] buffer,
            int bytesAvailable,
            out NetworkPacketType packetType,
            out object? packet,
            out int bytesConsumed)
        {
            packetType = default;
            packet = null;
            bytesConsumed = 0;

            // Insufficient bytes to read the 4-byte frame length header.
            if (bytesAvailable < 4)
            {
                return false;
            }

            // Extract frame data length from Big Endian header.
            int frameDataLength = BinaryPrimitives.ReadInt32BigEndian(buffer.AsSpan(0, 4));

            // Validate against anomalous or malicious frame lengths.
            if (frameDataLength <= 1 || frameDataLength > 1024 * 1024)
            {
                throw new InvalidDataException($"Invalid network packet frame length detected: {frameDataLength} bytes.");
            }

            // Verify if complete frame has arrived in the accumulating stream buffer.
            int totalFrameLength = 4 + frameDataLength;
            if (bytesAvailable < totalFrameLength)
            {
                return false;
            }

            // Parse packet type discriminator.
            packetType = (NetworkPacketType)buffer[4];
            
            // Extract UTF-8 payload slice.
            int payloadLength = frameDataLength - 1;
            ReadOnlySpan<byte> payloadSpan = buffer.AsSpan(5, payloadLength);

            // Dynamically deserialize payload into its concrete data contract type.
            packet = packetType switch
            {
                NetworkPacketType.Handshake => JsonSerializer.Deserialize<HandshakePacket>(payloadSpan, JsonOptions),
                NetworkPacketType.AimSync => JsonSerializer.Deserialize<AimSyncPacket>(payloadSpan, JsonOptions),
                NetworkPacketType.ShotExecution => JsonSerializer.Deserialize<ShotExecutionPacket>(payloadSpan, JsonOptions),
                NetworkPacketType.PhysicsSync => JsonSerializer.Deserialize<PhysicsSyncPacket>(payloadSpan, JsonOptions),
                NetworkPacketType.BallInHand => JsonSerializer.Deserialize<BallInHandPacket>(payloadSpan, JsonOptions),
                NetworkPacketType.TurnState => JsonSerializer.Deserialize<TurnStatePacket>(payloadSpan, JsonOptions),
                NetworkPacketType.Disconnect => JsonSerializer.Deserialize<DisconnectPacket>(payloadSpan, JsonOptions),
                NetworkPacketType.RelayControl => JsonSerializer.Deserialize<RelayControlPacket>(payloadSpan, JsonOptions),
                NetworkPacketType.CallPocket => JsonSerializer.Deserialize<CallPocketPacket>(payloadSpan, JsonOptions),
                _ => null
            };

            // Advance stream consumption by the full frame size even if deserialization returned null.
            // Returning true indicates a complete frame was processed, preventing buffer stalling on unknown payloads.
            bytesConsumed = totalFrameLength;
            return true;
        }
    }
}
