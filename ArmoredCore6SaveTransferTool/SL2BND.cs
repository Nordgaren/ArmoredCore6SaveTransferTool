using SoulsFormats;
using System.Security.Cryptography;

namespace ArmoredCore6SaveTransferTool;

public class SL2BND {
    private string _location;
    private BND4 _bnd;
    public SL2BND(string sl2Location) {
        _location = sl2Location;
        _bnd = BND4.Read(sl2Location);
    }
    public void ChangeSteamId(long steamId) {
        foreach (BinderFile? file in _bnd.Files) {
            SL2Entry sl2 = new(file.Bytes);
            if (!sl2.ChangeSteamID(steamId)) {
                continue;
            }
            sl2.PatchChecksum();
            sl2.EncryptSL2File();
            file.Bytes = sl2.GetData();
        }   
    }

    public void Write() {
        string backup = $"{Path.GetDirectoryName(_location)}\\{Path.GetFileName(_location)}.SL2Backup";
        if (!File.Exists(backup)) {
            File.Copy(_location, backup);
        }
        _bnd.Write(_location);
    }
    
    public static void PatchSL2(string path) {
        SL2BND sl2Bnd = new(path);

        string? parentDirName = Path.GetFileName(Path.GetDirectoryName(path));
        if (long.TryParse(parentDirName, out long id)) {
            SL2ChangeSteamID(sl2Bnd, id);
            return;
        }

        Console.WriteLine("Please enter the new steam ID:");
        string? userInput = Console.ReadLine();
        if (long.TryParse(userInput, out long steamId)) {
            SL2ChangeSteamID(sl2Bnd, steamId);
            return;
        }
        
        Console.WriteLine($"Invalid steam ID. Could not parse. Received \"{userInput}\"");

    }

    static void SL2ChangeSteamID(SL2BND sl2Bnd, long id) {
        Console.WriteLine($"Changing steam ID to {id}");
        sl2Bnd.ChangeSteamId(id);
        sl2Bnd.Write();
    }

}
