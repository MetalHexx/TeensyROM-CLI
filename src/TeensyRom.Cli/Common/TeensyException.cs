namespace TeensyRom.Cli.Common
{
    public class TeensyException: Exception
    {
        public TeensyException(string message) : base(message) { }
        public TeensyException(string message, Exception ex) : base(message, ex) { }
    }

    public class TeensyBusyException(string message) : TeensyException(message) { }

    public class TeensyStateException(string message) : Exception(message) { }
}
