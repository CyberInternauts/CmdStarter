using System.Text;

namespace CyberInternauts.CmdStarter.Analyzers;

internal static class NameConversion
{
    internal static string PascalToKebabCase(string? source)
    {
        if (source is null || source.Length == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        for (var i = 0; i < source.Length; i++)
        {
            if (char.IsLower(source[i]))
            {
                builder.Append(source[i]);
            }
            else if (i == 0)
            {
                builder.Append(char.ToLowerInvariant(source[i]));
            }
            else if (char.IsDigit(source[i]) && !char.IsDigit(source[i - 1]))
            {
                builder.Append('-');
                builder.Append(source[i]);
            }
            else if (char.IsDigit(source[i]))
            {
                builder.Append(source[i]);
            }
            else if (char.IsLower(source[i - 1]))
            {
                builder.Append('-');
                builder.Append(char.ToLowerInvariant(source[i]));
            }
            else if (i + 1 == source.Length || char.IsUpper(source[i + 1]))
            {
                builder.Append(char.ToLowerInvariant(source[i]));
            }
            else
            {
                builder.Append('-');
                builder.Append(char.ToLowerInvariant(source[i]));
            }
        }

        return builder.ToString();
    }
}
