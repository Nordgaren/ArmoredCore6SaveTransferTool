using System.Security.Cryptography;
using System.Text;

namespace ArmoredCore6SaveTransferTool;

public class SL2Entry {
    private readonly byte[] _sl2Key = { 0xB1, 0x56, 0x87, 0x9F, 0x13, 0x48, 0x97, 0x98, 0x70, 0x05, 0xC4, 0x87, 0x00, 0xAE, 0xF8, 0x79 };
    private const int _ivSize = 0x10;
    private const int _paddingSize = 0xC;
    private const int _sectionStringSize = 0x10;
    private const int _sectionHeaderSize = 0x20;
    private const int _startOfChecksumData = sizeof(int);
    private const int _endOfChecksumData = _paddingSize + MD5.HashSizeInBytes;
    private byte[] _data;
    private readonly byte[] _iv;
    private byte[] _decryptedData;

    public SL2Entry(byte[] data) {
        _data = data;
        _iv = new byte[_ivSize];
        Buffer.BlockCopy(_data, 0, _iv, 0, 16);
        DecryptSL2File();
    }
    /// <summary>
    /// Decrypts a file from a DS2/DS3 SL2BND. Do not remove the hash and IV before calling.
    /// </summary>
    public void DecryptSL2File() {
        using (Aes aes = Aes.Create()) {
            aes.Mode = CipherMode.CBC;
            aes.BlockSize = 128;
            // PKCS7-style padding is used, but they don't include the minimum padding
            // so it can't be stripped safely
            aes.Padding = PaddingMode.None;
            aes.Key = _sl2Key;
            aes.IV = _iv;

            ICryptoTransform decryptor = aes.CreateDecryptor();
            using (MemoryStream encStream = new(_data, _ivSize, _data.Length - _ivSize))
            using (CryptoStream cryptoStream = new(encStream, decryptor, CryptoStreamMode.Read))
            using (MemoryStream decStream = new()) {
                cryptoStream.CopyTo(decStream);
                _decryptedData = decStream.ToArray();
            }
        }
    }
    /// <summary>
    /// Encrypts a file for a DS2/DS3 SL2BND. Result includes the hash and IV.
    /// </summary>
    public void EncryptSL2File() {
        using (Aes aes = Aes.Create()) {
            aes.Mode = CipherMode.CBC;
            aes.BlockSize = 128;
            aes.Padding = PaddingMode.None;
            aes.Key = _sl2Key;
            aes.IV = _iv;

            ICryptoTransform encryptor = aes.CreateEncryptor();
            using (MemoryStream decStream = new(_decryptedData))
            using (CryptoStream cryptoStream = new(decStream, encryptor, CryptoStreamMode.Read))
            using (MemoryStream encStream = new()) {
                encStream.Write(aes.IV, 0, _ivSize);
                cryptoStream.CopyTo(encStream);
                _data = encStream.ToArray();
            }
        }
    }
    public void PatchData(byte[] newData, int offset) {
        Array.Copy(newData, 0, _decryptedData, offset, newData.Length);
    }
    public byte[] GetData() {
        return _data;
    }
    public byte[] GetDecryptedData() {
        return _decryptedData;
    }
    public void PatchChecksum() {
        byte[] checksum = getChecksum();
        int end = _decryptedData.Length - _endOfChecksumData;
        Array.Copy(checksum, 0, _decryptedData, end, checksum.Length);
    }
    byte[] getChecksum() {
        int end = _decryptedData.Length - _endOfChecksumData;
        byte[] bs = _decryptedData[_startOfChecksumData..end];
        return MD5.HashData(bs);
    }
    public bool ChangeSteamID(long steamId) {
        return ChangeSteamID(BitConverter.GetBytes(steamId));
    }
    public bool ChangeSteamID(byte[] steamId) {
        List<SL2Section> sections = scanForSection("Steam");
        if (sections.Count < 1) {
            return false;
        }
        foreach (SL2Section section in sections) {
            PatchData(steamId, section.Offset + _sectionHeaderSize);
        }

        return true;
    }
    private List<SL2Section> scanForSection(string str) {
        List<SL2Section> sections = new();

        for (int i = 0; i < _decryptedData.Length; i++) {
            int sectionEnd = i + _sectionStringSize;
            if (sectionEnd >= _decryptedData.Length) {
                break;
            }
            byte[] section = _decryptedData[i..sectionEnd];
            if (!char.IsAsciiLetter((char)section[0])) {
                    continue;
            }
            string sectionStr = getSectionString(section);
            if (!sectionStr.Contains(str)) {
                continue;
            }
            int size = i + 16;
            int sizeEnd = size + 4;
            byte[] b = _decryptedData[size..sizeEnd];
            int dataSize = BitConverter.ToInt32(b);
            if (dataSize != 8) {
                continue;
            }
            sections.Add(new SL2Section(sectionStr, i, dataSize));
            i += _sectionHeaderSize + dataSize;
        }

        return sections;
    }
    private string getSectionString(byte[] section) {
        int index = Array.FindIndex(section, b => b == 0);
        if (index == -1) {
            index = _sectionStringSize;
        }
        return Encoding.UTF8.GetString(section[..index]);

    }
}
