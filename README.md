# IKSUNG Reader SDK for .NET

> Easy-to-use SDK for IKSUNG NFC/RFID readers. Works on Windows, Linux, and macOS.

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE.md)

## ⚠️ Status

**Pre-release / under active development.** First public NuGet target: `v1.0.0`.

See [`docs/HANDOFF.md`](docs/HANDOFF.md) for current Phase 1 progress.

---

## Install

```bash
dotnet add package Iksung.Reader
```

(Coming soon — see [Status](#status))

---

## Hello World

```csharp
using ISReaderPro.Sdk;

await using var reader = await IksungReader.ConnectSerialAsync("COM3", 115200);
Console.WriteLine($"Firmware: {await reader.ReadVersionAsync()}");
Console.WriteLine($"UID: {Convert.ToHexString((await reader.ReadAnyCardAsync()).Uid)}");
```

That's it. **4 lines.**

---

## Supported Channels

| Channel | Status | Use Case |
|---------|--------|----------|
| Serial (System.IO.Ports) | Phase 1 | Most common — USB-to-Serial readers |
| FTDI D2XX | Phase 2 | Direct FTDI driver, slightly faster |
| PC/SC (winscard / pcsc-lite) | Phase 4 | CCID smart card readers |
| TCP/IP Socket | Phase 4 | Network gateways |

**Hardware scope:** FTDI-equipped readers only.

---

## Platform Setup

### Windows

Just install. FTDI VCP driver is usually pre-installed via Windows Update.

### Linux

```bash
# Add user to dialout group (logout/login required after)
sudo usermod -a -G dialout $USER

# (FTDI D2XX only) Blacklist kernel VCP driver
echo "blacklist ftdi_sio" | sudo tee /etc/modprobe.d/blacklist-ftdi.conf
```

Port name: `/dev/ttyUSB0`

### macOS

Install [FTDI VCP driver](https://ftdichip.com/drivers/vcp-drivers/), then approve in System Preferences → Security & Privacy.

Port name: `/dev/tty.usbserial-XXXXXXXX`

---

## Why Another SDK?

The original IKSUNG FTDI SDK (`IS_D2XX_NET.dll`) uses a C-style API:

```csharp
// Before: 8 lines + 2 error checks, manual buffer management
var reader = new IS_D2XX();
var status = reader.is_OpenSerialNumber("FTHV3K7L", 115200);
if (status != IS_OK) { /* ... */ }
uint length = 256;
byte[] buffer = new byte[256];
status = reader.is_WriteReadCommand(0x00, 0x10, 0, null, ref length, ref buffer);
if (status != IS_OK) { /* ... */ }
string version = Encoding.ASCII.GetString(buffer, 0, (int)length);
reader.is_Close();
```

This SDK provides a modern .NET-idiomatic API:

```csharp
// After: 3 lines, async/await, exceptions, no buffer management
await using var reader = await IksungReader.ConnectSerialAsync("COM3", 115200);
string version = await reader.ReadVersionAsync();
// (auto-disposed at end of scope)
```

---

## Roadmap

- **v0.1.0-preview** (Phase 2 end) — Serial + FTDI channels, Linux/Mac CI verified
- **v1.0.0** (Phase 3 end) — High-level helpers (`ReadAnyCardAsync`, `StartAutoReadAsync`), WPF/Console samples
- **v1.1.0** (Phase 4) — PC/SC + Socket channels, REST API sample, Bootloader helper
- **v2.x** — Custom channel injection (`IIksungChannel` public), more high-level helpers

---

## License

MIT — see [LICENSE.md](LICENSE.md).

Free for commercial and non-commercial use. No royalties, no fees.

---

## Contributing

Issues and PRs welcome. See [CLAUDE.md](CLAUDE.md) for project conventions.

---

## Related

- [ISReaderPro V6.01](../ISReaderPro-V6.01/) — Reference WPF application this SDK was extracted from
- [Springcard PC/SC SDK](https://github.com/springcard/springcard.pcsc.sdk) — Inspiration for clean SDK design
