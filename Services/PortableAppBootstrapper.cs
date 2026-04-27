using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Runtime.InteropServices;

namespace Subtitle_draft_GMTPC.Services
{
    internal static class PortableAppBootstrapper
    {
        private static readonly byte[] PayloadMarker = Encoding.ASCII.GetBytes("GMTPC_PAYLOAD_V1");
        private static bool _initialized;

        public static void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;

            var currentExePath = Process.GetCurrentProcess().MainModule != null
                ? Process.GetCurrentProcess().MainModule.FileName
                : Assembly.GetExecutingAssembly().Location;

            var payloadTargetDirectory = GetPayloadTargetDirectory(currentExePath);
            Directory.CreateDirectory(payloadTargetDirectory);

            if (TryExtractEmbeddedPayload(currentExePath, payloadTargetDirectory))
            {
                AppRuntimePaths.SetBaseDirectory(payloadTargetDirectory);
                try
                {
                    Directory.SetCurrentDirectory(payloadTargetDirectory);
                }
                catch
                {
                    // Ignore and keep running.
                }

                TrySetDllSearchPath(payloadTargetDirectory);
                AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            }
        }

        private static string GetPayloadTargetDirectory(string currentExePath)
        {
            using (var sha = SHA256.Create())
            using (var stream = File.OpenRead(currentExePath))
            {
                var hash = sha.ComputeHash(stream);
                var hashText = BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
                var directory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "GMTPC",
                    "Subtitle Draft GMTPC",
                    hashText);

                return directory;
            }
        }

        private static bool TryExtractEmbeddedPayload(string currentExePath, string targetDirectory)
        {
            using (var stream = File.OpenRead(currentExePath))
            {
                if (stream.Length <= PayloadMarker.Length + sizeof(long))
                {
                    return false;
                }

                stream.Seek(-PayloadMarker.Length, SeekOrigin.End);
                var marker = new byte[PayloadMarker.Length];
                if (stream.Read(marker, 0, marker.Length) != marker.Length || !marker.SequenceEqual(PayloadMarker))
                {
                    return false;
                }

                stream.Seek(-(PayloadMarker.Length + sizeof(long)), SeekOrigin.End);
                var lengthBuffer = new byte[sizeof(long)];
                if (stream.Read(lengthBuffer, 0, lengthBuffer.Length) != lengthBuffer.Length)
                {
                    return false;
                }

                var payloadLength = BitConverter.ToInt64(lengthBuffer, 0);
                if (payloadLength <= 0 || payloadLength > stream.Length)
                {
                    return false;
                }

                var payloadStart = stream.Length - PayloadMarker.Length - sizeof(long) - payloadLength;
                if (payloadStart < 0)
                {
                    return false;
                }

                stream.Seek(payloadStart, SeekOrigin.Begin);
                using (var payloadStream = new LimitedReadStream(stream, payloadLength))
                using (var archive = new ZipArchive(payloadStream, ZipArchiveMode.Read, false))
                {
                    foreach (var entry in archive.Entries)
                    {
                        var destinationPath = Path.Combine(targetDirectory, entry.FullName);
                        if (string.IsNullOrWhiteSpace(entry.Name))
                        {
                            Directory.CreateDirectory(destinationPath);
                            continue;
                        }

                        var directory = Path.GetDirectoryName(destinationPath);
                        if (!string.IsNullOrWhiteSpace(directory))
                        {
                            Directory.CreateDirectory(directory);
                        }

                        using (var entryStream = entry.Open())
                        using (var outputStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.Read))
                        {
                            entryStream.CopyTo(outputStream);
                        }
                    }
                }
            }

            return true;
        }

        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            var assemblyName = new AssemblyName(args.Name).Name + ".dll";
            var candidatePath = Path.Combine(AppRuntimePaths.BaseDirectory, assemblyName);
            if (File.Exists(candidatePath))
            {
                return Assembly.LoadFrom(candidatePath);
            }

            return null;
        }

        private static void TrySetDllSearchPath(string directory)
        {
            try
            {
                SetDllDirectory(directory);
            }
            catch
            {
                // Ignore if the native search path cannot be adjusted.
            }
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool SetDllDirectory(string lpPathName);

        private sealed class LimitedReadStream : Stream
        {
            private readonly Stream _inner;
            private readonly long _start;
            private readonly long _length;
            private long _position;

            public LimitedReadStream(Stream inner, long length)
            {
                _inner = inner;
                _start = inner.Position;
                _length = length;
                _position = 0;
            }

            public override bool CanRead => _inner.CanRead;
            public override bool CanSeek => _inner.CanSeek;
            public override bool CanWrite => false;
            public override long Length => _length;
            public override long Position
            {
                get { return _position; }
                set { Seek(value, SeekOrigin.Begin); }
            }

            public override void Flush()
            {
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                if (_position >= _length)
                {
                    return 0;
                }

                var remaining = _length - _position;
                if (count > remaining)
                {
                    count = (int)remaining;
                }

                var read = _inner.Read(buffer, offset, count);
                _position += read;
                return read;
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                long target;
                switch (origin)
                {
                    case SeekOrigin.Begin:
                        target = offset;
                        break;
                    case SeekOrigin.Current:
                        target = _position + offset;
                        break;
                    case SeekOrigin.End:
                        target = _length + offset;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(origin));
                }

                if (target < 0)
                {
                    target = 0;
                }
                else if (target > _length)
                {
                    target = _length;
                }

                _inner.Seek(_start + target, SeekOrigin.Begin);
                _position = target;
                return _position;
            }

            public override void SetLength(long value)
            {
                throw new NotSupportedException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new NotSupportedException();
            }
        }
    }
}
