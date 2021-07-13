/* Copyright (C) 2017 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */

using System.Collections.Generic;
using System.Text;
using ByteReader = RedstoneSmb.Utilities.ByteUtils.ByteReader;
using ByteWriter = RedstoneSmb.Utilities.ByteUtils.ByteWriter;

namespace RedstoneSmb.Authentication.GSSAPI.SPNEGO
{
    public enum DerEncodingTag : byte
    {
        ByteArray = 0x04, // Octet String
        ObjectIdentifier = 0x06,
        Enum = 0x0A,
        GeneralString = 0x1B,
        Sequence = 0x30
    }

    public class DerEncodingHelper
    {
        public static int ReadLength(byte[] buffer, ref int offset)
        {
            int length = ByteReader.ReadByte(buffer, ref offset);
            if (length >= 0x80)
            {
                var lengthFieldSize = length & 0x7F;
                var lengthField = ByteReader.ReadBytes(buffer, ref offset, lengthFieldSize);
                length = 0;
                foreach (var value in lengthField)
                {
                    length *= 256;
                    length += value;
                }
            }

            return length;
        }

        public static void WriteLength(byte[] buffer, ref int offset, int length)
        {
            if (length >= 0x80)
            {
                var values = new List<byte>();
                do
                {
                    var value = (byte) (length % 256);
                    values.Add(value);
                    length /= 256;
                } while (length > 0);

                values.Reverse();
                var lengthField = values.ToArray();
                ByteWriter.WriteByte(buffer, ref offset, (byte) (0x80 | lengthField.Length));
                ByteWriter.WriteBytes(buffer, ref offset, lengthField);
            }
            else
            {
                ByteWriter.WriteByte(buffer, ref offset, (byte) length);
            }
        }

        public static int GetLengthFieldSize(int length)
        {
            if (length >= 0x80)
            {
                var result = 1;
                do
                {
                    length /= 256;
                    result++;
                } while (length > 0);

                return result;
            }

            return 1;
        }

        public static byte[] EncodeGeneralString(string value)
        {
            // We do not support character-set designation escape sequences
            return Encoding.ASCII.GetBytes(value);
        }

        public static string DecodeGeneralString(byte[] bytes)
        {
            // We do not support character-set designation escape sequences
            return Encoding.ASCII.GetString(bytes);
        }
    }
}