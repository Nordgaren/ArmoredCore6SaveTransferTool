using System.Security.Cryptography;

namespace ArmoredCore6SaveTransferTool;

public static class Util {
    public static void PrintHelp() {
        Console.WriteLine("Please drag a valid SL2BND file from Armored Core 6 onto this tool.");
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
    public static void PrintHexBytes(IEnumerable<byte> bytes) {

        foreach (byte b in bytes) {
            Console.Write($"{b:X2} ");
        }
        Console.Write("\n");
    }
}
