namespace Iksung.Reader.Internals.Protocol;

internal static class Constants
{
    public const byte STX = 0x01;
    public const byte ETX = 0x03;

    public const byte MAJOR_RF125KHZ          = 0x0C;
    public const byte MAJOR_COMMON            = 0x00;
    public const byte MAJOR_ISO14443AB        = 0x01;
    public const byte MAJOR_MIFARE            = 0x02;
    public const byte MAJOR_ISO15693          = 0x03;
    public const byte MAJOR_MIFARE_ULTRALIGHT = 0x05;
    public const byte MAJOR_MIFARE_PLUS       = 0x07;
    public const byte MAJOR_DESFIRE           = 0x09;
    public const byte MAJOR_ISO7816           = 0x0A;
    public const byte MAJOR_CRYPT             = 0x10;
    public const byte MAJOR_ACU               = 0x14;
    public const byte MAJOR_AUTO              = 0x20;
    public const byte MAJOR_RELAY             = 0x22;
    public const byte MAJOR_RELAY_CONFIG      = 0x23;
    public const byte MAJOR_TAG_CONFIG        = 0x13;
    public const byte MAJOR_BLE_CONFIG        = 0x12;
    public const byte MAJOR_BLE_MESAGE        = 0x1F;
    public const byte MAJOR_TUNING            = 0x70;
    public const byte MAJOR_BOOT              = 0x77;

    // ───── MAJOR_TAG_CONFIG (0x13) ─────
    public const byte TAG_STOP_TIME_READ                 = 0x40;
    public const byte TAG_STOP_TIME_SAVE                 = 0x41;
    public const byte TAG_START_TIME_READ                = 0x42;
    public const byte TAG_START_TIME_SAVE                = 0x43;

    // ───── MAJOR_TUNING (0x70) ─────
    public const byte TUNING_ISO14443A_TAG_READ          = 0x21;
    public const byte TUNING_EM_125KHZ_TAG_READ          = 0x24;
    public const byte TUNING_POLLING_STOP                = 0x25;
    public const byte TUNING_POLLING_ENABLE              = 0x26;
    public const byte TUNING_RFON                        = 0x30;
    public const byte TUNING_RFOFF                       = 0x31;
    public const byte TUNING_125KHZ_RFON                 = 0x32;
    public const byte TUNING_125KHZ_RFOFF                = 0x33;
    public const byte TUNING_BLUETOOTH_CERTIFICATION_SEND = 0x40;
    public const byte TUNING_SAM_POLLING_START           = 0x41;
    public const byte TUNING_SAM_POLLING_STOP            = 0x42;
    public const byte TUNING_ISO14443A_MODWIDTH_READ     = 0x50;
    public const byte TUNING_ISO14443A_MODWIDTH_WRITE    = 0x51;

    // ───── MAJOR_BLE_CONFIG (0x12) ─────
    public const byte BLE_CFG_NAME_READ                       = 0x10;
    public const byte BLE_CFG_NAME_WRITE                      = 0x11;
    public const byte BLE_CFG_GAP_CONNECT_READ                = 0x12;
    public const byte BLE_CFG_GAP_CONNECT_WRITE               = 0x13;
    public const byte BLE_CFG_TX_POWER_READ                   = 0x14;
    public const byte BLE_CFG_TX_POWER_WRITE                  = 0x15;
    public const byte BLE_CFG_SYSTEM_RESET                    = 0x18;
    public const byte BLE_CFG_CENTRAL_PHYS_UPDATE_READ        = 0x1B;
    public const byte BLE_CFG_CENTRAL_PHYS_UPDATE_SAVE        = 0x1C;
    public const byte BLE_CFG_MAC_ADDRESS_READ                = 0x42;

    // Central 탭
    public const byte BLE_CFG_CENTRAL_ENABLE_READ             = 0x20;
    public const byte BLE_CFG_CENTRAL_ENABLE_WRITE            = 0x21;
    public const byte BLE_CFG_CENTRAL_UUID_READ               = 0x22;
    public const byte BLE_CFG_CENTRAL_UUID_SAVE               = 0x23;
    public const byte BLE_CFG_CENTRAL_UUID_DISCONNECT         = 0x24;
    public const byte BLE_CFG_CENTRAL_CONNECTED_RSSI_READ     = 0x25;
    public const byte BLE_CFG_CENTRAL_MATCHED_RSSI_READ       = 0x26;
    public const byte BLE_CFG_CENTRAL_SCAN_START              = 0x27;
    public const byte BLE_CFG_CENTRAL_SCAN_STOP               = 0x28;
    public const byte BLE_CFG_CENTRAL_SCAN_LIST               = 0x29;
    public const byte BLE_CFG_CENTRAL_CONNECT_STATE           = 0x2A;
    public const byte BLE_CFG_CENTRAL_SEND_DATA               = 0x2B;
    public const byte BLE_CFG_CENTRAL_MATCHED_CONNECT         = 0x2C;
    public const byte BLE_CFG_CENTRAL_CONNECT_PARAMS_READ     = 0x2D;
    public const byte BLE_CFG_CENTRAL_CONNECT_PARAMS_WRITE    = 0x2E;

    // Peripheral 탭
    public const byte BLE_CFG_PERIPHERAL_ENABLE_READ          = 0x30;
    public const byte BLE_CFG_PERIPHERAL_ENABLE_WRITE         = 0x31;
    public const byte BLE_CFG_PERIPHERAL_UUID_READ            = 0x32;
    public const byte BLE_CFG_PERIPHERAL_UUID_SAVE            = 0x33;
    public const byte BLE_CFG_PERIPHERAL_CONNECT_STATE        = 0x34;
    public const byte BLE_CFG_PERIPHERAL_SEND_DATA            = 0x35;
    public const byte BLE_CFG_PERIPHERAL_CONNECT_RSSI_READ    = 0x36;
    public const byte BLE_CFG_PERIPHERAL_ADVERTSING_START     = 0x37;
    public const byte BLE_CFG_PERIPHERAL_ADVERTSING_STOP      = 0x38;
    public const byte BLE_CFG_PERIPHERAL_DISCONNECT           = 0x39;
    public const byte BLE_CFG_PERIPHERAL_ADV_INTERVAL_READ    = 0x3A;
    public const byte BLE_CFG_PERIPHERAL_ADV_INTERVAL_SAVE    = 0x3B;

    // Option 탭
    public const byte BLE_CFG_OUTPUT_INTERFACE_READ           = 0x19;
    public const byte BLE_CFG_OUTPUT_INTERFACE_SAVE           = 0x1A;
    public const byte BLE_CFG_CENTRAL_INPUT_PROTOCOL_READ     = 0x1D;
    public const byte BLE_CFG_CENTRAL_INPUT_PROTOCOL_SAVE     = 0x1E;
    public const byte BLE_CFG_RECIVED_TIMEOUT_READ            = 0x52;
    public const byte BLE_CFG_RECIVED_TIMEOUT_SAVE            = 0x53;
    public const byte BLE_CFG_PERIPHERAL_NO_PROTOCOL_READ     = 0x55;
    public const byte BLE_CFG_PERIPHERAL_NO_PROTOCOL_SAVE     = 0x56;
    public const byte BLE_CFG_CENTRAL_NO_PROTOCOL_READ        = 0x57;
    public const byte BLE_CFG_CENTRAL_NO_PROTOCOL_SAVE        = 0x58;

    // Security 탭
    public const byte BLE_CFG_SECURITY_LEVELS_READ            = 0x60;
    public const byte BLE_CFG_SECURITY_LEVELS_WRITE           = 0x61;
    public const byte BLE_CFG_USER_SECURITY_LEVELS_READ       = 0x62;
    public const byte BLE_CFG_USER_SECURITY_LEVELS_WRITE      = 0x63;
    public const byte BLE_CFG_USER_SECURITY_RANDOM            = 0x64;
    public const byte BLE_CFG_USER_SECURITY_AUTH              = 0x65;
    public const byte BLE_CFG_USER_SECURITY_AUTH_STATE        = 0x66;

    // Bluetooth Card 탭
    public const byte BLE_CFG_BLECARD_USING_READ              = 0x70;
    public const byte BLE_CFG_BLECARD_USING_SAVE              = 0x71;
    public const byte BLE_CFG_BLECARD_RSSI_READ               = 0x72;
    public const byte BLE_CFG_BLECARD_RSSI_SAVE               = 0x73;
    public const byte BLE_CFG_BLECARD_KEY_SAVE                = 0x74;

    // ───── MAJOR_BLE_MESAGE (0x1F) ─────
    public const byte BLE_IS_CONNECT                     = 0x52;

    // ───── MAJOR_COMMON (0x00) ─────
    public const byte COMMON_WDT_TIMEOUT_READ    = 0x07;
    public const byte COMMON_WDT_TIMEOUT_WRITE   = 0x08;
    public const byte COMMON_UNIQUE_ID           = 0x0F;
    public const byte COMMON_VERSION             = 0x10;
    public const byte COMMON_BUZZER              = 0x11;
    public const byte COMMON_SERIAL_BAUD_CHANGE  = 0x12;
    public const byte COMMON_SAK_TYPE            = 0x14;
    public const byte COMMON_ATS                 = 0x15;
    public const byte COMMON_RFON                = 0x20;
    public const byte COMMON_RFOFF               = 0x21;
    public const byte COMMON_TAG_TYPE            = 0x22;
    public const byte COMMON_ALL_UID_READ        = 0x23;
    public const byte COMMON_ISO14443A_UID_READ  = 0x24;
    public const byte COMMON_ISO14443B_UID_READ  = 0x25;
    public const byte COMMON_FELICA_UID_READ     = 0x26;
    public const byte COMMON_ISO15693_UID_READ   = 0x27;
    public const byte COMMON_ALL_CARD_TYPE       = 0x2A;
    public const byte COMMON_TMONEY_SERIAL       = 0x30;
    public const byte COMMON_CASHBEE_SERIAL      = 0x31;
    public const byte COMMON_KCASH_SERIAL        = 0x32;
    public const byte COMMON_ALL_CASH_SERIAL     = 0x33;
    public const byte COMMON_RAILPLUS_SERIAL     = 0x34;

    public const byte STATE_SUCCESS = 0x01;
    public const byte STATE_FAIL    = 0xFF;
    public const byte BUZZER_FLAG   = 0x80;

    // ───── MAJOR_CRYPT (0x10) ─────
    public const byte CRYPT_RANDOM_SEED   = 0x30;
    public const byte CRYPT_RANDOM_CREATE = 0x31;
    public const byte CRYPT_SHA256        = 0x32;
    public const byte CRYPT_AES_KEY_SAVE  = 0x35;
    public const byte CRYPT_AES_IV_SAVE   = 0x36;
    public const byte CRYPT_AES_DECRYPT   = 0x37;
    public const byte CRYPT_AES_ENCRYPT   = 0x38;
    public const byte CRYPT_3DES_KEY_SAVE = 0x39;
    public const byte CRYPT_3DES_IV_SAVE  = 0x3A;
    public const byte CRYPT_3DES_DECRYPT  = 0x3B;
    public const byte CRYPT_3DES_ENCRYPT  = 0x3C;

    public const byte CIPHER_MODE_ECB = 0x00;
    public const byte CIPHER_MODE_CBC = 0x01;
    public const byte CIPHER_MODE_CFB = 0x02;

    // ───── MAJOR_MIFARE (0x02) ─────
    public const byte MIFARE_ACTIVE              = 0x20;
    public const byte MIFARE_AUTHENTICATE        = 0x21;
    public const byte MIFARE_BLOCK_READ          = 0x22;
    public const byte MIFARE_SECTOR_READ         = 0x23;
    public const byte MIFARE_BLOCK_WRITE         = 0x24;
    public const byte MIFARE_SECTOR_WRITE        = 0x25;
    public const byte MIFARE_VALUE_WRITE         = 0x26;
    public const byte MIFARE_VALUE_READ          = 0x27;
    public const byte MIFARE_INCREMENT           = 0x28;
    public const byte MIFARE_DECREMENT           = 0x29;
    public const byte MIFARE_TRANSFER            = 0x2A;
    public const byte MIFARE_RESTORE             = 0x2B;
    public const byte MIFARE_INC_TRANSFER        = 0x2C;
    public const byte MIFARE_DEC_TRANSFER        = 0x2D;
    public const byte MIFARE_RESTORE_TRANSFER    = 0x2E;
    public const byte MIFARE_AUTH_SECTOR_READ_3  = 0x35;
    public const byte MIFARE_AUTH_SECTOR_WRITE_3 = 0x36;
    public const byte MIFARE_AUTH_BLOCK_READ     = 0x37;
    public const byte MIFARE_AUTH_BLOCK_WRITE    = 0x38;
    public const byte MIFARE_AUTH_SECTOR_READ_4  = 0x39;
    public const byte MIFARE_AUTH_SECTOR_WRITE_4 = 0x3E;
    public const byte MIFARE_AUTH_VALUE_WRITE    = 0x77;
    public const byte MIFARE_AUTH_VALUE_READ     = 0x78;
    public const byte MIFARE_AUTH_INC_TRANSFER   = 0x7D;
    public const byte MIFARE_AUTH_DEC_TRANSFER   = 0x7E;
    public const byte MIFARE_KEY_A = 0x01;
    public const byte MIFARE_KEY_B = 0x02;

    public const byte AUTO_MIFARE_KEY_SAVE = 0x3F;

    public const byte MIFARE_KEYSAVE_SECTOR_READ   = 0x60;
    public const byte MIFARE_KEYSAVE_SECTOR_WRITE  = 0x61;
    public const byte MIFARE_KEYSAVE_BLOCK_READ    = 0x62;
    public const byte MIFARE_KEYSAVE_BLOCK_WRITE   = 0x63;
    public const byte MIFARE_KEYSAVE_VALUE_WRITE   = 0x67;
    public const byte MIFARE_KEYSAVE_VALUE_READ    = 0x68;
    public const byte MIFARE_KEYSAVE_INC_TRANSFER  = 0x6D;
    public const byte MIFARE_KEYSAVE_DEC_TRANSFER  = 0x6E;

    // ───── MAJOR_MIFARE_ULTRALIGHT (0x05) ─────
    public const byte ULC_ACTIVE              = 0x20;
    public const byte ULC_AUTHENTICATE        = 0x21;
    public const byte ULC_BLOCK_READ          = 0x22;
    public const byte ULC_BLOCK_WRITE         = 0x23;
    public const byte ULC_OTP_READ            = 0x25;
    public const byte ULC_OTP_WRITE           = 0x26;
    public const byte ULC_COUNTER_READ        = 0x27;
    public const byte ULC_COUNTER_INC_1       = 0x28;
    public const byte ULC_COUNTER_INC_ADD     = 0x29;
    public const byte ULC_AUTH0_AUTH1_READ    = 0x2D;
    public const byte ULC_AUTH0_WRITE         = 0x2E;
    public const byte ULC_AUTH1_WRITE         = 0x2F;

    // Mifare NTag (CMD1=0x05)
    public const byte NTAG_PASSWORD_AUTH      = 0x30;
    public const byte NTAG_COUNTER_READ       = 0x31;
    public const byte NTAG_GET_VERSION        = 0x32;
    public const byte NTAG_READ_SIGN_ECC      = 0x33;
    public const byte NTAG_FAST_READ          = 0x34;
    public const byte NTAG_AUTH0_WRITE        = 0x35;
    public const byte NTAG_ACCESS_WRITE       = 0x36;
    public const byte NTAG_AUTH0_ACCESS_READ  = 0x37;
    public const byte NTAG_COUNTER_ENABLE     = 0x38;
    public const byte NTAG_COUNTER_PROTECT    = 0x39;
    public const byte NTAG_COUNTER_STATE_READ = 0x3A;
    public const byte NTAG_PASSWORD_CHANGE    = 0x3B;
    public const byte NTAG_16BYTE_WRITE       = 0x3C;
    public const byte NTAG_ACTIVE_READ        = 0x40;
    public const byte NTAG_ACTIVE_SINGLE_WRITE= 0x41;
    public const byte NTAG_ACTIVE_16BYTE_WRITE= 0x42;

    // ───── MAJOR_MIFARE_PLUS (0x07) ─────
    public const byte MFP_SL3_ACTIVE          = 0x20;
    public const byte MFP_SL3_BLOCK_READ      = 0x21;
    public const byte MFP_SL3_BLOCK_WRITE     = 0x22;
    public const byte MFP_SL3_READ_VALUE      = 0x23;
    public const byte MFP_SL3_WRITE_VALUE     = 0x24;
    public const byte MFP_SL3_INCREMENT       = 0x25;
    public const byte MFP_SL3_DECREMENT       = 0x26;
    public const byte MFP_SL3_TRANSFER        = 0x27;
    public const byte MFP_SL3_RESTORE         = 0x28;
    public const byte MFP_SL3_INC_TRANSFER    = 0x29;
    public const byte MFP_SL3_DEC_TRANSFER    = 0x2A;
    public const byte MFP_SL3_AUTHENTICATE    = 0x2B;
    public const byte MFP_SL3_KEY_CHANGE      = 0x2C;
    public const byte MFP_SL2_BLOCK_READ      = 0x41;
    public const byte MFP_SL2_BLOCK_WRITE     = 0x42;
    public const byte MFP_SL2_PLUS_AUTH       = 0x43;
    public const byte MFP_SL2_KEY_CHANGE      = 0x44;
    public const byte MFP_SL2_MIFARE_AUTH     = 0x45;
    public const byte MFP_KEY_A = 0x01;
    public const byte MFP_KEY_B = 0x02;

    // ───── MAJOR_ISO15693 (0x03) ─────
    public const byte ISO15693_ACTIVE                            = 0x20;
    public const byte ISO15693_SINGLE_BLOCK_READ                 = 0x21;
    public const byte ISO15693_MULTIPLE_BLOCK_READ               = 0x22;
    public const byte ISO15693_SINGLE_BLOCK_WRITE                = 0x23;
    public const byte ISO15693_MULTIPLE_BLOCK_WRITE              = 0x24;
    public const byte ISO15693_STAYQUIET                         = 0x25;
    public const byte ISO15693_SELECT                            = 0x26;
    public const byte ISO15693_RESETTOREADY                      = 0x27;
    public const byte ISO15693_BLOCK_LOCK                        = 0x28;
    public const byte ISO15693_WRITE_AFI                         = 0x29;
    public const byte ISO15693_LOCK_AFI                          = 0x2A;
    public const byte ISO15693_WRITE_DSFID                       = 0x2B;
    public const byte ISO15693_LOCK_DSFID                        = 0x2C;
    public const byte ISO15693_GETSYSTEMINFORMATION              = 0x2D;
    public const byte ISO15693_GETMULTIPLEBLOCKSECURITYSTATUS    = 0x2E;
    public const byte ISO15693_ACTIVE_MULTIPLE_BLOCK_READ        = 0x61;
    public const byte ISO15693_ACTIVE_MULTIPLE_BLOCK_WRITE       = 0x62;

    // ICODE
    public const byte ICODE_GET_RANDOM_NUMBER                    = 0x30;
    public const byte ICODE_SET_PASSWD                           = 0x31;
    public const byte ICODE_WRITE_PASSWD                         = 0x32;
    public const byte ICODE_LOCK_PASSWD                          = 0x33;
    public const byte ICODE_PROTECT_PAGE                         = 0x34;
    public const byte ICODE_LOCK_PROTECT_PAGE                    = 0x35;
    public const byte ICODE_DESTROY                              = 0x36;
    public const byte ICODE_ENABLE_PRIVACY                       = 0x37;
    public const byte ICODE_PROTECT_BLOCK_STATE                  = 0x38;
    public const byte ICODE_AUTO_SET_PASSWD                      = 0x39;
    public const byte ICODE_INVENTORY_READ                       = 0x40;
    public const byte ICODE_FAST_INVENTORY_READ                  = 0x41;
    public const byte ICODE_SET_EAS                              = 0x42;
    public const byte ICODE_RESET_EAS                            = 0x43;
    public const byte ICODE_PROTECT_EAS                          = 0x44;
    public const byte ICODE_LOCK_EAS                             = 0x45;
    public const byte ICODE_EAS_ALARM                            = 0x46;
    public const byte ICODE_WRITE_EAS_ID                         = 0x47;
    public const byte ICODE_READ_EPC                             = 0x48;
    public const byte ICODE_GET_NXP_SYSTEM_INFORMATION           = 0x49;
    public const byte ICODE_STAY_QUIET_PERSISTENT                = 0x4A;
    public const byte ICODE_READ_SIGNATURE                       = 0x4B;
    public const byte ICODE_64BIT_PASSWORD_SET                   = 0x50;
    public const byte ICODE_16BIT_COUNTER_READ                   = 0x51;
    public const byte ICODE_16BIT_COUNTER_INCREMENT              = 0x52;
    public const byte ICODE_16BIT_COUNTER_PROTECT_SET            = 0x53;
    public const byte ICODE_16BIT_COUNTER_PROTECT_CLEAR          = 0x54;
    public const byte ICODE_ID_READ    = 0x01;
    public const byte ICODE_ID_WRITE   = 0x02;
    public const byte ICODE_ID_PRIVACY = 0x04;
    public const byte ICODE_ID_DESTROY = 0x08;
    public const byte ICODE_ID_EAS     = 0x10;
    public const byte ICODE_PROT_RW_PUBLIC      = 0x00;
    public const byte ICODE_PROT_R_PUBLIC_W_PWD = 0x01;
    public const byte ICODE_PROT_RW_PWD         = 0x02;
    public const byte ICODE_PROT_RW_DENY        = 0x03;

    // ───── MAJOR_ISO14443AB (0x01) ─────
    public const byte ISO14443A_ACTIVE          = 0x20;
    public const byte ISO14443_4A_106_ACTIVE    = 0x21;
    public const byte ISO14443_3A_4A_ACTIVE     = 0x22;
    public const byte ISO14443B_ACTIVE          = 0x23;
    public const byte ISO14443AB_ACTIVE         = 0x24;
    public const byte ISO14443A_HALT            = 0x2A;
    public const byte ISO14443B_HALT            = 0x2B;
    public const byte ISO14443A4_DESELECT       = 0x2C;
    public const byte ISO14443B_DESELECT        = 0x2D;
    public const byte ISO14443P4_DATA_EXCHANGE  = 0x30;

    // ───── MAJOR_DESFIRE (0x09) ─────
    public const byte DESFIRE_ACTIVE                   = 0x20;
    public const byte DESFIRE_KEYSAVE                  = 0x21;
    public const byte DESFIRE_AUTH_2K3DES              = 0x22;
    public const byte DESFIRE_AUTH_ISO                 = 0x23;
    public const byte DESFIRE_AUTH_AES                 = 0x24;
    public const byte DESFIRE_AUTH_KEY_CHANGE          = 0x25;
    public const byte DESFIRE_GET_APPIDS               = 0x26;
    public const byte DESFIRE_SELECT_APPIDS            = 0x27;
    public const byte DESFIRE_DELETE_APPIDS            = 0x28;
    public const byte DESFIRE_CREATE_APPIDS            = 0x29;
    public const byte DESFIRE_CREATE_STD_FILE          = 0x2A;
    public const byte DESFIRE_CREATE_VALUE_FILE        = 0x2B;
    public const byte DESFIRE_CREATE_RECORD_FILE       = 0x2C;
    public const byte DESFIRE_GET_FILE_ID              = 0x2D;
    public const byte DESFIRE_GET_FILE_SETTINGS        = 0x2E;
    public const byte DESFIRE_DELETE_FILE              = 0x2F;
    public const byte DESFIRE_FREE_MEMORY              = 0x30;
    public const byte DESFIRE_CONFIG_1                 = 0x31;
    public const byte DESFIRE_CONFIG_2                 = 0x32;
    public const byte DESFIRE_WRITE_DATA_FILE          = 0x35;
    public const byte DESFIRE_READ_DATA_FILE           = 0x36;
    public const byte DESFIRE_SET_FILE_CHANGE          = 0x3B;
    public const byte DESFIRE_GET_UID                  = 0x40;
    public const byte DESFIRE_FORMAT                   = 0x41;
    public const byte DESFIRE_SELECT_ROOT              = 0x42;
    public const byte DESFIRE_CHANGE_KEY_ACCESS_RIGHT  = 0x45;
    public const byte DESFIRE_AUTH_RESET               = 0x46;
    public const byte DESFIRE_ACTIVE_SELECT_AUTH       = 0x47;
    public const byte DESFIRE_KEY_INIT                 = 0x48;
    public const byte DESFIRE_WRITE_VALUE_FILE         = 0x50;
    public const byte DESFIRE_READ_VALUE_FILE          = 0x51;
    public const byte DESFIRE_DEBIT_VALUE_FILE         = 0x52;
    public const byte DESFIRE_LIMITCREDIT_VALUE_FILE   = 0x53;
    public const byte DESFIRE_COMMIT_TRANSACTION       = 0x54;
    public const byte DESFIRE_ABORT_TRANSACTION        = 0x55;
    public const byte DESFIRE_WRITE_RECORD_FILE        = 0x58;
    public const byte DESFIRE_READ_RECORD_FILE         = 0x59;
    public const byte DESFIRE_CLEAR_RECORD_FILE        = 0x5A;
    public const byte DESFIRE_CARD_ISSUE_FLASH_KEY_SAVE  = 0x60;
    public const byte DESFIRE_CARD_ISSUE_FLASH_KEY_CLEAR = 0x61;
    public const byte DESFIRE_CARD_ISSUE_CREATE          = 0x62;
    public const byte DESFIRE_CARD_ISSUE_FORMAT          = 0x63;
    public const byte DESFIRE_CARD_ISSUE_BLOCK_READ      = 0x64;
    public const byte DESFIRE_CARD_ISSUE_BLOCK_WRITE     = 0x65;
    public const byte DESFIRE_READ_WRITE_CARD_ISSUE_SAVE = 0x66;
    public const byte DESFIRE_ENC_AES_128    = 0x00;
    public const byte DESFIRE_ENC_2K3DES_16  = 0x04;
    public const byte DESFIRE_ENC_3K3DES_24  = 0x05;
    public const byte DESFIRE_COMM_PLAIN     = 0x00;
    public const byte DESFIRE_COMM_MAC       = 0x10;
    public const byte DESFIRE_COMM_ENCRYPT   = 0x30;

    // ───── MAJOR_AUTO (0x20) ─────
    public const byte AUTOSETUP_FILESIZE                       = 0x01;
    public const byte AUTOSETUP_BUZZER_HZ_READ                 = 0x03;
    public const byte AUTOSETUP_BUZZER_HZ_SAVE                 = 0x04;
    public const byte AUTOSETUP_WDG_TIME_READ                  = 0x05;
    public const byte AUTOSETUP_WDG_TIME_SAVE                  = 0x06;
    public const byte AUTOSETUP_NFC_POWER_READ                 = 0x07;
    public const byte AUTOSETUP_NFC_POWER_SAVE                 = 0x08;
    public const byte AUTOSETUP_UART_CHANGE                    = 0x09;
    public const byte AUTOSETUP_WIEGAND_IO_CHANGE              = 0x0A;
    public const byte AUTOSETUP_USB_KEYBOARD_DELAY_READ        = 0x0B;
    public const byte AUTOSETUP_USB_KEYBOARD_DELAY_WRITE       = 0x0C;
    public const byte AUTOSETUP_DUMMY                          = 0x10;
    public const byte AUTOSETUP_BUZZER_INIT_ON                 = 0x11;
    public const byte AUTOSETUP_BUZZER_INIT_OFF                = 0x12;
    public const byte AUTOSETUP_POLLING                        = 0x20;
    public const byte AUTOSETUP_POLLING_CLEAR                  = 0x21;
    public const byte AUTOSETUP_POLLING_START                  = 0x22;
    public const byte AUTOSETUP_POLLING_STOP                   = 0x23;
    public const byte AUTOSETUP_POLLING_RAM_START              = 0x24;
    public const byte AUTOSETUP_POLLING_RAM_STOP               = 0x25;
    public const byte AUTOSETUP_EXPANSION_POLLING              = 0x26;
    public const byte AUTOSETUP_CCID_POLLING                   = 0x27;
    public const byte AUTOSETUP_ISO14443A_UID                  = 0x30;
    public const byte AUTOSETUP_ISO14443B_UID                  = 0x31;
    public const byte AUTOSETUP_ISO14443A_SMART_NUMBER         = 0x32;
    public const byte AUTOSETUP_ISO14443A_MIFARE1              = 0x33;
    public const byte AUTOSETUP_ISO14443A_MIFARE2              = 0x34;
    public const byte AUTOSETUP_ISO14443A_MIFARE3              = 0x35;
    public const byte AUTOSETUP_ISO14443A_MIFARE4              = 0x36;
    public const byte AUTOSETUP_ISO15693_BLOCK                 = 0x37;
    public const byte AUTOSETUP_ISO15693_UID                   = 0x38;
    public const byte AUTOSETUP_ISO15693_SLIX2_PASSWORD        = 0x39;
    public const byte AUTOSETUP_MIFARE_PLUS                    = 0x3A;
    public const byte AUTOSETUP_MIFARE_NTAG                    = 0x3B;
    public const byte AUTOSETUP_MIFARE_NTAG_PASSWORD           = 0x3C;
    public const byte AUTOSETUP_MIFARE_UL                      = 0x3D;
    public const byte AUTOSETUP_MIFARE_UL_PASSWORD             = 0x3E;
    public const byte AUTOSETUP_HCE1                           = 0x41;
    public const byte AUTOSETUP_HCE2                           = 0x42;
    public const byte AUTOSETUP_HCE3                           = 0x43;
    public const byte AUTOSETUP_HCE_CRYPT_KEY_SAVE             = 0x45;
    public const byte AUTOSETUP_HCE_READDATA1                  = 0x47;
    public const byte AUTOSETUP_HCE_READDATA2                  = 0x48;
    public const byte AUTOSETUP_HCE_READDATA3                  = 0x49;
    public const byte AUTOSETUP_BALANCE                        = 0x4A;
    public const byte AUTOSETUP_CNC_BALANCE                    = 0x4B;
    public const byte AUTOSETUP_FELICA_UID                     = 0x4C;
    public const byte AUTOSETUP_HCE_READDATA2_1                = 0x4D;
    public const byte AUTOSETUP_HCE_READDATA2_2                = 0x4E;
    public const byte AUTOSETUP_HCE_READDATA2_3                = 0x4F;
    public const byte AUTOSETUP_ISO14443A_MIFARE1_BLOCK_READ   = 0x54;
    public const byte AUTOSETUP_ISO14443A_MIFARE2_BLOCK_READ   = 0x55;
    public const byte AUTOSETUP_ISO14443A_MIFARE3_BLOCK_READ   = 0x56;
    public const byte AUTOSETUP_ISO14443A_MIFARE4_BLOCK_READ   = 0x57;
    public const byte AUTOSETUP_WIEGAND_PLUS_READ              = 0x58;
    public const byte AUTOSETUP_WIEGAND_PLUS_SAVE              = 0x59;
    public const byte AUTOSETUP_USB_KEYBOARD_READ              = 0x5A;
    public const byte AUTOSETUP_USB_KEYBOARD_SAVE              = 0x5B;
    public const byte AUTOSETUP_LF_SECOM_LONGDATA_UID          = 0x65;
    public const byte AUTOSETUP_LF_EM_UID                      = 0x66;
    public const byte AUTOSETUP_DESFIRE_BLOCK_READ             = 0x6A;
    public const byte AUTOSETUP_HCE3_IKUSNG                    = 0x6B;
    public const byte AUTOSETUP_TAG_EMULATOR_ENABLE            = 0x6C;
    public const byte AUTOSETUP_DESFIRE_BLOCK_READ2            = 0x6D;
    public const byte AUTO_PROTOCOL_IKSUNG      = 0x00;
    public const byte AUTO_PROTOCOL_STX02       = 0x01;
    public const byte AUTO_PROTOCOL_MODBUS      = 0x02;
    public const byte AUTO_PROTOCOL_IKSUNG_CCID = 0x10;
    public const byte AUTO_IF_RS232               = 0x01;
    public const byte AUTO_IF_USB_TO_SERIAL       = 0x02;
    public const byte AUTO_IF_HID_KEYBOARD        = 0x04;
    public const byte AUTO_IF_HID_KEYBOARD_ENTER  = 0x08;
    public const byte AUTO_IF_WIEGAND             = 0x10;
    public const byte AUTO_IF_BLUETOOTH_PERIPHERAL = 0x20;
    public const byte AUTO_IF_BLUETOOTH_CENTRAL    = 0x40;

    // ───── MAJOR_ISO7816 (0x0A) ─────
    public const byte USIM_ACTIVE                  = 0x20;
    public const byte USIM_DEACTIVE                = 0x21;
    public const byte USIM_TPDU_COMMAND            = 0x22;
    public const byte USIM_CNCRF_SERIAL_READ       = 0x31;
    public const byte USIM_CNCRF_SERIAL_WRITE      = 0x32;
    public const byte USIM_WWT_BAUD                = 0x40;
    public const byte USIM_WWT_BAUD_READ           = 0x41;

    // ───── MAJOR_RF125KHZ (0x0C) ─────
    public const byte RF125_SECOM_BLOCK_READ          = 0x20;
    public const byte RF125_ISO11784_READ             = 0x21;
    public const byte RF125_ISO11784_WRITE            = 0x22;
    public const byte RF125_UNIQUE_ID                 = 0x30;
    public const byte RF125_EM410X_UNIQUE_ID          = 0x32;
    public const byte RF125_LOWDATA_READ              = 0x50;
    public const byte RF125_TEMIC_BLOCK_READ          = 0x51;
    public const byte RF125_TEMIC_LOWDATA_READ        = 0x52;
    public const byte RF125_READ_TIMMING              = 0x60;
    public const byte RF125_WRITE_TIMMING             = 0x61;
    public const byte RF125_READ_TIMMING_READ         = 0x62;
    public const byte RF125_WRITE_TIMMING_READ        = 0x63;
    public const byte RF125_ISO11784_LOW_READ         = 0x64;
    public const byte RF125_TEMIC_LOWDATA_BIN_READ    = 0x65;
    public const byte RF125_REG_SAMPLING_TIME_WRITE   = 0x70;
    public const byte RF125_REG_SAMPLING_TIME_READ    = 0x71;
    public const byte RF125_REG_SAMPLING_TIME_AUTO    = 0x72;

    // ───── MAJOR_RELAY (0x22) ─────
    public const byte RELAY_UART1_BAUD_CHANGE        = 0x15;
    public const byte RELAY_UART2_BAUD_CHANGE        = 0x16;
    public const byte RELAY_UART3_BAUD_CHANGE        = 0x17;
    public const byte RELAY_UART4_BAUD_CHANGE        = 0x18;
    public const byte RELAY_UART5_BAUD_CHANGE        = 0x19;
    public const byte RELAY_UART_ALL_BAUD_CHANGE     = 0x1A;
    public const byte RELAY_INPUT_READ_ALL           = 0x20;
    public const byte RELAY_INPUT_READ_1             = 0x21;
    public const byte RELAY_INPUT_READ_2             = 0x22;
    public const byte RELAY_INPUT_READ_3             = 0x23;
    public const byte RELAY_INPUT_READ_4             = 0x24;
    public const byte RELAY_INPUT_READ_5             = 0x25;
    public const byte RELAY_RELAY_READ_ALL           = 0x30;
    public const byte RELAY_RELAY_READ_1             = 0x31;
    public const byte RELAY_RELAY_READ_8             = 0x38;
    public const byte RELAY_RELAY_WRITE_ALL          = 0x40;
    public const byte RELAY_RELAY_WRITE_1            = 0x41;
    public const byte RELAY_RELAY_WRITE_8            = 0x48;
    public const byte RELAY_AUTO_OFF_RELAY_WRITE      = 0x70;
    public const byte RELAY_AUTO_OFF_RELAY_TIME_WRITE = 0x71;
    public const byte RELAY_UART_1_TX_OUT             = 0x2A;
    public const byte RELAY_UART_2_TX_OUT             = 0x2B;
    public const byte RELAY_UART_3_TX_OUT             = 0x2C;
    public const byte RELAY_UART_4_TX_OUT             = 0x2D;

    // ───── MAJOR_RELAY_CONFIG (0x23) ─────
    public const byte RELAY_CFG_RS232OUT_PROTOCOL_READ      = 0x51;
    public const byte RELAY_CFG_RS232OUT_PROTOCOL_SAVE      = 0x52;
    public const byte RELAY_CFG_AUTO_OFF_RELAY_TIME_READ    = 0x53;
    public const byte RELAY_CFG_AUTO_OFF_RELAY_TIME_SAVE    = 0x54;
    public const byte RELAY_CFG_TCPIP_PACKET_WAIT_READ      = 0x55;
    public const byte RELAY_CFG_TCPIP_PACKET_WAIT_WRITE     = 0x56;
    public const byte RELAY_CFG_SERIAL_WIFI_MODE_READ       = 0x57;
    public const byte RELAY_CFG_SERIAL_WIFI_MODE_WRITE      = 0x58;
}
