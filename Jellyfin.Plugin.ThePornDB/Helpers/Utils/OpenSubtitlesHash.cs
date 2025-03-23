using System;
using System.IO;

#if __EMBY__
using System.Text;
#endif

namespace ThePornDB.Helpers.Utils
{
    public static class OpenSubtitlesHash
    {
        public static string ComputeHash(string filepath)
        {
            var oshash = string.Empty;

            if (!File.Exists(filepath))
            {
                return oshash;
            }

            byte[] hash;
            using (var fileStream = File.OpenRead(filepath))
            {
                hash = ComputeMovieHash(fileStream);
            }

#if __EMBY__
            StringBuilder hexBuilder = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                hexBuilder.Append(hash[i].ToString("x2"));
            }

            oshash = hexBuilder.ToString();
#else
            oshash = Convert.ToHexString(hash).ToLowerInvariant();
#endif

            return oshash;
        }

        private static byte[] ComputeMovieHash(FileStream input)
        {
            using (input)
            {
                long lhash, streamsize;
                streamsize = input.Length;
                lhash = streamsize;

                long i = 0;
                byte[] buffer = new byte[sizeof(long)];
                while (i < 65536 / sizeof(long) && (input.Read(buffer, 0, sizeof(long)) > 0))
                {
                    i++;
                    lhash += BitConverter.ToInt64(buffer, 0);
                }

                input.Position = Math.Max(0, streamsize - 65536);
                i = 0;
                while (i < 65536 / sizeof(long) && (input.Read(buffer, 0, sizeof(long)) > 0))
                {
                    i++;
                    lhash += BitConverter.ToInt64(buffer, 0);
                }

                byte[] result = BitConverter.GetBytes(lhash);
                Array.Reverse(result);

                return result;
            }
        }
    }
}
