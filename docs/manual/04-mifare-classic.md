# 04. Mifare Classic

## 개요

Mifare Classic은 NXP의 ISO 14443 Type A 기반 칩입니다. 출입 통제, 교통, 사원증 등에 널리 사용됩니다.

| 제품 | 용량 | 섹터 | 블록 |
|------|------|------|------|
| Mifare Classic 1K | 1 KB | 16 섹터 | 64 블록 (블록 0~63) |
| Mifare Classic 4K | 4 KB | 40 섹터 | 256 블록 |

### 메모리 구조 (1K 기준)

```
섹터 0 : 블록  0(Manufacturer), 블록  1, 블록  2, 블록  3(Sector Trailer)
섹터 1 : 블록  4, 블록  5, 블록  6, 블록  7(Sector Trailer)
...
섹터 15: 블록 60, 블록 61, 블록 62, 블록 63(Sector Trailer)
```

- **블록 0**: 제조사 데이터 (읽기 전용, 쓰기 불가)
- **Sector Trailer (각 섹터 마지막 블록)**: Key A (6B), Access Bits (4B), Key B (6B)

---

## 1. 카드 활성화

```csharp
byte[] atr = await reader.ActivateMifareAsync();
Console.WriteLine($"Mifare activated: {BitConverter.ToString(atr).Replace("-", " ")}");
```

---

## 2. 인증

블록을 읽거나 쓰기 전에 해당 섹터의 Key로 인증해야 합니다.

```csharp
using Iksung.Reader.Models; // MifareKeyType

byte blockNo = 4;  // 섹터 1의 첫 번째 블록

// Key A로 인증 (기본 공장 키)
byte[] keyA = [0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF];
await reader.MifareAuthenticateAsync(blockNo, MifareKeyType.KeyA, keyA);

// Key B로 인증
byte[] keyB = [0xB0, 0xB1, 0xB2, 0xB3, 0xB4, 0xB5];
await reader.MifareAuthenticateAsync(blockNo, MifareKeyType.KeyB, keyB);
```

> 같은 섹터 내에서는 한 번 인증하면 해당 섹터의 모든 블록에 접근 가능합니다.

---

## 3. 블록 읽기

```csharp
byte[] data = await reader.MifareReadBlockAsync(blockNo);
Console.WriteLine($"Block {blockNo}: {BitConverter.ToString(data).Replace("-", " ")}");
// 출력 예: "00 11 22 33 44 55 66 77 88 99 AA BB CC DD EE FF"
// (항상 16바이트 반환)
```

---

## 4. 블록 쓰기

```csharp
byte[] newData = new byte[16]; // 반드시 16바이트
Array.Fill(newData, (byte)0x00);
newData[0] = 0xAB;
newData[1] = 0xCD;

await reader.MifareWriteBlockAsync(blockNo, newData);
Console.WriteLine("Block written.");
```

> **주의:** 블록 0 (제조사 블록)과 Sector Trailer에 잘못된 데이터를 쓰면 카드가 영구 손상될 수 있습니다.

---

## 5. 섹터 읽기

섹터의 데이터 블록 전체를 한 번에 읽습니다 (Sector Trailer 제외).

```csharp
byte sectorNo = 1;
byte[] sectorData = await reader.MifareReadSectorAsync(sectorNo);
// sectorData = 섹터 내 데이터 블록들의 합산 (각 블록 16바이트)
```

---

## 6. 전체 예제 — 블록 읽기/쓰기

```csharp
await using var reader = await IksungReader.ConnectSerialAsync("COM3");

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

Console.WriteLine("Mifare Classic 카드를 올려 주세요...");

while (!cts.Token.IsCancellationRequested)
{
    try
    {
        byte[] atr = await reader.ActivateMifareAsync(1000, cts.Token);
        Console.WriteLine($"카드 활성화: {BitConverter.ToString(atr).Replace("-", " ")}");

        byte blockNo = 4;
        byte[] key = [0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF];

        // 인증
        await reader.MifareAuthenticateAsync(blockNo, MifareKeyType.KeyA, key, 1000, cts.Token);
        Console.WriteLine("인증 성공");

        // 읽기
        byte[] before = await reader.MifareReadBlockAsync(blockNo, 1000, cts.Token);
        Console.WriteLine($"읽기 전: {BitConverter.ToString(before).Replace("-", " ")}");

        // 현재 시각을 블록에 기록
        byte[] write = new byte[16];
        BitConverter.GetBytes(DateTimeOffset.UtcNow.ToUnixTimeSeconds()).CopyTo(write, 0);
        await reader.MifareWriteBlockAsync(blockNo, write, 1000, cts.Token);
        Console.WriteLine("쓰기 완료");

        // 검증
        byte[] after = await reader.MifareReadBlockAsync(blockNo, 1000, cts.Token);
        Console.WriteLine($"읽기 후: {BitConverter.ToString(after).Replace("-", " ")}");

        await Task.Delay(2000, cts.Token);
    }
    catch (IksungTimeoutException)     { await Task.Delay(200, cts.Token); }
    catch (OperationCanceledException) { break; }
    catch (Exception ex)               { Console.WriteLine($"오류: {ex.Message}"); }
}
```

---

## 7. 섹터 번호 ↔ 블록 번호 변환 (1K)

```csharp
// 섹터 번호로 데이터 블록 번호 계산
static byte SectorToBlock(byte sector, byte blockInSector = 0)
    => (byte)(sector * 4 + blockInSector);

// 예
byte dataBlock  = SectorToBlock(2, 0);  // = 8  (섹터 2의 첫 번째 블록)
byte trailerBlock = SectorToBlock(2, 3); // = 11 (섹터 2의 Sector Trailer)
```

---

[← ISO 14443 A/B](03-iso14443ab.md) | [다음: Mifare Ultralight / NTag →](05-mifare-ultralight-ntag.md)
