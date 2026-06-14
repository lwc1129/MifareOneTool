using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace MifareOneTool.Core.Services
{
    /// <summary>
    /// Runs libnfc tools as sub-processes and streams output via IProgress.
    /// Platform-aware: uses DesktopNfcToolRunner to resolve binary paths.
    /// </summary>
    public class NfcService
    {
        private readonly DesktopNfcToolRunner _runner;
        private int _busy = 0; // 0=idle, 1=busy (Interlocked)

        public bool IsBusy => _busy == 1;

        public NfcService(DesktopNfcToolRunner? runner = null)
        {
            _runner = runner ?? new DesktopNfcToolRunner();
        }

        // ─────────────────────────────────────────────────────
        // Device scanning
        // ─────────────────────────────────────────────────────

        /// <summary>
        /// Runs nfc-scan-device and returns the list of device connstrings found.
        /// </summary>
        public async Task<List<string>> ScanDevicesAsync(
            IProgress<string> progress, CancellationToken ct = default)
        {
            var devices = new List<string>();
            var pattern = PlatformInfo.DeviceConnstringPattern;

            await RunToolAsync("nfc-scan-device", "", progress, ct,
                onLine: line =>
                {
                    if (string.IsNullOrWhiteSpace(line)) return;
                    var m = Regex.Match(line.Trim(), pattern);
                    if (!m.Success) return;
                    string connstring = m.Value;
                    // Ensure baud rate is present (some libnfc versions omit it)
                    if (!Regex.IsMatch(connstring, @":\d+$"))
                        connstring += ":115200";
                    if (!devices.Contains(connstring))
                        devices.Add(connstring);
                });

            return devices;
        }

        // ─────────────────────────────────────────────────────
        // Read card
        // ─────────────────────────────────────────────────────

        /// <summary>
        /// nfc-mfclassic r [keyMode] u "output.mfd" ["key.mfd" f]
        /// keyMode: A | B | C (C = smart KeyABN)
        /// </summary>
        public async Task<int> ReadCardAsync(
            string outputMfd, string keyMode, string? keyMfd,
            IProgress<string> progress, CancellationToken ct = default)
        {
            string args = $"r {keyMode} u \"{outputMfd}\"";
            if (!string.IsNullOrEmpty(keyMfd)) args += $" \"{keyMfd}\" f";
            return await RunToolAsync("nfc-mfclassic", args, progress, ct);
        }

        // ─────────────────────────────────────────────────────
        // Write card
        // ─────────────────────────────────────────────────────

        public async Task<int> WriteCardAsync(
            string sourceMfd, string keyMode, string? keyMfd,
            IProgress<string> progress, CancellationToken ct = default)
        {
            string args = $"w {keyMode} u \"{sourceMfd}\"";
            if (!string.IsNullOrEmpty(keyMfd)) args += $" \"{keyMfd}\" f";
            return await RunToolAsync("nfc-mfclassic", args, progress, ct);
        }

        // ─────────────────────────────────────────────────────
        // mfoc crack
        // ─────────────────────────────────────────────────────

        public async Task<int> MfocCrackAsync(
            string outputMfd, string extraKeys,
            IProgress<string> progress, CancellationToken ct = default)
        {
            string args = $"{extraKeys} -O \"{outputMfd}\"".Trim();
            return await RunToolAsync("mfoc", args, progress, ct);
        }

        // ─────────────────────────────────────────────────────
        // UID card operations
        // ─────────────────────────────────────────────────────

        public async Task<int> WriteUidAsync(
            string uid, IProgress<string> progress, CancellationToken ct = default)
        {
            // uid must be 8 hex chars; tool expects uid + rest of block 0
            string block0Tail = "2B0804006263646566676869";
            string args = $"{uid}{block0Tail}";
            return await RunToolAsync("nfc-mfsetuid", args, progress, ct);
        }

        public async Task<int> ResetUidAsync(
            IProgress<string> progress, CancellationToken ct = default)
        {
            byte[] uid = new byte[4];
            RandomNumberGenerator.Fill(uid);
            string uidHex = Utils.Hex2Str(uid);
            return await WriteUidAsync(uidHex, progress, ct);
        }

        public async Task<int> FormatUidCardAsync(
            IProgress<string> progress, CancellationToken ct = default)
        {
            byte[] uid = new byte[4];
            RandomNumberGenerator.Fill(uid);
            string uidHex = Utils.Hex2Str(uid);
            string block0Tail = "2B0804006263646566676869";
            string args = $"-f {uidHex}{block0Tail}";
            return await RunToolAsync("nfc-mfsetuid", args, progress, ct);
        }

        // ─────────────────────────────────────────────────────
        // Scan card (nfc-list)
        // ─────────────────────────────────────────────────────

        public async Task<int> ScanCardAsync(
            IProgress<string> progress, CancellationToken ct = default)
        {
            return await RunToolAsync("nfc-list", "", progress, ct);
        }

        // ─────────────────────────────────────────────────────
        // Core runner
        // ─────────────────────────────────────────────────────

        private async Task<int> RunToolAsync(
            string tool, string arguments,
            IProgress<string> progress, CancellationToken ct,
            Action<string>? onLine = null)
        {
            if (Interlocked.CompareExchange(ref _busy, 1, 0) != 0)
                throw new InvalidOperationException("NfcService is already running a task.");

            string binary = _runner.ResolveTool(tool);

            if (!File.Exists(binary))
            {
                progress.Report($"[ERROR] Tool not found: {binary}");
                Interlocked.Exchange(ref _busy, 0);
                return -1;
            }

            var psi = new ProcessStartInfo(binary)
            {
                Arguments = arguments,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            int exitCode = -1;
            try
            {
                using var proc = new Process { StartInfo = psi, EnableRaisingEvents = true };
                proc.OutputDataReceived += (_, e) =>
                {
                    if (e.Data != null) { progress.Report(e.Data); onLine?.Invoke(e.Data); }
                };
                proc.ErrorDataReceived += (_, e) =>
                {
                    if (e.Data != null) { progress.Report(e.Data); onLine?.Invoke(e.Data); }
                };

                proc.Start();
                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();

                using var reg = ct.Register(() =>
                {
                    try { if (!proc.HasExited) proc.Kill(); } catch { }
                });

                await proc.WaitForExitAsync(ct).ConfigureAwait(false);
                exitCode = proc.ExitCode;
            }
            finally
            {
                Interlocked.Exchange(ref _busy, 0);
            }

            return exitCode;
        }
    }

    // Small shim so NfcService can call Utils without adding a project reference loop
    file static class Utils
    {
        public static string Hex2Str(byte[] bytes)
        {
            var sb = new System.Text.StringBuilder();
            foreach (byte b in bytes) sb.AppendFormat("{0:x2}", b);
            return sb.ToString();
        }
    }
}
