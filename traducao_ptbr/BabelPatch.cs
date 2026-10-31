// Grava um book.babel novo com as traducoes, no mesmo formato do Engine_BabelWriter do sdk
// (C# 5 para compilar via Add-Type no PowerShell 5.1).
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

public static class BabelPatch
{
    public const int ScratchLimit = 18022;

    static uint R(byte[] d, ref int p) { uint v = ((uint)d[p] << 24) | ((uint)d[p + 1] << 16) | ((uint)d[p + 2] << 8) | d[p + 3]; p += 4; return v; }
    static void W(Stream s, uint v) { s.WriteByte((byte)(v >> 24)); s.WriteByte((byte)(v >> 16)); s.WriteByte((byte)(v >> 8)); s.WriteByte((byte)v); }

    static byte[] Inflate(byte[] d, int p, int c, int u)
    {
        var o = new byte[u];
        using (var z = new DeflateStream(new MemoryStream(d, p + 2, c - 2), CompressionMode.Decompress))
        { int r = 0; while (r < u) { int k = z.Read(o, r, u - r); if (k <= 0) break; r += k; } }
        return o;
    }

    static byte[] Zlib(byte[] src)
    {
        var o = new MemoryStream(); o.WriteByte(0x78); o.WriteByte(0xDA);
        using (var z = new DeflateStream(o, CompressionLevel.Optimal, true)) z.Write(src, 0, src.Length);
        uint a = 1, b = 0; foreach (byte x in src) { a = (a + x) % 65521; b = (b + a) % 65521; }
        W(o, (b << 16) | a);
        return o.ToArray();
    }

    // Le o babel: chave -> texto, maior bloco e bytes que sobram depois dos blocos.
    public static SortedDictionary<uint, string> Read(byte[] d, out int largest, out byte[] tail)
    {
        int p = 0;
        if (R(d, ref p) != 0x5B010000u) throw new Exception("magic");
        int n = (int)R(d, ref p); var keys = new uint[n]; var bid = new uint[n]; var off = new uint[n];
        for (int i = 0; i < n; i++) { keys[i] = R(d, ref p); bid[i] = R(d, ref p); off[i] = R(d, ref p); }
        int bc = (int)R(d, ref p); var blocks = new Dictionary<uint, byte[]>(); largest = 0;
        for (int i = 0; i < bc; i++)
        {
            uint id = R(d, ref p); int c = (int)R(d, ref p); int u = (int)R(d, ref p);
            blocks[id] = Inflate(d, p, c, u); p += c; if (u > largest) largest = u;
        }
        tail = new byte[d.Length - p]; Array.Copy(d, p, tail, 0, tail.Length);
        var map = new SortedDictionary<uint, string>();
        for (int i = 0; i < n; i++)
        {
            byte[] b = blocks[bid[i]]; int q = (int)off[i]; int cnt = (int)R(b, ref q);
            var ch = new char[cnt - 1];
            for (int j = 0; j < cnt - 1; j++) ch[j] = (char)((b[q + j * 2] << 8) | b[q + j * 2 + 1]);
            map[keys[i]] = new string(ch);
        }
        return map;
    }

    public static byte[] Write(byte[] original, Dictionary<uint, string> overrides, List<string> errors)
    {
        int largest; byte[] tail;
        var strings = Read(original, out largest, out tail);
        foreach (var kv in overrides) strings[kv.Key] = kv.Value;
        int cap = Math.Min(Math.Max(largest, 1024), ScratchLimit);

        var recKey = new List<uint>(); var recBlock = new List<uint>(); var recOff = new List<uint>();
        var blocks = new List<byte[]>(); var cur = new MemoryStream();
        foreach (var kv in strings)
        {
            string v = kv.Value; var e = new byte[4 + (v.Length + 1) * 2]; int p = 0;
            var tmp = new MemoryStream(); W(tmp, (uint)(v.Length + 1)); Array.Copy(tmp.ToArray(), e, 4); p = 4;
            foreach (char c in v) { e[p++] = (byte)(c >> 8); e[p++] = (byte)c; }
            if (e.Length > cap) { errors.Add(string.Format("0x{0:X8}: {1} bytes > {2}", kv.Key, e.Length, cap)); continue; }
            if (cur.Length + e.Length > cap) { blocks.Add(cur.ToArray()); cur.SetLength(0); }
            recKey.Add(kv.Key); recBlock.Add((uint)blocks.Count); recOff.Add((uint)cur.Length);
            cur.Write(e, 0, e.Length);
        }
        if (cur.Length > 0) blocks.Add(cur.ToArray());

        var f = new MemoryStream();
        W(f, 0x5B010000u); W(f, (uint)recKey.Count);
        for (int i = 0; i < recKey.Count; i++) { W(f, recKey[i]); W(f, recBlock[i]); W(f, recOff[i]); }
        W(f, (uint)blocks.Count);
        for (int i = 0; i < blocks.Count; i++)
        {
            byte[] z = Zlib(blocks[i]);
            W(f, (uint)i); W(f, (uint)z.Length); W(f, (uint)blocks[i].Length); f.Write(z, 0, z.Length);
        }
        f.Write(tail, 0, tail.Length);
        return f.ToArray();
    }
}
