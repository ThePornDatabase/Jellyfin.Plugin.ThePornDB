using System;

namespace PhoenixAdult.Helpers.Utils
{
    internal static class LevenshteinDistance
    {
        public static int Calculate(string source1, string source2)
        {
            if (source1 == null || source2 == null)
            {
                return -1;
            }

            var source1Length = source1.Length;
            var source2Length = source2.Length;

            if (source1Length == 0 || source2Length == 0)
            {
                return 0;
            }

            var matrix = new int[source1Length + 1][];
            for (var i = 0; i <= source1Length; i++)
            {
                matrix[i] = new int[source2Length + 1];
                matrix[i][0] = i;
            }

            for (var j = 1; j <= source2Length; j++)
            {
                matrix[0][j] = j;
            }

            for (var i = 1; i <= source1Length; i++)
            {
                for (var j = 1; j <= source2Length; j++)
                {
                    var cost = (source2[j - 1] == source1[i - 1]) ? 0 : 1;

                    matrix[i][j] = Math.Min(
                        Math.Min(matrix[i - 1][j] + 1, matrix[i][j - 1] + 1),
                        matrix[i - 1][j - 1] + cost);
                }
            }

            return matrix[source1Length][source2Length];
        }

        public static int Calculate(string source1, string source2, StringComparison comparsion)
        {
            int result;

            switch (comparsion)
            {
                case StringComparison.CurrentCultureIgnoreCase:
                case StringComparison.InvariantCultureIgnoreCase:
                case StringComparison.OrdinalIgnoreCase:
                    result = Calculate(source1.ToLowerInvariant(), source2.ToLowerInvariant());
                    break;
                default:
                    result = Calculate(source1, source2);
                    break;
            }

            return result;
        }
    }
}
