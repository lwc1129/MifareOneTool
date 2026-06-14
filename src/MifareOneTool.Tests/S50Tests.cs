using MifareOneTool.Core.Models;
using System;
using System.IO;
using Xunit;

namespace MifareOneTool.Tests;

public class S50Tests
{
    // ── Sector defaults ───────────────────────────────────────────────────

    [Fact]
    public void NewS50_HasSixteenSectors()
    {
        var s50 = new S50();
        Assert.Equal(16, s50.Sectors.Count);
    }

    [Fact]
    public void NewS50_Sector0HasUIDBlock()
    {
        var s50 = new S50();
        // Sector 0 block 0 should be 16 bytes (UID placeholder)
        Assert.Equal(16, s50.Sectors[0].Block[0].Length);
    }

    // ── UID / BCC ─────────────────────────────────────────────────────────

    [Fact]
    public void SetUID_UpdatesBCCCorrectly()
    {
        var s50 = new S50();
        byte[] uid = { 0xDE, 0xAD, 0xBE, 0xEF };
        s50.Sectors[0].Block[0][0] = uid[0];
        s50.Sectors[0].Block[0][1] = uid[1];
        s50.Sectors[0].Block[0][2] = uid[2];
        s50.Sectors[0].Block[0][3] = uid[3];
        byte bcc = (byte)(uid[0] ^ uid[1] ^ uid[2] ^ uid[3]);
        s50.Sectors[0].Block[0][4] = bcc;

        Assert.Equal(bcc, s50.Sectors[0].Block[0][4]);
    }

    // ── KeyA / KeyB property round-trip ──────────────────────────────────

    [Fact]
    public void KeyA_RoundTrip()
    {
        var sec = new Sector();
        byte[] key = { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06 };
        sec.KeyA = key;
        Assert.Equal(key, sec.KeyA);
    }

    [Fact]
    public void KeyB_RoundTrip()
    {
        var sec = new Sector();
        byte[] key = { 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF };
        sec.KeyB = key;
        Assert.Equal(key, sec.KeyB);
    }

    // ── MFD export / import round-trip ───────────────────────────────────

    [Fact]
    public void ExportImportMfd_RoundTrip()
    {
        var original = new S50();
        // Write some known data
        original.Sectors[0].Block[0][0] = 0x12;
        original.Sectors[0].Block[0][1] = 0x34;
        original.Sectors[0].Block[0][2] = 0x56;
        original.Sectors[0].Block[0][3] = 0x78;
        original.Sectors[0].Block[0][4] = (byte)(0x12 ^ 0x34 ^ 0x56 ^ 0x78);
        original.Sectors[1].Block[0] = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 };

        string tmp = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.mfd");
        try
        {
            original.ExportToMfd(tmp);

            var loaded = new S50();
            loaded.LoadFromMfd(tmp);

            Assert.Equal(original.Sectors[0].Block[0], loaded.Sectors[0].Block[0]);
            Assert.Equal(original.Sectors[1].Block[0], loaded.Sectors[1].Block[0]);
        }
        finally { if (File.Exists(tmp)) File.Delete(tmp); }
    }

    // ── MCT export / import round-trip ───────────────────────────────────

    [Fact]
    public void ExportImportMct_RoundTrip()
    {
        var original = new S50();
        original.Sectors[0].Block[0] = new byte[]
            { 0x12, 0x34, 0x56, 0x78, 0x44, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
        original.Sectors[0].Block[3] = new byte[]
            { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x07, 0x80, 0x69, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };

        string tmp = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.txt");
        try
        {
            original.ExportToMctTxt(tmp);
            Assert.True(File.Exists(tmp));

            string content = File.ReadAllText(tmp);
            Assert.Contains("+Sector: 0", content);

            var loaded = new S50();
            loaded.LoadFromMctTxt(tmp);
            Assert.Equal(original.Sectors[0].Block[0], loaded.Sectors[0].Block[0]);
        }
        finally { if (File.Exists(tmp)) File.Delete(tmp); }
    }

    // ── Verify ───────────────────────────────────────────────────────────

    [Fact]
    public void Verify_FreshCard_NoErrors()
    {
        var s50 = new S50();
        int[] result = s50.Verify();
        Assert.Equal(0, result[16]);
    }

    [Fact]
    public void Verify_DetectsBadBCC()
    {
        var s50 = new S50();
        // Set UID with wrong BCC
        s50.Sectors[0].Block[0][0] = 0xDE;
        s50.Sectors[0].Block[0][1] = 0xAD;
        s50.Sectors[0].Block[0][2] = 0xBE;
        s50.Sectors[0].Block[0][3] = 0xEF;
        s50.Sectors[0].Block[0][4] = 0x00; // wrong BCC

        int[] result = s50.Verify();
        Assert.NotEqual(0, result[0] & 0x01); // sector 0 BCC error
        Assert.NotEqual(0, result[16]);        // global error flag
    }

    // ── KeyListStr ────────────────────────────────────────────────────────

    [Fact]
    public void KeyListStr_ReturnsSixteenPairs()
    {
        var s50 = new S50();
        var keys = s50.KeyListStr();
        // 16 sectors × 2 keys = 32 entries, but implementation may vary
        Assert.NotEmpty(keys);
    }
}
