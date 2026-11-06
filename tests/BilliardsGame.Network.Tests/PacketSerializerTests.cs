using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using BilliardsGame.Interfaces.Enums;
using BilliardsGame.Network.Protocol;
using Xunit;

namespace BilliardsGame.Network.Tests
{
    /// <summary>
    /// Comprehensive test suite verifying deterministic packet framing, serialization, and deserialization across network byte streams.
    /// </summary>
    public class PacketSerializerTests
    {
        /// <summary>
        /// Validates that HandshakePacket serializes and deserializes accurately preserving player identity and room codes.
        /// </summary>
        [Fact]
        public void SerializeAndDeserialize_HandshakePacket_PreservesValues()
        {
            var original = new HandshakePacket(2, "Player 2", "54321");
            byte[] bytes = PacketSerializer.Serialize(NetworkPacketType.Handshake, original);

            bool success = PacketSerializer.TryDeserialize(bytes, bytes.Length, out var type, out var packet, out int consumed);

            Assert.True(success);
            Assert.Equal(NetworkPacketType.Handshake, type);
            Assert.Equal(bytes.Length, consumed);

            var result = Assert.IsType<HandshakePacket>(packet);
            Assert.Equal(2, result.AssignedPlayerId);
            Assert.Equal("Player 2", result.PlayerName);
            Assert.Equal("54321", result.RoomCode);
        }

        /// <summary>
        /// Validates that RelayControlPacket serializes and deserializes accurately for all relay action states and messages.
        /// </summary>
        [Fact]
        public void SerializeAndDeserialize_RelayControlPacket_PreservesValues()
        {
            var original = new RelayControlPacket
            {
                Action = RelayAction.RoomCreated,
                RoomCode = "84920",
                Message = "Room ready"
            };
            byte[] bytes = PacketSerializer.Serialize(NetworkPacketType.RelayControl, original);

            bool success = PacketSerializer.TryDeserialize(bytes, bytes.Length, out var type, out var packet, out int consumed);

            Assert.True(success);
            Assert.Equal(NetworkPacketType.RelayControl, type);
            Assert.Equal(bytes.Length, consumed);

            var result = Assert.IsType<RelayControlPacket>(packet);
            Assert.Equal(RelayAction.RoomCreated, result.Action);
            Assert.Equal("84920", result.RoomCode);
            Assert.Equal("Room ready", result.Message);
        }

        /// <summary>
        /// Validates that RelayControlPacket with default values round-trips without null reference exceptions.
        /// </summary>
        [Fact]
        public void SerializeAndDeserialize_RelayControlPacket_DefaultValues_RoundTrips()
        {
            var original = new RelayControlPacket
            {
                Action = RelayAction.CreateRoom
            };
            byte[] bytes = PacketSerializer.Serialize(NetworkPacketType.RelayControl, original);

            bool success = PacketSerializer.TryDeserialize(bytes, bytes.Length, out var type, out var packet, out int consumed);

            Assert.True(success);
            Assert.Equal(NetworkPacketType.RelayControl, type);

            var result = Assert.IsType<RelayControlPacket>(packet);
            Assert.Equal(RelayAction.CreateRoom, result.Action);
            Assert.Equal(string.Empty, result.RoomCode);
            Assert.Equal(string.Empty, result.Message);
        }

        /// <summary>
        /// Validates that AimSyncPacket preserves aiming vectors, cursor world coordinates, and charging status without precision loss.
        /// </summary>
        [Fact]
        public void SerializeAndDeserialize_AimSyncPacket_PreservesValues()
        {
            var original = new AimSyncPacket(0.7071f, -0.7071f, 450.5f, 280.25f, 0.65f, true);
            byte[] bytes = PacketSerializer.Serialize(NetworkPacketType.AimSync, original);

            bool success = PacketSerializer.TryDeserialize(bytes, bytes.Length, out var type, out var packet, out int consumed);

            Assert.True(success);
            Assert.Equal(NetworkPacketType.AimSync, type);
            Assert.Equal(bytes.Length, consumed);

            var result = Assert.IsType<AimSyncPacket>(packet);
            Assert.Equal(0.7071f, result.CueDirectionX, 4);
            Assert.Equal(-0.7071f, result.CueDirectionY, 4);
            Assert.Equal(450.5f, result.MouseWorldX, 2);
            Assert.Equal(280.25f, result.MouseWorldY, 2);
            Assert.Equal(0.65f, result.Power, 4);
            Assert.True(result.IsCharging);
        }

        /// <summary>
        /// Validates that ShotExecutionPacket correctly frames linear impulse and strike directional vectors.
        /// </summary>
        [Fact]
        public void SerializeAndDeserialize_ShotExecutionPacket_PreservesValues()
        {
            var original = new ShotExecutionPacket(350.0f, -120.0f, 0.9f, -0.1f, 0.75f);
            byte[] bytes = PacketSerializer.Serialize(NetworkPacketType.ShotExecution, original);

            bool success = PacketSerializer.TryDeserialize(bytes, bytes.Length, out var type, out var packet, out int consumed);

            Assert.True(success);
            Assert.Equal(NetworkPacketType.ShotExecution, type);
            Assert.Equal(bytes.Length, consumed);

            var result = Assert.IsType<ShotExecutionPacket>(packet);
            Assert.Equal(350.0f, result.ImpulseX, 2);
            Assert.Equal(-120.0f, result.ImpulseY, 2);
            Assert.Equal(0.9f, result.CueDirectionX, 2);
            Assert.Equal(-0.1f, result.CueDirectionY, 2);
            Assert.Equal(0.75f, result.Power, 2);
        }

        /// <summary>
        /// Validates that PhysicsSyncPacket preserves an entire array of active kinematic ball snapshots and rest flags.
        /// </summary>
        [Fact]
        public void SerializeAndDeserialize_PhysicsSyncPacket_PreservesValues()
        {
            var balls = new List<BallSnapshot>
            {
                new BallSnapshot(0, 100.5f, 200.0f, 15.0f, -5.0f),
                new BallSnapshot(8, 300.0f, 400.0f, 0.0f, 0.0f),
                new BallSnapshot(3, 500.25f, 150.75f, -2.5f, 8.0f)
            };
            var original = new PhysicsSyncPacket(balls, false);
            byte[] bytes = PacketSerializer.Serialize(NetworkPacketType.PhysicsSync, original);

            bool success = PacketSerializer.TryDeserialize(bytes, bytes.Length, out var type, out var packet, out int consumed);

            Assert.True(success);
            Assert.Equal(NetworkPacketType.PhysicsSync, type);
            Assert.Equal(bytes.Length, consumed);

            var result = Assert.IsType<PhysicsSyncPacket>(packet);
            Assert.False(result.AreAllAtRest);
            Assert.Equal(3, result.Balls.Count);
            Assert.Equal(0, result.Balls[0].Id);
            Assert.Equal(100.5f, result.Balls[0].X, 2);
            Assert.Equal(8, result.Balls[1].Id);
            Assert.Equal(3, result.Balls[2].Id);
        }

        /// <summary>
        /// Validates that BallInHandPacket accurately encodes candidate placement positions and confirmation drops.
        /// </summary>
        [Fact]
        public void SerializeAndDeserialize_BallInHandPacket_PreservesValues()
        {
            var original = new BallInHandPacket(240f, 320f, true);
            byte[] bytes = PacketSerializer.Serialize(NetworkPacketType.BallInHand, original);

            bool success = PacketSerializer.TryDeserialize(bytes, bytes.Length, out var type, out var packet, out int consumed);

            Assert.True(success);
            Assert.Equal(NetworkPacketType.BallInHand, type);
            Assert.Equal(bytes.Length, consumed);

            var result = Assert.IsType<BallInHandPacket>(packet);
            Assert.Equal(240f, result.X);
            Assert.Equal(320f, result.Y);
            Assert.True(result.IsPlaced);
        }

        /// <summary>
        /// Validates that TurnStatePacket serializes complete domain progression metrics, score counters, and notification semantics.
        /// </summary>
        [Fact]
        public void SerializeAndDeserialize_TurnStatePacket_PreservesValues()
        {
            var original = new TurnStatePacket(
                GameState.BallInHand,
                2,
                3,
                5,
                BallType.Solid,
                BallType.Striped,
                "FOUL: Time Out",
                NotificationType.Foul,
                null);

            byte[] bytes = PacketSerializer.Serialize(NetworkPacketType.TurnState, original);

            bool success = PacketSerializer.TryDeserialize(bytes, bytes.Length, out var type, out var packet, out int consumed);

            Assert.True(success);
            Assert.Equal(NetworkPacketType.TurnState, type);
            Assert.Equal(bytes.Length, consumed);

            var result = Assert.IsType<TurnStatePacket>(packet);
            Assert.Equal(GameState.BallInHand, result.State);
            Assert.Equal(2, result.ActivePlayerId);
            Assert.Equal(3, result.Player1Score);
            Assert.Equal(5, result.Player2Score);
            Assert.Equal(BallType.Solid, result.Player1AssignedType);
            Assert.Equal(BallType.Striped, result.Player2AssignedType);
            Assert.Equal("FOUL: Time Out", result.NotificationMessage);
            Assert.Equal(NotificationType.Foul, result.NotificationType);
            Assert.Null(result.WinnerId);
        }

        /// <summary>
        /// Validates that DisconnectPacket properly serializes human-readable exit explanations.
        /// </summary>
        [Fact]
        public void SerializeAndDeserialize_DisconnectPacket_PreservesValues()
        {
            var original = new DisconnectPacket("Player surrendered match.");
            byte[] bytes = PacketSerializer.Serialize(NetworkPacketType.Disconnect, original);

            bool success = PacketSerializer.TryDeserialize(bytes, bytes.Length, out var type, out var packet, out int consumed);

            Assert.True(success);
            Assert.Equal(NetworkPacketType.Disconnect, type);
            Assert.Equal(bytes.Length, consumed);

            var result = Assert.IsType<DisconnectPacket>(packet);
            Assert.Equal("Player surrendered match.", result.Reason);
        }

        /// <summary>
        /// Simulates TCP stream fragmentation where only partial header/payload bytes arrive, verifying that deserializer reports incomplete.
        /// </summary>
        [Fact]
        public void TryDeserialize_WhenBufferHasIncompleteFrame_ReturnsFalse()
        {
            var original = new HandshakePacket(1, "Player 1", "99999");
            byte[] fullBytes = PacketSerializer.Serialize(NetworkPacketType.Handshake, original);

            // Simulate arriving slice having only 3 bytes (incomplete length header).
            bool success = PacketSerializer.TryDeserialize(fullBytes, 3, out _, out _, out int consumed);
            Assert.False(success);
            Assert.Equal(0, consumed);

            // Simulate arriving slice having length header but only partial payload.
            bool success2 = PacketSerializer.TryDeserialize(fullBytes, fullBytes.Length - 5, out _, out _, out int consumed2);
            Assert.False(success2);
            Assert.Equal(0, consumed2);
        }

        /// <summary>
        /// Simulates TCP stream coalescing where two separate packets are delivered in a single buffer, verifying sequential parsing.
        /// </summary>
        [Fact]
        public void TryDeserialize_WhenBufferContainsMultipleFrames_ParsesSequentially()
        {
            var p1 = new HandshakePacket(1, "Host", "12345");
            var p2 = new DisconnectPacket("Match ended.");

            byte[] b1 = PacketSerializer.Serialize(NetworkPacketType.Handshake, p1);
            byte[] b2 = PacketSerializer.Serialize(NetworkPacketType.Disconnect, p2);

            // Concatenate both packet frames into single contiguous byte buffer.
            byte[] combined = new byte[b1.Length + b2.Length];
            Buffer.BlockCopy(b1, 0, combined, 0, b1.Length);
            Buffer.BlockCopy(b2, 0, combined, b1.Length, b2.Length);

            // Parse first packet.
            bool s1 = PacketSerializer.TryDeserialize(combined, combined.Length, out var t1, out var obj1, out int c1);
            Assert.True(s1);
            Assert.Equal(NetworkPacketType.Handshake, t1);
            Assert.Equal(b1.Length, c1);
            Assert.IsType<HandshakePacket>(obj1);

            // Parse second packet by providing remaining slice.
            byte[] secondSlice = new byte[combined.Length - c1];
            Buffer.BlockCopy(combined, c1, secondSlice, 0, secondSlice.Length);

            bool s2 = PacketSerializer.TryDeserialize(secondSlice, secondSlice.Length, out var t2, out var obj2, out int c2);
            Assert.True(s2);
            Assert.Equal(NetworkPacketType.Disconnect, t2);
            Assert.Equal(b2.Length, c2);
            Assert.IsType<DisconnectPacket>(obj2);
        }

        /// <summary>
        /// Verifies that frame lengths exceeding maximum safety threshold trigger an InvalidDataException.
        /// </summary>
        [Fact]
        public void TryDeserialize_ExcessiveFrameLength_ThrowsInvalidDataException()
        {
            byte[] corruptedBuffer = new byte[8];
            // Write length of 50 MB into header.
            System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(corruptedBuffer.AsSpan(0, 4), 50 * 1024 * 1024);

            Assert.Throws<InvalidDataException>(() =>
            {
                PacketSerializer.TryDeserialize(corruptedBuffer, corruptedBuffer.Length, out _, out _, out _);
            });
        }

        /// <summary>
        /// Validates that encountering an unknown packet type consumes the frame and returns true with a null packet payload,
        /// ensuring the stream reader advances its buffer rather than entering an infinite parsing loop.
        /// </summary>
        [Fact]
        public void TryDeserialize_UnknownPacketType_ConsumesFrameAndReturnsNullPayload()
        {
            // Construct a valid length frame (4 bytes header + 1 byte unknown type + 4 bytes dummy payload)
            byte[] frame = new byte[9];
            System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(frame.AsSpan(0, 4), 5); // frameDataLength = 5
            frame[4] = 255; // Unknown packet type discriminator

            bool success = PacketSerializer.TryDeserialize(frame, frame.Length, out var type, out var packet, out int consumed);

            Assert.True(success);
            Assert.Equal((NetworkPacketType)255, type);
            Assert.Null(packet);
            Assert.Equal(9, consumed);
        }
    }
}
