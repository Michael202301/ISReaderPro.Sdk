/*
 * Sample 14 – System Features (.NET Framework 4.x)
 * ==================================================
 * 시스템 레벨 기능 예제:
 *   A. 포트 탐색     — IksungReaderDiscovery.ScanIksungPortsAsync
 *   B. 연결 옵션     — IksungReaderOptions (AutoReconnect, LogRawPackets)
 *   C. 연결 상태 이벤트 — reader.ConnectionChanged
 *   D. 리더기 정보   — reader.GetReaderInfoAsync, reader.PingAsync
 *   E. Raw 패킷 로그 — reader.RawPacketReceived
 *   F. 명시적 해제   — reader.DisconnectAsync
 *
 * 사용법:
 *   (인수 없이 실행)                리더를 자동으로 찾습니다 — 포트를 몰라도 됩니다
 *   SystemFeatures.Net4x.exe COM3                      (Serial — Windows)
 *   SystemFeatures.Net4x.exe scan                      (Serial 포트 탐색 모드)
 *   SystemFeatures.Net4x.exe pcsc                      (PC/SC — 첫 번째 리더 자동 선택)
 *   SystemFeatures.Net4x.exe "pcsc:iksung IS-3500Z 0"  (PC/SC — 리더 이름 직접 지정)
 *   SystemFeatures.Net4x.exe pcsc-list                 (PC/SC 리더 목록 출력)
 */

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Iksung.Reader;

namespace SystemFeatures.Net4x
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            // ══════════════════════════════════════════════════════════════
            // A. 포트 탐색 모드 (exe scan)
            // ══════════════════════════════════════════════════════════════
            if (args.Length > 0 && string.Equals(args[0], "scan", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("[SCAN] Iksung 리더기 포트 탐색 시작...");

                IProgress<(string portName, bool isIksung)> progress =
                    new Progress<(string, bool)>(p =>
                    {
                        string mark = p.Item2 ? "O Iksung" : "X";
                        Console.WriteLine("  " + p.Item1.PadRight(8) + " " + mark);
                    });

                IReadOnlyList<string> found = await IksungReaderDiscovery.ScanIksungPortsAsync(
                    baudRate:         115200,
                    pingTimeoutMs:    400,
                    progressCallback: progress);

                Console.WriteLine();
                if (found.Count == 0)
                    Console.WriteLine("[SCAN] Iksung 리더기를 찾을 수 없습니다.");
                else
                    Console.WriteLine("[SCAN] 발견된 포트: " + string.Join(", ", found));
                return;
            }

            // ══════════════════════════════════════════════════════════════
            // PC/SC 리더 목록 모드 (exe pcsc-list)
            // ══════════════════════════════════════════════════════════════
            if (args.Length > 0 && string.Equals(args[0], "pcsc-list", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("[PCSC] 연결된 PC/SC 리더 목록:");
                IReadOnlyList<string> pcscReaders = IksungPcscDiscovery.GetAvailableReaders();
                if (pcscReaders.Count == 0)
                    Console.WriteLine("  (없음)");
                else
                    for (int i = 0; i < pcscReaders.Count; i++)
                        Console.WriteLine("  [" + i + "] " + pcscReaders[i]);
                return;
            }

            // ══════════════════════════════════════════════════════════════
            // B-F. 연결 및 시스템 기능 데모
            // ══════════════════════════════════════════════════════════════
            // ── 채널 선택: "pcsc" 또는 "pcsc:리더이름" → PC/SC, 그 외 → Serial ──────────
            string firstArg = args.Length > 0 ? args[0] : "(자동 탐색)";

            Console.WriteLine("[IKSUNG] Connecting to " + firstArg + "...");

            // ── B. 연결 옵션 설정 ──────────────────────────────────────────
            var options = new IksungReaderOptions
            {
                AutoReconnect    = true,
                ReconnectDelayMs = 2000,
                DefaultTimeoutMs = 1000,
                LogRawPackets    = true,
            };

            // ── 연결: 인수 없으면 자동 탐색 / "COMx" = Serial / "pcsc[:이름]" = PC/SC ──
            IksungReader? reader = await SampleConnect.OpenAsync(args);
            if (reader == null) return;

            try
            {
                // ── 연결 확인 ─────────────────────────────────────────────────
                Console.WriteLine("[IKSUNG] 리더기 응답 확인 중...");
                bool isReady = await reader.PingAsync(timeoutMs: 500);
                if (!isReady)
                {
                    Console.WriteLine("[ERROR] 리더기가 응답하지 않습니다. (연결: " + firstArg + ")");
                    Console.WriteLine("  - 리더기 전원을 확인하세요.");
                    Console.WriteLine("  - 케이블 연결을 확인하세요.");
                    return;
                }

                // ── C. 연결 상태 이벤트 ────────────────────────────────────
                reader.ConnectionChanged += OnConnectionChanged;

                // ── E. Raw 패킷 로그 이벤트 ───────────────────────────────
                reader.RawPacketReceived += OnRawPacketReceived;

                Console.WriteLine("[IKSUNG] 연결 성공!\n");

                // ── D. 리더기 정보 조회 ────────────────────────────────────
                Console.WriteLine("-- D. GetReaderInfoAsync ------------------------------------------");
                ReaderInfo info = await reader.GetReaderInfoAsync();
                Console.WriteLine("  " + info.ToString());
                Console.WriteLine();

                // ── D. Ping 테스트 ─────────────────────────────────────────
                Console.WriteLine("-- D. PingAsync ---------------------------------------------------");
                bool alive = await reader.PingAsync(timeoutMs: 500);
                Console.WriteLine("  리더기 응답: " + (alive ? "OK" : "TIMEOUT"));
                Console.WriteLine();

                // ── D. 버전 읽기 (Raw 패킷 로그 확인용) ──────────────────
                Console.WriteLine("-- E. RawPacketReceived (ReadVersion 전송 예시) ---------------");
                string version = await reader.ReadVersionAsync();
                Console.WriteLine("  Firmware: " + version);
                Console.WriteLine();

                // ── Raw 명령 직접 전송 (부저 On/Off) ─────────────────────
                Console.WriteLine("-- SendRawCommandAsync (부저 1회) ---------------------------------");
                await reader.SendRawCommandAsync(0x00, 0x11, data: new byte[] { 0x01 }, timeoutMs: 500);
                Console.WriteLine("  부저 명령 전송 완료");
                Console.WriteLine();

                // ── AutoReconnect 데모 안내 ────────────────────────────────
                Console.WriteLine("-- AutoReconnect 데모 ---------------------------------------------");
                Console.WriteLine("  케이블을 뽑으면 ConnectionChanged(false) 이벤트가 발생하고");
                Console.WriteLine("  2초마다 자동 재연결을 시도합니다.");
                Console.WriteLine("  Ctrl+C 를 눌러 종료하세요.\n");

                using (var cts = new CancellationTokenSource())
                {
                    Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

                    while (!cts.Token.IsCancellationRequested)
                    {
                        try { await Task.Delay(3000, cts.Token); }
                        catch (OperationCanceledException) { break; }

                        bool ok = await reader.PingAsync(timeoutMs: 500, ct: cts.Token);
                        Console.WriteLine(
                            "  [" + DateTime.Now.ToString("HH:mm:ss") + "] Ping: " +
                            (ok ? "OK" : "NO RESPONSE"));
                    }
                }

                // ── F. 명시적 연결 해제 ───────────────────────────────────
                Console.WriteLine("\n-- F. DisconnectAsync ---------------------------------------------");
                await reader.DisconnectAsync();
                Console.WriteLine("  연결 해제 완료 (AutoReconnect 중단됨)");
                Console.WriteLine("\n[IKSUNG] Done.");

                // 이벤트 핸들러 해제 (Named method 패턴)
                reader.ConnectionChanged -= OnConnectionChanged;
                reader.RawPacketReceived -= OnRawPacketReceived;
            }
            finally
            {
                await reader.DisposeAsync();
            }
        }

        // ── Named event handlers (net472: 이름 있는 메서드로 해제 가능) ──────

        private static void OnConnectionChanged(object? sender, bool isConnected)
        {
            string status = isConnected ? "O 연결됨" : "X 연결 끊김";
            Console.WriteLine("\n[CONNECTION] " + status);
        }

        private static void OnRawPacketReceived(object? sender, RawPacketEventArgs e)
        {
            string dir = e.IsTransmit ? "TX" : "RX";
            string hex = BitConverter.ToString(e.Data).Replace("-", " ");
            Console.WriteLine("  [" + e.Timestamp.ToString("HH:mm:ss.fff") + "] " + dir + ": " + hex);
        }
    }
}
