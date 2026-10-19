using System.Text;

namespace TeensyRom.Cli.Common
{
    public static class StringBuilderExtensions
    {
        public static StringBuilder AppendWithLimit(this StringBuilder builder, string value, int maxSize = 10000)
        {
            ArgumentNullException.ThrowIfNull(builder);

            if (maxSize <= 0) throw new ArgumentOutOfRangeException(nameof(maxSize), "Max size must be greater than 0.");

            int availableSpace = maxSize - builder.Length;

            if (availableSpace <= 0) return builder;

            if (value.Length <= availableSpace) 
            {
                builder.Append(value);
                return builder;
            }
            string truncatedValue = value[..availableSpace];
            builder.Append(truncatedValue);
            return builder;
        }
    }
}
