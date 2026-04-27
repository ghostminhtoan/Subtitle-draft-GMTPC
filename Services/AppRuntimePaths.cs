using System;
using System.IO;

namespace Subtitle_draft_GMTPC.Services
{
    internal static class AppRuntimePaths
    {
        private static string _baseDirectory = AppDomain.CurrentDomain.BaseDirectory;

        public static string BaseDirectory
        {
            get { return _baseDirectory; }
        }

        public static void SetBaseDirectory(string directory)
        {
            if (string.IsNullOrWhiteSpace(directory))
            {
                return;
            }

            _baseDirectory = directory;
        }

        public static string ResolvePath(params string[] segments)
        {
            var path = BaseDirectory;
            if (segments == null)
            {
                return path;
            }

            for (var i = 0; i < segments.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(segments[i]))
                {
                    continue;
                }

                path = Path.Combine(path, segments[i]);
            }

            return path;
        }
    }
}
