# 05. Mifare Ultralight / NTag

## 개요

Mifare Ultralight와 NTag는 NXP의 저비용 ISO 14443 Type A 메모리 태그입니다.  
스티커, 포스터, 소모품 추적 등에 주로 사용됩니다.

| 제품 | 페이지 수 | 사용자 메모리 | 특징 |
|------|-----------|-------------|------|
| Mifare Ultralight | 16 | 48 바이트 | 기본형, 암호화 없음 |
| Mifare Ultralight C | 48 | 144 바이트 | 3DES 인증 지원 |
| NTag 213 | 45 | 144 바이트 | 비밀번호 보호, ECC 서명 |
| NTag 215 | 135 | 504 바이트 | 대용량 |
| NTag 216 | 231 | 888 바이트 | 최대 용량 |

각 페이지는 **4바이트**입니다.

---

## 1. 카드 활성화

```csharp
byte[] atr = await reader.ActivateMifareUltralightAsync();
Console.WriteLine($"UL activated: {BitConverter.ToString(atr).Replace("-", " ")}");
```

---

## 2. 페이지 읽기

```csharp
byte page = 4; // 사용자 메모리 시작 페이지 (NTag213 기준)
byte[] data = await reader.MifareUltralightReadPageAsync(page);
// 리더기는 지정 페이지부터 4페이지(16바이트)를 반환하는 경우도 있음
Console.WriteLine($"Page {page}: {BitConverter.ToString(data).Replace("-", " ")}");
```

**NTag213 메모리 레이아웃:**

| 페이지 | 내용 |
|--------|------|
| 0~1 | UID (7바이트) + BCC |
| 2 | UID(6~7) + Internal |
| 3 | OTP (One-Time Programmable) |
| 4~39 | 사용자 데이터 (144바이트) |
| 40 | CFG 0 (AUTH0 등 설정) |
| 41 | CFG 1 (Access 설정) |
| 42 | PWD (비밀번호 — 읽기 불가) |
| 43 | PACK (비밀번호 응답) |
| 44 | 내부 |

---

## 3. 페이지 쓰기

```csharp
byte page = 5;
byte[] data = [0x12, 0x34, 0x56, 0x78]; // 반드시 4바이트
await reader.MifareUltralightWritePageAsync(page, data);
Console.WriteLine("Written.");
```

> **주의:** 페이지 0~3은 제조사 / OTP 영역이므로 쓰지 마세요.

---

## 4. NTag 전용 기능

### 버전 정보 읽기

```csharp
byte[] ver = await reader.NTagGetVersionAsync();
string chipType = IksungReader.ParseNTagType(ver);
Console.WriteLine($"Chip type: {chipType}");
// 출력 예: "NTag213"
```

### 빠른 읽기 (FastRead)

여러 페이지를 한 번에 읽습니다.

```csharp
byte startPage = 4;
byte endPage   = 15;
byte[] pages = await reader.NTagFastReadAsync(startPage, endPage);
// pages.Length = (endPage - startPage + 1) * 4
```

### NFC 카운터 읽기

NTag215/216은 태그가 리더기에 근접할 때마다 자동으로 증가하는 카운터를 제공합니다.

```csharp
byte[] counter = await reader.NTagReadCounterAsync(0); // counterNo=0
int count = (counter[0]) | (counter[1] << 8) | (counter[2] << 16);
Console.WriteLine($"NFC Counter: {count}");
```

### ECC 서명 읽기

NXP 공장에서 주입된 32바이트 ECC 서명을 읽어 칩의 진위를 확인합니다.

```csharp
byte[] sig = await reader.NTagReadSignatureAsync();
Console.WriteLine($"ECC Signature: {BitConverter.ToString(sig).Replace("-", " ")}");
// 32바이트 서명
```

---

## 5. 비밀번호 보호

### 비밀번호 인증

비밀번호 보호가 설정된 태그에 접근 시 먼저 인증해야 합니다.

```csharp
byte[] password = [0x01, 0x02, 0x03, 0x04]; // 4바이트 비밀번호
await reader.NTagPasswordAuthAsync(password);
Console.WriteLine("비밀번호 인증 성공");
```

### AUTH0 설정 (보호 시작 페이지)

AUTH0 페이지 번호 이상의 페이지에 비밀번호 인증이 요구되도록 설정합니다.

```csharp
byte auth0 = 4; // 페이지 4부터 비밀번호 보호
await reader.NTagWriteAuth0Async(auth0);
```

> `auth0 = 0xFF` 로 설정하면 비밀번호 보호가 비활성화됩니다.

### Access 설정

읽기 보호 여부, 쓰기 보호 여부를 설정합니다.

```csharp
// bit7=PROT(1=읽기도 보호), bit6=CFGLCK, bit2:0=NFC_CNT_PKG
byte access = 0x00; // 쓰기만 보호 (읽기는 자유)
await reader.NTagWriteAccessAsync(access);
```

### 비밀번호 변경

```csharp
byte[] newPwd  = [0xAA, 0xBB, 0xCC, 0xDD]; // 새 비밀번호 4바이트
byte[] newPack = [0x12, 0x34];              // 응답 코드 2바이트
await reader.NTagChangePasswordAsync(newPwd, newPack);
Console.WriteLine("비밀번호 변경 완료");
```

> 비밀번호를 잊으면 해당 영역에 접근할 수 없으므로 신중하게 설정하세요.

---

## 6. 전체 예제 — NTag213 전체 덤프

```csharp
await using var reader = await IksungReader.ConnectSerialAsync("COM3");

while (true)
{
    try
    {
        byte[] atr = await reader.ActivateMifareUltralightAsync();
        Console.WriteLine($"Tag: {BitConverter.ToString(atr).Replace("-", " ")}");

        // 버전으로 칩 타입 확인
        byte[] ver = await reader.NTagGetVersionAsync();
        Console.WriteLine($"Chip: {IksungReader.ParseNTagType(ver)}");

        // 페이지 0~44 전체 읽기
        byte[] all = await reader.NTagFastReadAsync(0, 44);
        for (int i = 0; i < all.Length / 4; i++)
        {
            byte[] page = all[(i * 4)..((i + 1) * 4)];
            Console.WriteLine($"  Page {i,2}: {BitConverter.ToString(page).Replace("-", " ")}");
        }

        await Task.Delay(3000);
    }
    catch (IksungTimeoutException) { await Task.Delay(200); }
}
```

---

[← Mifare Classic](04-mifare-classic.md) | [다음: ISO 15693 →](06-iso15693.md)
