/*
 * Sample net4x-04 – WinForms 통합 패턴 (.NET Framework 4.7.2)
 * =============================================================
 * WinForms 애플리케이션에서 Iksung.Reader를 통합하는 권장 패턴:
 *   - Form.Load  → ConnectSerialAsync + StartAutoReadAsync
 *   - TagDetected → Invoke()로 UI 스레드에서 Label 업데이트
 *   - Form.Closing → StopAutoReadAsync + DisposeAsync
 *
 * 이 파일은 실행 가능한 최소 WinForms 예제입니다.
 * 실제 프로젝트에서는 디자이너(.Designer.cs)와 분리하세요.
 *
 * 빌드:
 *   dotnet build
 * 실행:
 *   dotnet run -- COM3
 */

using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using Iksung.Reader;
using Iksung.Reader.Exceptions;

namespace WinFormsIntegration.Net4x
{
    internal static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            string portName = args.Length > 0 ? args[0] : "COM3";
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm(portName));
        }
    }

    // ─── 메인 폼 ───────────────────────────────────────────────────────────────
    internal sealed class MainForm : Form
    {
        private readonly string _portName;
        private IksungReader? _reader;

        // UI 컨트롤
        private readonly Label    _labelStatus;
        private readonly Label    _labelUid;
        private readonly Label    _labelCardType;
        private readonly TextBox  _logBox;
        private readonly Button   _btnConnect;
        private readonly Button   _btnDisconnect;

        public MainForm(string portName)
        {
            _portName = portName;
            Text      = "Iksung Reader — WinForms Sample";
            Width     = 520;
            Height    = 420;

            // ── 컨트롤 배치 ──
            _labelStatus = new Label   { Text = "연결 안됨", Left = 10, Top = 10,  Width = 490, Height = 24, Font = new Font("맑은 고딕", 10f) };
            _labelUid    = new Label   { Text = "UID: —",    Left = 10, Top = 40,  Width = 490, Height = 28, Font = new Font("맑은 고딕", 14f, FontStyle.Bold) };
            _labelCardType = new Label { Text = "Type: —",   Left = 10, Top = 70,  Width = 490, Height = 24, Font = new Font("맑은 고딕", 10f) };
            _logBox      = new TextBox { Left = 10, Top = 105, Width = 490, Height = 240,
                                         Multiline = true, ScrollBars = ScrollBars.Vertical,
                                         ReadOnly = true, Font = new Font("Consolas", 9f) };
            _btnConnect    = new Button { Text = "Connect",    Left = 10,  Top = 355, Width = 110 };
            _btnDisconnect = new Button { Text = "Disconnect", Left = 130, Top = 355, Width = 110, Enabled = false };

            Controls.AddRange(new Control[] {
                _labelStatus, _labelUid, _labelCardType,
                _logBox, _btnConnect, _btnDisconnect
            });

            _btnConnect.Click    += BtnConnect_Click;
            _btnDisconnect.Click += BtnDisconnect_Click;
            FormClosing          += MainForm_FormClosing;
        }

        // ── 연결 버튼 ──
        private async void BtnConnect_Click(object? sender, EventArgs e)
        {
            _btnConnect.Enabled    = false;
            _btnDisconnect.Enabled = false;
            _labelStatus.Text      = "연결 중...";

            try
            {
                _reader = await IksungReader.ConnectSerialAsync(_portName);
                string fw = await _reader.ReadVersionAsync();

                _reader.TagDetected += Reader_TagDetected;
                await _reader.StartAutoReadAsync();

                _labelStatus.Text      = "연결됨  |  포트: " + _portName + "  |  FW: " + fw;
                _btnDisconnect.Enabled = true;
                AppendLog("AutoRead 시작.");
            }
            catch (Exception ex)
            {
                _labelStatus.Text   = "연결 실패: " + ex.Message;
                _btnConnect.Enabled = true;
                _reader             = null;
            }
        }

        // ── 연결 해제 버튼 ──
        private async void BtnDisconnect_Click(object? sender, EventArgs e)
        {
            _btnDisconnect.Enabled = false;
            await DisconnectAsync();
            _btnConnect.Enabled = true;
            _labelStatus.Text   = "연결 해제됨";
        }

        // ── 폼 닫기 ──
        private async void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            // 비동기 해제를 동기로 기다림 (폼 닫기 전에 완료)
            await DisconnectAsync();
        }

        // ── 공통 해제 로직 ──
        private async Task DisconnectAsync()
        {
            if (_reader == null) return;
            try
            {
                _reader.TagDetected -= Reader_TagDetected;
                await _reader.StopAutoReadAsync();
            }
            catch { }
            finally
            {
                await _reader.DisposeAsync();
                _reader = null;
            }
            AppendLog("연결 해제.");
        }

        // ── TagDetected 이벤트 핸들러 ──
        // 백그라운드 스레드에서 호출되므로 반드시 Invoke() 사용
        private void Reader_TagDetected(object? sender, TagDetectedEventArgs e)
        {
            if (InvokeRequired)
            {
                // UI 스레드로 전환 — WinForms 핵심 패턴
                Invoke(new Action<TagDetectedEventArgs>(UpdateUi), e);
            }
            else
            {
                UpdateUi(e);
            }
        }

        private void UpdateUi(TagDetectedEventArgs e)
        {
            _labelUid.Text     = "UID: " + e.UidHex;
            _labelCardType.Text = "Type: " + e.CardType;

            _labelUid.ForeColor = e.CardType switch
            {
                CardType.MifareClassic    => Color.Navy,
                CardType.MifareUltralight => Color.DarkGreen,
                CardType.MifareDesfire    => Color.DarkOrange,
                CardType.Iso15693         => Color.Purple,
                _                         => Color.Black,
            };

            AppendLog(DateTime.Now.ToString("HH:mm:ss.fff") +
                      "  [" + e.CardType + "]  " + e.UidHex);
        }

        private void AppendLog(string msg)
        {
            if (InvokeRequired) { Invoke(new Action<string>(AppendLog), msg); return; }
            _logBox.AppendText(msg + Environment.NewLine);
        }
    }
}
