# API 레퍼런스 (전체 메서드 목록)

모든 공개(public) 메서드의 서명과 간략한 설명입니다.  
메서드 이름 뒤 `Async`는 생략하지 않았습니다.

---

## IksungReader — 팩토리 / 속성 / 이벤트

```csharp
// 팩토리
static Task<IksungReader> ConnectSerialAsync(string portName, int baudRate = 115200, CancellationToken ct = default)

// 속성
bool   IsConnected  { get; }
ChannelType ConnectedVia { get; }

// 이벤트
event EventHandler<TagDetectedEventArgs>? TagDetected

// 해제
ValueTask DisposeAsync()
```

---

## 공통 명령 (MAJOR_COMMON = 0x00)

```csharp
Task<string>  ReadVersionAsync  (int timeoutMs = 1000, CancellationToken ct = default)
Task<byte[]>  ReadUniqueIdAsync (int timeoutMs = 1000, CancellationToken ct = default)
Task          RfOnAsync         (int timeoutMs = 1000, CancellationToken ct = default)
Task          RfOffAsync        (int timeoutMs = 1000, CancellationToken ct = default)
```

---

## ISO 14443 A/B (MAJOR_ISO14443AB = 0x01)

```csharp
// 빠른 UID 읽기
Task<byte[]> ReadIso14443aUidAsync(int timeoutMs = 1000, CancellationToken ct = default)
Task<byte[]> ReadIso14443bUidAsync(int timeoutMs = 1000, CancellationToken ct = default)

// Layer-3 활성화
Task<byte[]> ActivateIso14443aAsync    (int timeoutMs = 1000, CancellationToken ct = default)
Task<byte[]> ActivateIso14443bAsync    (int timeoutMs = 1000, CancellationToken ct = default)
Task<byte[]> ActivateIso14443abAsync   (int timeoutMs = 1000, CancellationToken ct = default)

// Layer-4 활성화 (ISO-DEP)
Task<byte[]> ActivateIso14443_4aAsync  (int timeoutMs = 1000, CancellationToken ct = default)
Task<byte[]> ActivateIso14443_3a4aAsync(int timeoutMs = 1000, CancellationToken ct = default)

// Halt
Task HaltIso14443aAsync(int timeoutMs = 1000, CancellationToken ct = default)
Task HaltIso14443bAsync(int timeoutMs = 1000, CancellationToken ct = default)

// APDU 교환
Task<byte[]> ExchangeApduAsync(byte[] apdu, int timeoutMs = 3000, CancellationToken ct = default)
```

---

## Mifare Classic (MAJOR_MIFARE = 0x02)

```csharp
Task<byte[]> ActivateMifareAsync(int timeoutMs = 1000, CancellationToken ct = default)

Task MifareAuthenticateAsync(
    byte blockNo, MifareKeyType keyType, byte[] key,
    int timeoutMs = 1000, CancellationToken ct = default)

Task<byte[]> MifareReadBlockAsync (byte blockNo, int timeoutMs = 1000, CancellationToken ct = default)
Task         MifareWriteBlockAsync(byte blockNo, byte[] data, int timeoutMs = 1000, CancellationToken ct = default)
Task<byte[]> MifareReadSectorAsync(byte sectorNo, int timeoutMs = 1000, CancellationToken ct = default)
```

---

## Mifare Ultralight / NTag (MAJOR_MIFARE_ULTRALIGHT = 0x05)

```csharp
Task<byte[]> ActivateMifareUltralightAsync(int timeoutMs = 1000, CancellationToken ct = default)
Task<byte[]> MifareUltralightReadPageAsync (byte page, int timeoutMs = 1000, CancellationToken ct = default)
Task         MifareUltralightWritePageAsync(byte page, byte[] data, int timeoutMs = 1000, CancellationToken ct = default)
Task         NTagPasswordAuthAsync         (byte[] password, int timeoutMs = 1000, CancellationToken ct = default)

// NTag 전용
Task<byte[]> NTagGetVersionAsync    (int timeoutMs = 1000, CancellationToken ct = default)
Task<byte[]> NTagFastReadAsync      (byte startPage, byte endPage, int timeoutMs = 2000, CancellationToken ct = default)
Task<byte[]> NTagReadCounterAsync   (byte counterNo = 0, int timeoutMs = 1000, CancellationToken ct = default)
Task<byte[]> NTagReadSignatureAsync (int timeoutMs = 2000, CancellationToken ct = default)
Task         NTagWriteAuth0Async    (byte auth0, int timeoutMs = 1000, CancellationToken ct = default)
Task         NTagWriteAccessAsync   (byte access, int timeoutMs = 1000, CancellationToken ct = default)
Task         NTagChangePasswordAsync(byte[] newPassword, byte[] pack, int timeoutMs = 1000, CancellationToken ct = default)

static string ParseNTagType(byte[] versionData)
```

---

## ISO 15693 (MAJOR_ISO15693 = 0x03)

```csharp
Task<byte[]> ReadIso15693UidAsync             (int timeoutMs = 1000, CancellationToken ct = default)
Task<byte[]> ActivateIso15693Async            (int timeoutMs = 1000, CancellationToken ct = default)
Task<byte[]> Iso15693ReadBlockAsync           (byte blockNo, int timeoutMs = 1000, CancellationToken ct = default)
Task         Iso15693WriteBlockAsync          (byte blockNo, byte[] data, int timeoutMs = 1000, CancellationToken ct = default)
Task<byte[]> Iso15693ReadMultipleBlocksAsync  (byte firstBlock, byte blockCount, int timeoutMs = 2000, CancellationToken ct = default)
```

---

## DESFire (MAJOR_DESFIRE = 0x09)

```csharp
// 활성화 / 정보
Task<byte[]> ActivateDesfireAsync    (int timeoutMs = 1000, CancellationToken ct = default)
Task<byte[]> DesfireGetFreeMemoryAsync(int timeoutMs = 1000, CancellationToken ct = default)
Task<byte[]> DesfireGetUidAsync      (int timeoutMs = 1000, CancellationToken ct = default)
Task         DesfireFormatAsync      (int timeoutMs = 3000, CancellationToken ct = default)

// 키 관리
Task DesfireKeySaveAsync(byte keyIndex, byte keyVersion, byte cryptoType, byte[] key,
    int timeoutMs = 1000, CancellationToken ct = default)
Task DesfireKeyInitAsync(int timeoutMs = 1000, CancellationToken ct = default)

// 인증
Task DesfireAuthenticateAesAsync  (byte keyNo = 0, int timeoutMs = 2000, CancellationToken ct = default)
Task DesfireAuthenticate2K3DesAsync(byte keyNo = 0, int timeoutMs = 2000, CancellationToken ct = default)
Task DesfireAuthenticateIsoAsync  (byte keyNo = 0, int timeoutMs = 2000, CancellationToken ct = default)
Task DesfireAuthResetAsync        (int timeoutMs = 1000, CancellationToken ct = default)

// 애플리케이션
Task<byte[]> DesfireGetApplicationIdsAsync(int timeoutMs = 1000, CancellationToken ct = default)
Task         DesfireSelectApplicationAsync(byte[] appId, int timeoutMs = 1000, CancellationToken ct = default)
Task         DesfireSelectRootAsync       (int timeoutMs = 1000, CancellationToken ct = default)
Task         DesfireDeleteApplicationAsync(byte[] appId, int timeoutMs = 1000, CancellationToken ct = default)
Task         DesfireCreateApplicationAsync(byte[] appId, byte keySettings, byte keyCount,
    int timeoutMs = 1000, CancellationToken ct = default)

// 파일
Task<byte[]> DesfireGetFileIdsAsync    (int timeoutMs = 1000, CancellationToken ct = default)
Task<byte[]> DesfireGetFileSettingsAsync(byte fileNo, int timeoutMs = 1000, CancellationToken ct = default)
Task         DesfireCreateStdFileAsync (byte fileNo, byte commSettings, ushort accessRights, int fileSize,
    int timeoutMs = 1000, CancellationToken ct = default)
Task         DesfireDeleteFileAsync    (byte fileNo, int timeoutMs = 1000, CancellationToken ct = default)

// 데이터 파일
Task<byte[]> DesfireReadDataFileAsync (byte fileNo, byte commSettings = 0, int offset = 0, int length = 0,
    int timeoutMs = 2000, CancellationToken ct = default)
Task         DesfireWriteDataFileAsync(byte fileNo, byte[] payload, byte commSettings = 0, int offset = 0,
    int timeoutMs = 2000, CancellationToken ct = default)

// 밸류 파일
Task<byte[]> DesfireReadValueFileAsync  (byte fileNo, byte commSettings = 0,
    int timeoutMs = 1000, CancellationToken ct = default)
Task         DesfireCreditValueFileAsync(byte fileNo, int amount, byte commSettings = 0,
    int timeoutMs = 1000, CancellationToken ct = default)
Task         DesfireDebitValueFileAsync (byte fileNo, int amount, byte commSettings = 0,
    int timeoutMs = 1000, CancellationToken ct = default)

// 트랜잭션
Task DesfireCommitTransactionAsync(int timeoutMs = 1000, CancellationToken ct = default)
Task DesfireAbortTransactionAsync (int timeoutMs = 1000, CancellationToken ct = default)
```

---

## LF 125 kHz (MAJOR_RF125KHZ = 0x0C)

```csharp
Task<byte[]> ReadLf125KhzUidAsync       (int timeoutMs = 2000, CancellationToken ct = default)
Task<byte[]> ReadLf125KhzRawUidAsync    (int timeoutMs = 2000, CancellationToken ct = default)
Task<byte[]> ReadLfIso11784Async        (int timeoutMs = 2000, CancellationToken ct = default)
Task<byte[]> ReadLfIso11784LowDataAsync (int timeoutMs = 2000, CancellationToken ct = default)
Task<byte[]> ReadLfSecomBlockAsync      (int timeoutMs = 2000, CancellationToken ct = default)
Task<byte[]> ReadLfRawBitsAsync         (byte bitLength = 64, int timeoutMs = 2000, CancellationToken ct = default)
Task<byte[]> ReadLfTemicBlockAsync      (byte readBits = 0x40, byte delayMs = 0x0A,
    int timeoutMs = 2000, CancellationToken ct = default)
Task<byte[]> ReadLfTemicLowDataAsync    (int timeoutMs = 2000, CancellationToken ct = default)
Task<byte[]> ReadLfHtrcSamplingTimeAsync(int timeoutMs = 1000, CancellationToken ct = default)
Task<byte[]> AutoTuneLfSamplingAsync    (int timeoutMs = 3000, CancellationToken ct = default)
Task<byte[]> ReadLfTimingParamsAsync    (int timeoutMs = 1000, CancellationToken ct = default)
```

---

## AutoRead (MAJOR_AUTO = 0x20)

```csharp
Task StartAutoReadAsync(int timeoutMs = 1000, CancellationToken ct = default)
Task StopAutoReadAsync (int timeoutMs = 1000, CancellationToken ct = default)
```

---

## ISO 7816 / USIM (MAJOR_ISO7816 = 0x0A)

```csharp
Task<byte[]> UsimActivateAsync  (byte channel = 0, int timeoutMs = 2000, CancellationToken ct = default)
Task         UsimDeactivateAsync(byte channel = 0, int timeoutMs = 1000, CancellationToken ct = default)
Task<byte[]> UsimSendTpduAsync  (byte[] tpdu, byte channel = 0, int timeoutMs = 3000, CancellationToken ct = default)
Task<byte[]> UsimReadSerialAsync(int timeoutMs = 1000, CancellationToken ct = default)
```

---

## Bluetooth / BLE (MAJOR_BLE_CONFIG = 0x12)

```csharp
Task<string>  BleReadNameAsync               (int timeoutMs = 1000, CancellationToken ct = default)
Task          BleWriteNameAsync              (string name, int timeoutMs = 1000, CancellationToken ct = default)
Task<byte[]>  BleReadMacAddressAsync         (int timeoutMs = 1000, CancellationToken ct = default)
Task<byte[]>  BleReadTxPowerAsync            (int timeoutMs = 1000, CancellationToken ct = default)
Task          BleWriteTxPowerAsync           (byte powerIndex, int timeoutMs = 1000, CancellationToken ct = default)
Task<byte[]>  BleReadGapConnectParamsAsync   (int timeoutMs = 1000, CancellationToken ct = default)
Task          BleSystemResetAsync            (int timeoutMs = 1000, CancellationToken ct = default)
Task<byte[]>  BleReadCentralEnableAsync      (int timeoutMs = 1000, CancellationToken ct = default)
Task          BleCentralScanStartAsync       (int timeoutMs = 1000, CancellationToken ct = default)
Task          BleCentralScanStopAsync        (int timeoutMs = 1000, CancellationToken ct = default)
Task<byte[]>  BleCentralScanListAsync        (int timeoutMs = 2000, CancellationToken ct = default)
Task          BlePeripheralAdvertisingStartAsync(int timeoutMs = 1000, CancellationToken ct = default)
Task          BlePeripheralAdvertisingStopAsync (int timeoutMs = 1000, CancellationToken ct = default)

static string TxPowerToString(byte powerIndex)
```

---

## 릴레이 보드 (MAJOR_RELAY = 0x22)

```csharp
// 입력 읽기
Task<byte[]> RelayReadAllInputsAsync(int timeoutMs = 1000, CancellationToken ct = default)
Task<byte[]> RelayReadInputAsync    (byte inputNo, int timeoutMs = 1000, CancellationToken ct = default)

// 출력 읽기
Task<byte[]> RelayReadAllOutputsAsync(int timeoutMs = 1000, CancellationToken ct = default)
Task<byte[]> RelayReadOutputAsync    (byte relayNo, int timeoutMs = 1000, CancellationToken ct = default)

// 출력 제어
Task RelayWriteAllOutputsAsync(byte mask, int timeoutMs = 1000, CancellationToken ct = default)
Task RelayWriteOutputAsync    (byte relayNo, bool on, int timeoutMs = 1000, CancellationToken ct = default)
Task RelayAllOffAsync         (int timeoutMs = 1000, CancellationToken ct = default)
Task RelayAllOnAsync          (int timeoutMs = 1000, CancellationToken ct = default)

// 설정
Task RelaySetAutoOffTimeAsync(byte relayNo, ushort timeMs, int timeoutMs = 1000, CancellationToken ct = default)

// 유틸리티 (static)
static bool GetInputState(byte mask, byte inputNo)
static bool GetRelayState(byte mask, byte relayNo)
```

---

## Raw 명령

```csharp
Task<byte[]> SendRawCommandAsync(
    byte cmd1,
    byte cmd2,
    byte[]? data = null,
    int timeoutMs = 1000,
    CancellationToken ct = default)
```

---

## 모델 / 열거형

### CardType

```csharp
public enum CardType
{
    Unknown, Iso14443a, Iso14443b, Iso14443ab,
    Iso15693, MifareClassic, MifareUltralight,
    MifareDesfire, Felica, Lf125Khz
}
```

### MifareKeyType

```csharp
public enum MifareKeyType : byte { KeyA = 0x01, KeyB = 0x02 }
```

### TagDetectedEventArgs

```csharp
public sealed class TagDetectedEventArgs : EventArgs
{
    public CardType CardType { get; }
    public byte[]   Uid      { get; }
    public string   UidHex   { get; }  // 대시 없는 16진수
    public byte     Cmd1     { get; }
    public byte     Cmd2     { get; }
    public byte[]   RawData  { get; }
}
```

### 예외

```csharp
// Iksung.Reader.Exceptions 네임스페이스
IksungProtocolException  // 리더기 오류 응답 (State != 0)
IksungTimeoutException   // 응답 타임아웃
```

---

[← 예외 처리 및 고급 사용](13-error-handling.md) | [목차로 →](README.md)
