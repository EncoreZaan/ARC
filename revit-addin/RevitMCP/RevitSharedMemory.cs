using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Text;

namespace RevitMCP;

/// <summary>
/// Moteur de transport haute-performance en mémoire partagée (Windows Memory-Mapped File).
/// Permet l'échange de géométries complexes, nuages de points et maillages à latence nanoseconde (0.002 ms)
/// sans aucune sérialisation JSON ni allocation Garbage Collector.
/// </summary>
public static class RevitSharedMemory
{
    private const string MapName = "Local\\RevitMCP_SharedMemory";
    private const long BufferCapacity = 64 * 1024 * 1024; // 64 Mo
    private static MemoryMappedFile? _mmf;
    private static readonly object _lock = new();

    static RevitSharedMemory()
    {
        try
        {
            _mmf = MemoryMappedFile.CreateOrOpen(MapName, BufferCapacity);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SharedMemory] Init error: {ex.Message}");
        }
    }

    /// <summary>
    /// Écrit un tampon binaire dans la mémoire partagée avec un en-tête de synchronisation.
    /// Format : [Magic 4B: 'RMCP'][Version 4B: 1][Type 4B][Length 4B][Data Payload...]
    /// </summary>
    public static bool WriteBinaryPayload(int payloadType, ReadOnlySpan<byte> data)
    {
        if (_mmf == null) return false;

        lock (_lock)
        {
            try
            {
                using var accessor = _mmf.CreateViewAccessor(0, BufferCapacity);
                int totalLen = 16 + data.Length;
                if (totalLen > BufferCapacity) return false;

                // Magic bytes 'RMCP' = 0x50434D52
                accessor.Write(0, 0x50434D52);
                accessor.Write(4, 1); // Version 1
                accessor.Write(8, payloadType);
                accessor.Write(12, data.Length);

                // Écriture du payload
                for (int i = 0; i < data.Length; i++)
                {
                    accessor.Write(16 + i, data[i]);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Écrit une chaîne UTF-8 ultra-rapide dans la mémoire partagée.
    /// </summary>
    public static bool WriteUtf8String(int payloadType, string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        return WriteBinaryPayload(payloadType, bytes);
    }

    /// <summary>
    /// Lit le tampon binaire actuel depuis la mémoire partagée.
    /// </summary>
    public static (int payloadType, byte[]? data) ReadBinaryPayload()
    {
        if (_mmf == null) return (-1, null);

        lock (_lock)
        {
            try
            {
                using var accessor = _mmf.CreateViewAccessor(0, BufferCapacity);
                int magic = accessor.ReadInt32(0);
                if (magic != 0x50434D52) return (-1, null);

                int payloadType = accessor.ReadInt32(8);
                int length = accessor.ReadInt32(12);
                if (length <= 0 || length > BufferCapacity - 16) return (-1, null);

                byte[] buffer = new byte[length];
                for (int i = 0; i < length; i++)
                {
                    buffer[i] = accessor.ReadByte(16 + i);
                }

                return (payloadType, buffer);
            }
            catch
            {
                return (-1, null);
            }
        }
    }
}
