# 10. ISO 7816 / USIM (SIM 카드)

## 개요

IS-3500K에는 USIM(SIM 카드) 슬롯이 내장되어 있습니다.  
ISO 7816 T=0 프로토콜로 SIM 카드와 통신하여 ICCID, IMSI, 파일 시스템 등을 읽을 수 있습니다.

### ISO 7816 기본 개념

| 용어 | 설명 |
|------|------|
| ATR | Answer-to-Reset: 카드가 활성화 시 반환하는 초기 정보 |
| TPDU | T=0 전송 단위, 5바이트 헤더 [CLA INS P1 P2 P3] |
| APDU | Application PDU: TPDU 위에 올라가는 애플리케이션 명령 |
| SW | Status Word (2바이트): 9000=성공, 61XX=응답 대기 중 |
| MF | Master File (3F00): 파일 시스템 루트 |
| EF | Elementary File: 실제 데이터 파일 |

---

## 1. 카드 활성화 — ATR 수신

```csharp
byte channel = 0; // 채널 번호 (0 고정, 슬롯 1개)
byte[] atr = await reader.UsimActivateAsync(channel, 3000);

Console.WriteLine($"ATR: {BitConverter.ToString(atr).Replace("-", " ")}");

// T=0 / T=1 프로토콜 확인
bool isT1 = atr.Length >= 2 && (atr[1] & 0x0F) == 1;
Console.WriteLine($"Protocol: {(isT1 ? "T=1" : "T=0")}");
```

---

## 2. APDU 전송 (TPDU)

```csharp
// SELECT MF (Master File 3F00)
byte[] selectMf = [0x00, 0xA4, 0x00, 0x00, 0x02, 0x3F, 0x00];
byte[] resp = await reader.UsimSendTpduAsync(selectMf, channel, 2000);

Console.WriteLine($"SELECT MF: {BitConverter.ToString(resp).Replace("-", " ")}");
Console.WriteLine($"SW: {resp[^2]:X2}{resp[^1]:X2}");
```

---

## 3. ICCID 읽기 예제

ICCID는 SIM 카드의 고유 식별 번호 (20자리) 입니다. EF_ICCID 파일 경로: `MF(3F00) > EF_ICCID(2FE2)`

```csharp
byte[] atr = await reader.UsimActivateAsync(0, 3000);

// 1. SELECT EF_ICCID
byte[] selectIccid = [0x00, 0xA4, 0x00, 0x00, 0x02, 0x2F, 0xE2];
byte[] resp = await reader.UsimSendTpduAsync(selectIccid, 0, 2000);

bool canRead = resp.Length >= 2 && resp[^2] == 0x90 && resp[^1] == 0x00;

// GET RESPONSE가 필요한 경우 (SW = 9FXX)
if (!canRead && resp.Length >= 2 && resp[^2] == 0x9F)
{
    byte le = resp[^1];
    byte[] getResp = [0x00, 0xC0, 0x00, 0x00, le];
    resp = await reader.UsimSendTpduAsync(getResp, 0, 2000);
    canRead = resp.Length >= 2 && resp[^2] == 0x90;
}

if (canRead)
{
    // 2. READ BINARY (10바이트 = ICCID BCD)
    byte[] readBin = [0x00, 0xB0, 0x00, 0x00, 0x0A];
    resp = await reader.UsimSendTpduAsync(readBin, 0, 2000);

    if (resp.Length >= 12 && resp[^2] == 0x90)
    {
        byte[] bcd = resp[..10];
        // BCD nibble-swap 디코딩
        var sb = new System.Text.StringBuilder();
        foreach (byte b in bcd)
        {
            sb.Append((char)('0' + (b & 0x0F)));
            sb.Append((char)('0' + (b >> 4)));
        }
        Console.WriteLine($"ICCID: {sb}");
    }
}

await reader.UsimDeactivateAsync(0, 1000);
```

---

## 4. 카드 비활성화

```csharp
await reader.UsimDeactivateAsync(channel, 1000);
Console.WriteLine("USIM deactivated.");
```

---

## 5. 시리얼 번호 읽기

```csharp
byte[] serial = await reader.UsimReadSerialAsync();
Console.WriteLine($"USIM Serial: {BitConverter.ToString(serial).Replace("-", " ")}");
```

---

## 6. 자주 사용하는 APDU 명령 참조

| 명령 | CLA | INS | P1 | P2 | Lc/Data | 설명 |
|------|-----|-----|----|----|---------|------|
| SELECT MF | 00 | A4 | 00 | 00 | 02 3F 00 | 루트 선택 |
| SELECT EF_ICCID | 00 | A4 | 00 | 00 | 02 2F E2 | ICCID 파일 선택 |
| SELECT EF_IMSI | 00 | A4 | 00 | 00 | 02 6F 07 | IMSI 파일 선택 (3GPP) |
| READ BINARY | 00 | B0 | offset_hi | offset_lo | Le | 파일 읽기 |
| GET RESPONSE | 00 | C0 | 00 | 00 | Le | 응답 데이터 가져오기 (T=0) |
| VERIFY PIN | 00 | 20 | 00 | 01 | 08 PIN... | PIN 검증 |

---

## 7. 전체 예제 — 반복 읽기

```csharp
await using var reader = await IksungReader.ConnectSerialAsync("COM3");

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

while (!cts.Token.IsCancellationRequested)
{
    try
    {
        byte[] atr = await reader.UsimActivateAsync(0, 3000, cts.Token);
        Console.WriteLine($"ATR: {BitConverter.ToString(atr).Replace("-", " ")}");

        byte[] selectMf = [0x00, 0xA4, 0x00, 0x00, 0x02, 0x3F, 0x00];
        byte[] resp = await reader.UsimSendTpduAsync(selectMf, 0, 2000, cts.Token);
        Console.WriteLine($"SELECT MF SW: {resp[^2]:X2}{resp[^1]:X2}");

        await reader.UsimDeactivateAsync(0, 1000, cts.Token);
        Console.WriteLine("Done.\n");
    }
    catch (IksungTimeoutException)     { Console.WriteLine("(no card)"); }
    catch (OperationCanceledException) { break; }

    try { await Task.Delay(5000, cts.Token); } catch (OperationCanceledException) { break; }
}
```

---

[← AutoRead 이벤트 모드](09-autoread.md) | [다음: Bluetooth / BLE →](11-bluetooth.md)
