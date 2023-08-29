// See https://aka.ms/new-console-template for more information

using ArmoredCore6SaveTransferTool;
using SoulsFormats;

if (args.Length < 1 || !BND4.Is(args[0])) {
    Util.PrintHelp();
}

SL2BND.PatchSL2(args[0]);
