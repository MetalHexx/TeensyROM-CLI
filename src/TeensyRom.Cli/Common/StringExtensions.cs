namespace TeensyRom.Cli.Common
{
    public static class StringExtensions
    {
        public static List<string> SplitAtCarriageReturn(this string message)
        {
            string[] delimiters = ["\r\n", "\n", "\r"];
            string[] parts = message.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
            return new List<string>(parts);
        }

        public static string DropLastNewLine(this string message) => message.TrimEnd('\r', '\n', ' ');
    }
}
