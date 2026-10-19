namespace TeensyRom.Cli.Storage.Entities
{
    public static class StorageTypeExtensions
    {
        public static TeensyFileType GetFileType(this string extension) => extension.ToLower() switch
        {
            ".sid" => TeensyFileType.Sid,
            ".crt" => TeensyFileType.Crt,
            ".prg" => TeensyFileType.Prg,
            ".p00" => TeensyFileType.P00,
            ".hex" => TeensyFileType.Hex,
            ".kla" => TeensyFileType.Kla,
            ".koa" => TeensyFileType.Koa,
            ".art" => TeensyFileType.Art,
            ".aas" => TeensyFileType.Aas,
            ".hpi" => TeensyFileType.Hpi,
            ".d64" => TeensyFileType.D64,
            _ => TeensyFileType.Unknown
        };
    }
}
