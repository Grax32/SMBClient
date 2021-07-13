/* Copyright (C) 2014-2017 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */

using System.Text;
using RedstoneSmb.Authentication.NTLM.Structures;
using RedstoneSmb.Authentication.NTLM.Structures.Enums;
using ByteReader = RedstoneSmb.Utilities.ByteUtils.ByteReader;
using ByteUtils = RedstoneSmb.Utilities.ByteUtils.ByteUtils;
using LittleEndianConverter = RedstoneSmb.Utilities.Conversion.LittleEndianConverter;
using LittleEndianWriter = RedstoneSmb.Utilities.ByteUtils.LittleEndianWriter;

namespace RedstoneSmb.Authentication.NTLM.Helpers
{
    public class AuthenticationMessageUtils
    {
        public static string ReadAnsiStringBufferPointer(byte[] buffer, int offset)
        {
            var bytes = ReadBufferPointer(buffer, offset);
            return Encoding.Default.GetString(bytes);
        }

        public static string ReadUnicodeStringBufferPointer(byte[] buffer, int offset)
        {
            var bytes = ReadBufferPointer(buffer, offset);
            return Encoding.Unicode.GetString(bytes);
        }

        public static byte[] ReadBufferPointer(byte[] buffer, int offset)
        {
            var length = LittleEndianConverter.ToUInt16(buffer, offset);
            var maxLength = LittleEndianConverter.ToUInt16(buffer, offset + 2);
            var bufferOffset = LittleEndianConverter.ToUInt32(buffer, offset + 4);

            if (length == 0)
                return new byte[0];
            return ByteReader.ReadBytes(buffer, (int) bufferOffset, length);
        }

        public static void WriteBufferPointer(byte[] buffer, int offset, ushort bufferLength, uint bufferOffset)
        {
            LittleEndianWriter.WriteUInt16(buffer, offset, bufferLength);
            LittleEndianWriter.WriteUInt16(buffer, offset + 2, bufferLength);
            LittleEndianWriter.WriteUInt32(buffer, offset + 4, bufferOffset);
        }

        public static bool IsSignatureValid(byte[] messageBytes)
        {
            if (messageBytes.Length < 8) return false;
            var signature = ByteReader.ReadAnsiString(messageBytes, 0, 8);
            return signature == AuthenticateMessage.ValidSignature;
        }

        /// <summary>
        ///     If NTLM v1 Extended Session Security is used, LMResponse starts with 8-byte challenge, followed by 16 bytes of
        ///     padding (set to zero).
        /// </summary>
        /// <remarks>
        ///     LMResponse is 24 bytes for NTLM v1, NTLM v1 Extended Session Security and NTLM v2.
        /// </remarks>
        public static bool IsNtlMv1ExtendedSessionSecurity(byte[] lmResponse)
        {
            if (lmResponse.Length == 24)
            {
                if (ByteUtils.AreByteArraysEqual(ByteReader.ReadBytes(lmResponse, 0, 8), new byte[8]))
                    // Challenge not present, cannot be NTLM v1 Extended Session Security
                    return false;
                return ByteUtils.AreByteArraysEqual(ByteReader.ReadBytes(lmResponse, 8, 16), new byte[16]);
            }

            return false;
        }

        /// <remarks>
        ///     NTLM v1 / NTLM v1 Extended Session Security NTResponse is 24 bytes.
        /// </remarks>
        public static bool IsNtlMv2NtResponse(byte[] ntResponse)
        {
            return ntResponse.Length >= 16 + NtlMv2ClientChallenge.MinimumLength &&
                   ntResponse[16] == NtlMv2ClientChallenge.StructureVersion &&
                   ntResponse[17] == NtlMv2ClientChallenge.StructureVersion;
        }

        public static MessageTypeName GetMessageType(byte[] messageBytes)
        {
            return (MessageTypeName) LittleEndianConverter.ToUInt32(messageBytes, 8);
        }
    }
}