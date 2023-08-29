using System.Security.Cryptography;
using System.Text;

namespace ArmoredCore6SaveTransferTool;

public class SL2Entry {
    private readonly byte[] _sl2Key = { 0xB1, 0x56, 0x87, 0x9F, 0x13, 0x48, 0x97, 0x98, 0x70, 0x05, 0xC4, 0x87, 0x00, 0xAE, 0xF8, 0x79 };
    private const int IvSize = 0x10;
    private const int PaddingSize = 0xC;
    private byte[] _data;
    private readonly byte[] _iv;
    private byte[] _decryptedData;

    public SL2Entry(byte[] data) {
        _data = data;
        _iv = new byte[IvSize];
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
            using (MemoryStream encStream = new(_data, IvSize, _data.Length - IvSize))
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
                encStream.Write(aes.IV, 0, IvSize);
                cryptoStream.CopyTo(encStream);
                _data = encStream.ToArray();
            }
        }
    }
    public byte[] GetMD5Hash() {
        return MD5.HashData(_data);
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
        int padding = 12 + 16;
        int end = _decryptedData.Length - padding;
        Array.Copy(checksum, 0, _decryptedData, end, checksum.Length);
    }
    byte[] getChecksum() {
        int padding = 12 + MD5.HashSizeInBytes;
        int end = _decryptedData.Length - padding;
        byte[] bs = _decryptedData[0x4..end];
        return MD5.HashData(bs);
    }
    public bool ChangeSteamID(long steamId) {
        return ChangeSteamID(BitConverter.GetBytes(steamId));
    }
    public bool ChangeSteamID(byte[] steamId) {
        List<int> hits = scanForString("Steam");
        if (hits.Count < 1) {
            return false;
        }
        foreach (int hit in hits) {
            PatchData(steamId, hit);
        }

        return true;
    }
    public List<int> scanForString(string str) {
        List<int> hits = new();

        for (int i = 0; i < _decryptedData.Length; i++) {
            int sectionEnd = i + 16;
            if (sectionEnd >= _decryptedData.Length) {
                break;
            }
            byte[] section = _decryptedData[i..sectionEnd];
            if (!char.IsAsciiLetter((char)section[0])) {
                continue;
            }
            string sectionStr = Encoding.UTF8.GetString(section);
            if (!sectionStr.Contains(str)) {
                continue;
            }
            int size = i + 16;
            int sizeEnd = size + 4;
            byte[] b = _decryptedData[size..sizeEnd];
            int val = BitConverter.ToInt32(b);
            if (val != 8) {
                continue;
            }
            i += 32;
            hits.Add(i);
        }

        return hits;
    }
}
