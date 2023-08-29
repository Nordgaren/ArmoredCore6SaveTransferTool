using SoulsFormats;
using System.Security.Cryptography;

namespace ArmoredCore6SaveTransferTool;

public class SL2 {
    private const int IdLocationOne = 0x271B3;
    private const int IdLocationTwo = 0x60078;
    private const int IdLocationThree = 0x871C3;
    private const int IdLocationFour = 0x68;
    private string _location;
    private BND4 _bnd;
    public SL2(string sl2Location) {
        _location = sl2Location;
        _bnd = BND4.Read(sl2Location);
    }
    public void ChangeSteamId(long steamId) {
        SL2Entry fileZero = new (_bnd.Files[0].Bytes);
        SL2Entry fileNine = new (_bnd.Files[9].Bytes);
        
        fileZero.DecryptSL2File();
        fileNine.DecryptSL2File();
        
        byte[] steamIdBytes = BitConverter.GetBytes(steamId);
        fileZero.PatchData(steamIdBytes , IdLocationOne);
        fileZero.PatchData(steamIdBytes , IdLocationTwo);
        fileZero.PatchData(steamIdBytes , IdLocationThree);
        
        fileNine.PatchData(steamIdBytes , IdLocationOne);
        fileNine.PatchData(steamIdBytes , IdLocationFour);
        
        Console.WriteLine("File Zero");
        fileZero.PatchChecksum();
        Console.WriteLine("File Nine");
        fileNine.PatchChecksum();
        
        fileZero.EncryptSL2File();
        fileNine.EncryptSL2File();
        _bnd.Files[0].Bytes = fileZero.GetData();
        _bnd.Files[9].Bytes = fileNine.GetData();
    }

    public void WriteSL2ToDisk() {
        string backup = $"{Path.GetDirectoryName(_location)}\\{Path.GetFileName(_location)}.SL2Backup";
        if (!File.Exists(backup)) {
            File.Copy(_location, backup);
        }
        _bnd.Write(_location);
    }

}
