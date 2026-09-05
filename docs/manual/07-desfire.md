# 07. DESFire EV1 / EV2 / EV3

## 개요

Mifare DESFire는 NXP의 고보안 ISO 14443 Type A 스마트카드입니다.  
파일 시스템 구조와 AES/DES 암호화를 내장하여 전자화폐, 출입 통제, 대중교통에 사용됩니다.

### 메모리 구조

```
PICC (카드)
└── Root Application (AID = 000000)
    └── Application (AID = 예: 010203)
        ├── File 01 (Standard Data File)
        ├── File 02 (Value File)
        └── File 03 (Record File)
```

### 암호화 방식 (cryptoType)

| 값 | 알고리즘 | 키 길이 |
|----|---------|---------|
| `0x00` | DES / 2TDEA | 8 / 16 바이트 |
| `0x01` | 3TDEA (3K3DES) | 24 바이트 |
| `0x02` | AES-128 | 16 바이트 |

---

## 1. 카드 활성화

```csharp
byte[] atr = await reader.ActivateDesfireAsync();
Console.WriteLine($"DESFire ATR: {BitConverter.ToString(atr).Replace("-", " ")}");
```

---

## 2. 카드 정보 조회

```csharp
// 남은 메모리 (바이트)
byte[] freeMemRaw = await reader.DesfireGetFreeMemoryAsync();
int freeMem = (freeMemRaw[0]) | (freeMemRaw[1] << 8) | (freeMemRaw[2] << 16);
Console.WriteLine($"Free memory: {freeMem} bytes");

// 랜덤 UID 읽기 (RandomUID 모드일 때)
byte[] uid = await reader.DesfireGetUidAsync();
Console.WriteLine($"UID: {BitConverter.ToString(uid).Replace("-", " ")}");
```

---

## 3. 키 관리

DESFire 인증에 사용할 키를 리더기의 플래시 메모리에 저장합니다.  
(키는 리더기 내부에서만 사용되며 외부로 노출되지 않습니다.)

```csharp
byte keyIndex   = 0;    // 리더기 내부 키 슬롯 번호 (0~N)
byte keyVersion = 0x00; // 키 버전
byte cryptoType = 0x02; // 0x02 = AES-128

byte[] aesKey = [
    0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07,
    0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F
];

await reader.DesfireKeySaveAsync(keyIndex, keyVersion, cryptoType, aesKey);
Console.WriteLine("Key saved to reader flash.");
```

### 키 초기화 (공장 기본값으로 리셋)

```csharp
await reader.DesfireKeyInitAsync();
```

---

## 4. 인증

저장된 키를 사용하여 카드와 상호 인증합니다.

```csharp
// AES 인증 (EV1/EV2/EV3)
byte keyNo = 0x00; // 카드의 키 번호 (보통 마스터 키 = 0x00)
await reader.DesfireAuthenticateAesAsync(keyNo);
Console.WriteLine("AES 인증 성공");

// 2K3DES 인증 (구형 EV1)
await reader.DesfireAuthenticate2K3DesAsync(keyNo);

// ISO 인증 (EV2/EV3 고보안 모드)
await reader.DesfireAuthenticateIsoAsync(keyNo);

// 인증 초기화 (인증 상태 취소)
await reader.DesfireAuthResetAsync();
```

---

## 5. 애플리케이션 관리

### 애플리케이션 목록 조회

```csharp
byte[] appIdsRaw = await reader.DesfireGetApplicationIdsAsync();
// 3바이트씩 AID 목록
for (int i = 0; i + 2 < appIdsRaw.Length; i += 3)
{
    byte[] aid = appIdsRaw[i..(i + 3)];
    Console.WriteLine($"AID: {BitConverter.ToString(aid).Replace("-", "")}");
}
```

### 애플리케이션 선택

```csharp
byte[] appId = [0x01, 0x02, 0x03];
await reader.DesfireSelectApplicationAsync(appId);
Console.WriteLine($"Application {BitConverter.ToString(appId).Replace("-", "")} selected.");

// 루트(PICC) 선택
await reader.DesfireSelectRootAsync();
```

### 애플리케이션 생성

```csharp
byte[] newAppId = [0x01, 0x02, 0x03];
byte keySettings = 0x0F; // 키 설정 (상세 내용은 DESFire 데이터시트 참조)
byte keyCount    = 0x01; // 이 앱에 사용할 키 개수

await reader.DesfireCreateApplicationAsync(newAppId, keySettings, keyCount);
Console.WriteLine("Application created.");
```

### 애플리케이션 삭제

```csharp
byte[] appId = [0x01, 0x02, 0x03];
await reader.DesfireDeleteApplicationAsync(appId);
```

---

## 6. 파일 관리

### 파일 목록 조회

```csharp
byte[] fileIds = await reader.DesfireGetFileIdsAsync();
Console.WriteLine($"Files: {BitConverter.ToString(fileIds).Replace("-", " ")}");
```

### 파일 설정 조회

```csharp
byte fileNo = 0x01;
byte[] settings = await reader.DesfireGetFileSettingsAsync(fileNo);
Console.WriteLine($"File {fileNo} settings: {BitConverter.ToString(settings).Replace("-", " ")}");
```

### Standard Data File 생성

```csharp
byte fileNo       = 0x01;
byte commSettings = 0x00;    // 0x00=평문, 0x01=MAC, 0x03=암호화
ushort accessRights = 0x0000; // 키 번호 조합 (상세 내용은 데이터시트 참조)
int fileSize      = 32;       // 파일 크기 (바이트)

await reader.DesfireCreateStdFileAsync(fileNo, commSettings, accessRights, fileSize);
Console.WriteLine("Standard file created.");
```

### 파일 삭제

```csharp
await reader.DesfireDeleteFileAsync(0x01);
```

---

## 7. 데이터 파일 읽기/쓰기

### 읽기

```csharp
byte fileNo       = 0x01;
byte commSettings = 0x00; // 평문
int offset        = 0;
int length        = 0;    // 0 = 파일 전체

byte[] data = await reader.DesfireReadDataFileAsync(fileNo, commSettings, offset, length);
Console.WriteLine($"Data: {BitConverter.ToString(data).Replace("-", " ")}");
```

### 쓰기 + 트랜잭션 커밋

```csharp
byte[] payload = System.Text.Encoding.UTF8.GetBytes("Hello DESFire!");

await reader.DesfireWriteDataFileAsync(0x01, payload);
await reader.DesfireCommitTransactionAsync();
Console.WriteLine("Write committed.");

// 트랜잭션 취소
// await reader.DesfireAbortTransactionAsync();
```

---

## 8. 밸류 파일 (Value File)

포인트, 잔액처럼 정수값을 안전하게 저장합니다.

```csharp
// 현재 값 읽기
byte[] raw = await reader.DesfireReadValueFileAsync(0x02);
int value = BitConverter.ToInt32(raw, 0);
Console.WriteLine($"Balance: {value}");

// 충전 (Credit)
await reader.DesfireCreditValueFileAsync(0x02, 1000);
await reader.DesfireCommitTransactionAsync();
Console.WriteLine("Credited 1000.");

// 차감 (Debit)
await reader.DesfireDebitValueFileAsync(0x02, 300);
await reader.DesfireCommitTransactionAsync();
Console.WriteLine("Debited 300.");
```

---

## 9. 카드 포맷 (공장 초기화)

**주의: 모든 데이터와 앱이 삭제됩니다. PICC 마스터 키로 인증된 상태에서만 가능합니다.**

```csharp
await reader.DesfireFormatAsync(3000);
Console.WriteLine("Card formatted.");
```

---

## 10. 전체 워크플로우 예제

```csharp
await using var reader = await IksungReader.ConnectSerialAsync("COM3");

// 1. 활성화
byte[] atr = await reader.ActivateDesfireAsync();
Console.WriteLine($"DESFire activated: {BitConverter.ToString(atr).Replace("-", " ")}");

// 2. 키 저장
byte[] aesKey = new byte[16]; // All-zero AES key (공장 기본값)
await reader.DesfireKeySaveAsync(0, 0, 0x02, aesKey);

// 3. 루트 선택 + 마스터 키 인증
await reader.DesfireSelectRootAsync();
await reader.DesfireAuthenticateAesAsync(0x00);
Console.WriteLine("Root authenticated.");

// 4. 앱 생성
byte[] aid = [0xAA, 0xBB, 0xCC];
await reader.DesfireCreateApplicationAsync(aid, 0x0F, 1);

// 5. 앱 선택 + 인증
await reader.DesfireSelectApplicationAsync(aid);
await reader.DesfireAuthenticateAesAsync(0x00);

// 6. 파일 생성
await reader.DesfireCreateStdFileAsync(0x01, 0x00, 0x0000, 32);

// 7. 쓰기
byte[] data = new byte[32];
System.Text.Encoding.ASCII.GetBytes("Hello DESFire!").CopyTo(data, 0);
await reader.DesfireWriteDataFileAsync(0x01, data);
await reader.DesfireCommitTransactionAsync();

// 8. 읽기
byte[] read = await reader.DesfireReadDataFileAsync(0x01);
Console.WriteLine($"Read: {System.Text.Encoding.ASCII.GetString(read).TrimEnd('\0')}");
```

---

[← ISO 15693](06-iso15693.md) | [다음: LF 125 kHz →](08-lf125khz.md)
