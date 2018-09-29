/*
   Derived from zlib header files (zlib license)
   Copyright (C) 1995-2017 Jean-loup Gailly and Mark Adler

   C# Wrapper based on zlibnet v1.3.3 (https://zlibnet.codeplex.com/)
   Copyright (C) @hardon (https://www.codeplex.com/site/users/view/hardon)
   Copyright (C) 2017-2018 Hajin Jang

   zlib license

   This software is provided 'as-is', without any express or implied
   warranty.  In no event will the authors be held liable for any damages
   arising from the use of this software.

   Permission is granted to anyone to use this software for any purpose,
   including commercial applications, and to alter it and redistribute it
   freely, subject to the following restrictions:

   1. The origin of this software must not be misrepresented; you must not
      claim that you wrote the original software. If you use this software
      in a product, an acknowledgment in the product documentation would be
      appreciated but is not required.
   2. Altered source versions must be plainly marked as such, and must not be
      misrepresented as being the original software.
   3. This notice may not be removed or altered from any source distribution.
*/

using System.IO;

namespace Joveler.ZLib
{
    #region Crc32Stream
    public class Crc32Stream : Stream
    {
        #region Fields
        private uint _crc32 = 0;
        private readonly Stream _baseStream;
        #endregion

        #region Properties
        public uint Crc32 => _crc32;
        public uint Checksum => _crc32;
        public Stream BaseStream => _baseStream;
        #endregion

        #region Constructor
        public Crc32Stream(Stream stream)
        {
            NativeMethods.CheckZLibLoaded();
            _baseStream = stream;
        }
        #endregion

        #region Stream Methods
        public override int Read(byte[] buffer, int offset, int count)
        {
            int readLen = _baseStream.Read(buffer, offset, count);
            using (PinnedArray<byte> pinRead = new PinnedArray<byte>(buffer))
            {
                _crc32 = NativeMethods.Crc32(_crc32, pinRead[offset], (uint)readLen);
            }
            return readLen;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _baseStream.Write(buffer, offset, count);
            using (PinnedArray<byte> pinRead = new PinnedArray<byte>(buffer))
            {
                _crc32 = NativeMethods.Crc32(_crc32, pinRead[offset], (uint)count);
            }
        }

        public override void Flush()
        {
            _baseStream.Flush();
        }

        public override bool CanRead => _baseStream.CanRead;
        public override bool CanWrite => _baseStream.CanWrite;
        public override bool CanSeek => _baseStream.CanSeek;

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _baseStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            _baseStream.SetLength(value);
        }

        public override long Length => _baseStream.Length;

        public override long Position
        {
            get => _baseStream.Position;
            set => _baseStream.Position = value;
        }
        #endregion
    }
    #endregion

    #region Adler32Stream
    public class Adler32Stream : Stream
    {
        #region Fields
        private uint _adler32 = 1;
        private readonly Stream _baseStream;
        #endregion

        #region Properties
        public uint Adler32 => _adler32;
        public uint Checksum => _adler32;
        public Stream BaseStream => _baseStream;
        #endregion

        #region Constructor
        public Adler32Stream(Stream stream)
        {
            NativeMethods.CheckZLibLoaded();
            _baseStream = stream;
        }
        #endregion

        #region Stream Methods
        public override int Read(byte[] buffer, int offset, int count)
        {
            int readLen = _baseStream.Read(buffer, offset, count);
            using (PinnedArray<byte> pinRead = new PinnedArray<byte>(buffer))
            {
                _adler32 = NativeMethods.Adler32(_adler32, pinRead[offset], (uint)readLen);
            }
            return readLen;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _baseStream.Write(buffer, offset, count);
            using (PinnedArray<byte> pinRead = new PinnedArray<byte>(buffer))
            {
                _adler32 = NativeMethods.Adler32(_adler32, pinRead[offset], (uint)count);
            }
        }

        public override void Flush()
        {
            _baseStream.Flush();
        }

        public override bool CanRead => _baseStream.CanRead;
        public override bool CanWrite => _baseStream.CanWrite;
        public override bool CanSeek => _baseStream.CanSeek;

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _baseStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            _baseStream.SetLength(value);
        }

        public override long Length => _baseStream.Length;

        public override long Position
        {
            get => _baseStream.Position;
            set => _baseStream.Position = value;
        }
        #endregion
    }
    #endregion

    #region Crc32Checksum
    public class Crc32Checksum
    {
        #region Fields and Properties
        private const uint InitChecksum = 0;

        private uint _checksum;
        public uint Checksum => _checksum;
        #endregion

        #region Constructor
        public Crc32Checksum()
        {
            NativeMethods.CheckZLibLoaded();
            Reset();
        }
        #endregion

        #region Append, Reset
        public uint Append(byte[] buffer)
        {
            _checksum = Crc32(_checksum, buffer);
            return _checksum;
        }

        public uint Append(byte[] buffer, int offset, int count)
        {
            _checksum = Crc32(_checksum, buffer, offset, count);
            return _checksum;
        }

        public uint Append(Stream stream)
        {
            byte[] buffer = new byte[NativeMethods.BufferSize];
            while (stream.Position < stream.Length)
            {
                int readByte = stream.Read(buffer, 0, NativeMethods.BufferSize);
                _checksum = Crc32(_checksum, buffer, 0, readByte);
            }
            return _checksum;
        }

        public void Reset()
        {
            _checksum = InitChecksum;
        }
        #endregion

        #region zlib crc32 Wrapper
        public static uint Crc32(byte[] buffer)
        {
            NativeMethods.CheckZLibLoaded();

            using (PinnedArray<byte> pinRead = new PinnedArray<byte>(buffer))
            {
                return NativeMethods.Crc32(InitChecksum, pinRead, (uint)buffer.Length);
            }
        }

        public static uint Crc32(byte[] buffer, int offset, int count)
        {
            NativeMethods.CheckZLibLoaded();

            DeflateStream.ValidateReadWriteArgs(buffer, offset, count);

            using (PinnedArray<byte> pinRead = new PinnedArray<byte>(buffer))
            {
                return NativeMethods.Crc32(InitChecksum, pinRead[offset], (uint)count);
            }
        }

        public static uint Crc32(Stream stream)
        {
            uint checksum = InitChecksum;

            byte[] buffer = new byte[NativeMethods.BufferSize];
            int readByte;
            do
            {
                readByte = stream.Read(buffer, 0, buffer.Length);
                checksum = Crc32(checksum, buffer, 0, readByte);
            }
            while (0 < readByte);

            return checksum;
        }

        public static uint Crc32(uint checksum, byte[] buffer)
        {
            NativeMethods.CheckZLibLoaded();

            using (PinnedArray<byte> bufferPtr = new PinnedArray<byte>(buffer))
            {
                return NativeMethods.Crc32(checksum, bufferPtr, (uint)buffer.Length);
            }
        }

        public static uint Crc32(uint checksum, byte[] buffer, int offset, int count)
        {
            NativeMethods.CheckZLibLoaded();

            DeflateStream.ValidateReadWriteArgs(buffer, offset, count);

            using (PinnedArray<byte> pinRead = new PinnedArray<byte>(buffer))
            {
                return NativeMethods.Crc32(checksum, pinRead[offset], (uint)count);
            }
        }

        public static uint Crc32(uint checksum, Stream stream)
        {
            byte[] buffer = new byte[NativeMethods.BufferSize];
            int readByte;
            do
            {
                readByte = stream.Read(buffer, 0, buffer.Length);
                checksum = Crc32(checksum, buffer, 0, readByte);
            }
            while (0 < readByte);

            return checksum;
        }
        #endregion
    }
    #endregion

    #region Adler32Checksum
    public class Adler32Checksum
    {
        #region Fields and Properties
        private const uint InitChecksum = 1;

        private uint _checksum;
        public uint Checksum => _checksum;
        #endregion

        #region Constructor
        public Adler32Checksum()
        {
            NativeMethods.CheckZLibLoaded();

            Reset();
        }
        #endregion

        #region Append, Reset
        public uint Append(byte[] buffer)
        {
            _checksum = Adler32(_checksum, buffer);
            return _checksum;
        }

        public uint Append(byte[] buffer, int offset, int count)
        {
            _checksum = Adler32(_checksum, buffer, offset, count);
            return _checksum;
        }

        public uint Append(Stream stream)
        {
            byte[] buffer = new byte[NativeMethods.BufferSize];
            while (stream.Position < stream.Length)
            {
                int readByte = stream.Read(buffer, 0, NativeMethods.BufferSize);
                _checksum = Adler32(_checksum, buffer, 0, readByte);
            }
            return _checksum;
        }

        public void Reset()
        {
            _checksum = InitChecksum;
        }
        #endregion

        #region zlib adler32 Wrapper
        public static uint Adler32(byte[] buffer)
        {
            NativeMethods.CheckZLibLoaded();

            using (PinnedArray<byte> bufferPtr = new PinnedArray<byte>(buffer))
            {
                return NativeMethods.Adler32(InitChecksum, bufferPtr, (uint)buffer.Length);
            }
        }

        public static uint Adler32(byte[] buffer, int offset, int count)
        {
            NativeMethods.CheckZLibLoaded();

            DeflateStream.ValidateReadWriteArgs(buffer, offset, count);

            using (PinnedArray<byte> bufferPtr = new PinnedArray<byte>(buffer))
            {
                return NativeMethods.Adler32(InitChecksum, bufferPtr[offset], (uint)count);
            }
        }

        public static uint Adler32(Stream stream)
        {
            uint checksum = InitChecksum;

            byte[] buffer = new byte[NativeMethods.BufferSize];
            int readByte;
            do
            {
                readByte = stream.Read(buffer, 0, buffer.Length);
                checksum = Adler32(checksum, buffer, 0, readByte);
            }
            while (0 < readByte);

            return checksum;
        }

        public static uint Adler32(uint checksum, byte[] buffer)
        {
            NativeMethods.CheckZLibLoaded();

            using (PinnedArray<byte> bufferPtr = new PinnedArray<byte>(buffer))
            {
                return NativeMethods.Adler32(checksum, bufferPtr, (uint)buffer.Length);
            }
        }

        public static uint Adler32(uint checksum, byte[] buffer, int offset, int count)
        {
            NativeMethods.CheckZLibLoaded();

            DeflateStream.ValidateReadWriteArgs(buffer, offset, count);

            using (PinnedArray<byte> bufferPtr = new PinnedArray<byte>(buffer))
            {
                return NativeMethods.Adler32(checksum, bufferPtr[offset], (uint)count);
            }
        }

        public static uint Adler32(uint checksum, Stream stream)
        {
            byte[] buffer = new byte[NativeMethods.BufferSize];
            int readByte;
            do
            {
                readByte = stream.Read(buffer, 0, buffer.Length);
                checksum = Adler32(checksum, buffer, 0, readByte);
            }
            while (0 < readByte);

            return checksum;
        }
        #endregion
    }
    #endregion
}