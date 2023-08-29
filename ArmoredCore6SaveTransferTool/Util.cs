using System.Security.Cryptography;

namespace ArmoredCore6SaveTransferTool;

public static class Util {
    public static void PrintHelp() {
        Console.WriteLine("Please drag a valid SL2BND file from Armored Core 6 onto this tool."
            + "If the SL2 file is in the folder with the new Steam ID, it will patch automatically"
            + "Otherwise you will be prompted for the new Steam Id.");
    }
    public static void PrintHexBytes(IEnumerable<byte> bytes) {
        foreach (byte b in bytes) {
            Console.Write($"{b:X2} ");
        }
        Console.Write("\n");
    }
}
