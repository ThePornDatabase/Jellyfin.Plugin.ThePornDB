using System;

internal static class StringExtensions
{
    public static string GetAttributeValue(this string str, string attribute)
    {
        if (string.IsNullOrEmpty(str) || string.IsNullOrEmpty(attribute))
        {
            return null;
        }

        var attributeIndex = str.IndexOf(attribute, StringComparison.OrdinalIgnoreCase);

        var maxIndex = str.Length - attribute.Length - 3;
        while (attributeIndex > -1 && attributeIndex < maxIndex)
        {
            var attributeEnd = attributeIndex + attribute.Length;
            if (attributeIndex > 0 && str[attributeIndex - 1] == '[' && (str[attributeEnd] == '=' || str[attributeEnd] == '-'))
            {
                var closingIndex = str[attributeEnd..].IndexOf(']');
                if (closingIndex > 1)
                {
                    return str[(attributeEnd + 1) .. (attributeEnd + closingIndex)].Trim().ToString();
                }
            }

            str = str[attributeEnd..];
            attributeIndex = str.IndexOf(attribute, StringComparison.OrdinalIgnoreCase);
        }

        return null;
    }
}
