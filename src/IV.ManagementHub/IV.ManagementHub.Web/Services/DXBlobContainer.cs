using System.Security.Cryptography;
using System.Text;

namespace IV.ManagementHub.Web.Services
{
    internal static class DXBlobContainer
    {
        // MAGIC = "DXBLOB" (6 bytes)
        private static readonly byte[] Magic = Encoding.ASCII.GetBytes("DXBLOB");
        private const byte Version = 1;

        // TLV tags
        private const byte TagMime = 1;       // UTF-8 string
        private const byte TagFileName = 2;   // UTF-8 string
        private const byte TagSha256 = 3;     // 32 bytes
        private const byte TagCreatedUtc = 4; // Int64 unix millis

        public sealed class Meta
        {
            public string Mime { get; set; } = "application/octet-stream";
            public string? FileName { get; set; }
            public byte[]? Sha256 { get; set; }          // 32 bytes
            public long? CreatedUtcUnixMs { get; set; }
        }

        public static byte[] Pack(byte[] data, Meta meta, bool includeSha256 = true, bool includeCreatedUtc = true)
        {
            if (data is null) data = Array.Empty<byte>();
            meta ??= new Meta();

            var header = new MemoryStream();
            // TLV: mime
            WriteTlvUtf8(header, TagMime, meta.Mime ?? "application/octet-stream");

            // TLV: filename (optional)
            if (!string.IsNullOrWhiteSpace(meta.FileName))
                WriteTlvUtf8(header, TagFileName, meta.FileName!);

            // TLV: createdUtc (optional)
            if (includeCreatedUtc)
            {
                var unixMs = meta.CreatedUtcUnixMs ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                WriteTlvInt64(header, TagCreatedUtc, unixMs);
            }

            // TLV: sha256 (optional)
            if (includeSha256)
            {
                var hash = meta.Sha256;
                if (hash is null || hash.Length != 32)
                {
                    using var sha = SHA256.Create();
                    hash = sha.ComputeHash(data);
                }
                WriteTlvBytes(header, TagSha256, hash);
            }

            var headerBytes = header.ToArray();

            using var ms = new MemoryStream(capacity: Magic.Length + 1 + 4 + headerBytes.Length + data.Length);
            ms.Write(Magic, 0, Magic.Length);
            ms.WriteByte(Version);
            WriteUInt32LE(ms, (uint)headerBytes.Length);
            ms.Write(headerBytes, 0, headerBytes.Length);
            ms.Write(data, 0, data.Length);
            return ms.ToArray();
        }

        /// <summary>
        /// Если это DXBLOB контейнер — распакует. Если нет (raw bytes) — вернёт false.
        /// </summary>
        public static bool TryUnpack(byte[] blob, out Meta meta, out byte[] data)
        {
            meta = new Meta();
            data = Array.Empty<byte>();

            if (blob is null || blob.Length < Magic.Length + 1 + 4)
                return false;

            // magic
            for (int i = 0; i < Magic.Length; i++)
                if (blob[i] != Magic[i]) return false;

            int offset = Magic.Length;
            var ver = blob[offset++];
            if (ver != Version) return false;

            var headerLen = ReadUInt32LE(blob, ref offset);
            if (headerLen > (uint)(blob.Length - offset)) return false;

            int headerStart = offset;
            int headerEnd = headerStart + (int)headerLen;
            offset = headerStart;

            // TLV parse
            while (offset < headerEnd)
            {
                if (offset + 1 + 4 > headerEnd) return false;
                byte tag = blob[offset++];
                uint len = ReadUInt32LE(blob, ref offset);
                if (len > (uint)(headerEnd - offset)) return false;

                var valueSpan = new ReadOnlySpan<byte>(blob, offset, (int)len);
                offset += (int)len;

                switch (tag)
                {
                    case TagMime:
                        meta.Mime = Encoding.UTF8.GetString(valueSpan);
                        break;
                    case TagFileName:
                        meta.FileName = Encoding.UTF8.GetString(valueSpan);
                        break;
                    case TagSha256:
                        meta.Sha256 = valueSpan.ToArray();
                        break;
                    case TagCreatedUtc:
                        if (len == 8)
                            meta.CreatedUtcUnixMs = ReadInt64LE(valueSpan);
                        break;
                }
            }

            // rest is data
            int dataOffset = headerEnd;
            int dataLen = blob.Length - dataOffset;
            data = dataLen > 0 ? CopyRange(blob, dataOffset, dataLen) : Array.Empty<byte>();
            return true;
        }

        // ---------- helpers ----------
        private static void WriteTlvUtf8(Stream s, byte tag, string value)
            => WriteTlvBytes(s, tag, Encoding.UTF8.GetBytes(value ?? ""));

        private static void WriteTlvInt64(Stream s, byte tag, long value)
        {
            Span<byte> buf = stackalloc byte[8];
            WriteInt64LE(buf, value);
            WriteTlvBytes(s, tag, buf.ToArray());
        }

        private static void WriteTlvBytes(Stream s, byte tag, byte[] bytes)
        {
            s.WriteByte(tag);
            WriteUInt32LE(s, (uint)(bytes?.Length ?? 0));
            if (bytes is { Length: > 0 })
                s.Write(bytes, 0, bytes.Length);
        }

        private static void WriteUInt32LE(Stream s, uint v)
        {
            Span<byte> b = stackalloc byte[4];
            b[0] = (byte)(v);
            b[1] = (byte)(v >> 8);
            b[2] = (byte)(v >> 16);
            b[3] = (byte)(v >> 24);
            s.Write(b);
        }

        private static uint ReadUInt32LE(byte[] src, ref int offset)
        {
            uint v = (uint)(src[offset]
                | (src[offset + 1] << 8)
                | (src[offset + 2] << 16)
                | (src[offset + 3] << 24));
            offset += 4;
            return v;
        }

        private static long ReadInt64LE(ReadOnlySpan<byte> s)
            => (long)(
                ((ulong)s[0]) |
                ((ulong)s[1] << 8) |
                ((ulong)s[2] << 16) |
                ((ulong)s[3] << 24) |
                ((ulong)s[4] << 32) |
                ((ulong)s[5] << 40) |
                ((ulong)s[6] << 48) |
                ((ulong)s[7] << 56));

        private static void WriteInt64LE(Span<byte> dst, long v)
        {
            ulong u = (ulong)v;
            dst[0] = (byte)u;
            dst[1] = (byte)(u >> 8);
            dst[2] = (byte)(u >> 16);
            dst[3] = (byte)(u >> 24);
            dst[4] = (byte)(u >> 32);
            dst[5] = (byte)(u >> 40);
            dst[6] = (byte)(u >> 48);
            dst[7] = (byte)(u >> 56);
        }

        private static byte[] CopyRange(byte[] src, int offset, int len)
        {
            var dst = new byte[len];
            Buffer.BlockCopy(src, offset, dst, 0, len);
            return dst;
        }
    }
}
