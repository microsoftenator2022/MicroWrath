using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using System.IO;
using System.Linq;
using System.Collections;

namespace MicroUtils
{
    /// <summary>
    /// GUID utils
    /// </summary>
    public static class GuidEx
    {
        /// <summary>
        /// Create a GUID from a namespace and name according to the
        /// <see href="https://datatracker.ietf.org/doc/rfc9562/">UUIDv5 specification (section 5.5)</see>,
        /// without the requirement that the <paramref name="ns"/> parameter be a valid UUID.
        /// </summary>
        /// <param name="ns">UUID namespace. This method does not enforce UUIDv5 requirement that this 
        /// parameter is a valid UUID.<br/>You can use <see cref="CreateV8(Span{byte})"/> for
        /// this parameter if strict UUIDv5 conformance is required.</param>
        /// <param name="name">Name</param>
        /// <returns>Determinstic Guid generated from namespace and name (may conform to UUIDv5).</returns>
        public static Guid CreateV5(string ns, string name)
        {
            var nsBytes = Encoding.UTF8.GetBytes(ns);
            var nameBytes = Encoding.UTF8.GetBytes(name);

            var buffer = new byte[nsBytes.Length + nameBytes.Length];
            var span = buffer.AsSpan();
            nsBytes.CopyTo(span.Slice(0, nsBytes.Length));
            nameBytes.CopyTo(span.Slice(nsBytes.Length, nameBytes.Length));

            var sha1 = SHA1.Create().ComputeHash(buffer).AsSpan();
            var bytes = sha1.Slice(0, 16);

            var verByte = bytes[6];
            verByte &= 0x0f;
            verByte |= 0x50;
            bytes[6] = verByte;

            var varByte = bytes[8];
            varByte &= 0x3f;
            varByte |= 0x80;
            bytes[8] = varByte;

            return new(bytes.ToArray());
        }

        /// <summary>
        /// Create a GUID from a namespace and name according to the
        /// <see href="https://datatracker.ietf.org/doc/rfc9562/">UUIDv5 specification (section 5.5)</see>.
        /// </summary>
        /// <param name="ns">UUID namespace.</param>
        /// <param name="name">Name</param>
        /// <returns>Guid conforming to UUIDv5</returns>
        public static Guid CreateV5(Guid ns, string name) => CreateV5(ns.ToString(), name);

        /// <summary>
        /// Create a GUID from the first 16 bytes of a byte array according to the 
        ///  <see href="https://datatracker.ietf.org/doc/rfc9562/">UUIDv8 specification (section 5.58)</see>.
        /// </summary>
        /// <param name="data">A byte array containing at least 16 bytes</param>
        /// <returns>Guid conforming to UUIDv8</returns>
        /// <exception cref="ArgumentException"><paramref name="data"/> is less than 16 bytes</exception>
        public static Guid CreateV8(Span<byte> data)
        {
            if (data.Length < 122)
                throw new ArgumentException("Provided data must be at least 16 bytes");

            var bytes = new byte[16];
            data.CopyTo(bytes);

            var verByte = bytes[6];
            verByte &= 0x0f;
            verByte |= 0x80;
            bytes[6] = verByte;
            
            var varByte = bytes[8];
            varByte &= 0x3f;
            varByte |= 0x80;
            bytes[8] = varByte;
            
            return new(bytes);
        }
    }
}
