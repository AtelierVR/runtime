using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using api.nox.relay.types;
using Nox.CCK.Utils;

namespace api.nox.relay.connection
{
    public class ClientMultiPacketSession
    {
        public ushort SessionId { get; set; }
        public ushort TotalPackets { get; set; }
        public uint TotalSize { get; set; }
        public Dictionary<ushort, byte[]> Packets { get; set; } = new Dictionary<ushort, byte[]>();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public ResponseType OriginalType { get; set; }
        public ushort OriginalState { get; set; }
        
        public bool IsComplete => Packets.Count == TotalPackets;
        
        public byte[] GetMergedData()
        {
            if (!IsComplete) return null;
            
            var result = new byte[TotalSize];
            uint offset = 0;
            
            for (ushort i = 0; i < TotalPackets; i++)
            {
                if (Packets.TryGetValue(i, out var packetData))
                {
                    Array.Copy(packetData, 0, result, offset, packetData.Length);
                    offset += (uint)packetData.Length;
                }
                else
                {
                    return null; // Missing packet
                }
            }
            
            return result;
        }
    }
    
    public static class ClientMultiPacketManager
    {
        private static readonly ConcurrentDictionary<ushort, ClientMultiPacketSession> Sessions = new();
        private static readonly object CleanupLock = new object();
        private static DateTime LastCleanup = DateTime.UtcNow;
        private const int SessionTimeoutSeconds = 30;
        
        public static void CleanupExpiredSessions()
        {
            lock (CleanupLock)
            {
                if (DateTime.UtcNow.Subtract(LastCleanup).TotalSeconds < 5) return;
                LastCleanup = DateTime.UtcNow;
                
                var expiredKeys = Sessions
                    .Where(kvp => DateTime.UtcNow.Subtract(kvp.Value.CreatedAt).TotalSeconds > SessionTimeoutSeconds)
                    .Select(kvp => kvp.Key)
                    .ToList();
                
                foreach (var key in expiredKeys)
                {
                    Sessions.TryRemove(key, out _);
                    Logger.LogWarning($"Client multipacket session {key} expired and was removed");
                }
            }
        }
        
        public static void StartSession(ushort sessionId, ushort totalPackets, uint totalSize, ResponseType originalType, ushort originalState)
        {
            CleanupExpiredSessions();
            
            var session = new ClientMultiPacketSession
            {
                SessionId = sessionId,
                TotalPackets = totalPackets,
                TotalSize = totalSize,
                OriginalType = originalType,
                OriginalState = originalState
            };
            
            Sessions[sessionId] = session;
            Logger.LogDebug($"Started client multipacket session {sessionId} with {totalPackets} packets, total size {totalSize}");
        }
        
        public static bool AddPacket(ushort sessionId, ushort packetIndex, byte[] data)
        {
            if (!Sessions.TryGetValue(sessionId, out var session))
            {
                Logger.LogWarning($"Received packet for unknown client session {sessionId}");
                return false;
            }
            
            if (packetIndex >= session.TotalPackets)
            {
                Logger.LogWarning($"Invalid packet index {packetIndex} for client session {sessionId} (max: {session.TotalPackets - 1})");
                return false;
            }
            
            session.Packets[packetIndex] = data;
            Logger.LogDebug($"Added packet {packetIndex}/{session.TotalPackets - 1} to client session {sessionId}");
            
            return true;
        }
        
        public static ClientMultiPacketSession CompleteSession(ushort sessionId)
        {
            if (!Sessions.TryRemove(sessionId, out var session))
            {
                Logger.LogWarning($"Cannot complete unknown client session {sessionId}");
                return null;
            }
            
            if (!session.IsComplete)
            {
                Logger.LogWarning($"Attempted to complete incomplete client session {sessionId} ({session.Packets.Count}/{session.TotalPackets})");
                return null;
            }
            
            Logger.LogDebug($"Completed client multipacket session {sessionId}");
            return session;
        }
    }
}
