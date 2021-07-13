/* Copyright (C) 2014 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */

using System.Collections.Generic;
using ByteReader = RedstoneSmb.Utilities.ByteUtils.ByteReader;
using ByteWriter = RedstoneSmb.Utilities.ByteUtils.ByteWriter;
using LittleEndianConverter = RedstoneSmb.Utilities.Conversion.LittleEndianConverter;
using LittleEndianWriter = RedstoneSmb.Utilities.ByteUtils.LittleEndianWriter;

namespace RedstoneSmb.RPC.Structures
{
    /// <summary>
    ///     p_cont_list_t
    ///     Presentation Context List
    /// </summary>
    public class ContextList : List<ContextElement>
    {
        //byte NumberOfContextElements;
        public byte Reserved1;
        public ushort Reserved2;

        public ContextList()
        {
        }

        public ContextList(byte[] buffer, int offset)
        {
            var numberOfContextElements = ByteReader.ReadByte(buffer, offset + 0);
            Reserved1 = ByteReader.ReadByte(buffer, offset + 1);
            Reserved2 = LittleEndianConverter.ToUInt16(buffer, offset + 2);
            offset += 4;
            for (var index = 0; index < numberOfContextElements; index++)
            {
                var element = new ContextElement(buffer, offset);
                Add(element);
                offset += element.Length;
            }
        }

        public int Length
        {
            get
            {
                var length = 4;
                for (var index = 0; index < Count; index++) length += this[index].Length;
                return length;
            }
        }

        public void WriteBytes(byte[] buffer, int offset)
        {
            var numberOfContextElements = (byte) Count;

            ByteWriter.WriteByte(buffer, offset + 0, numberOfContextElements);
            ByteWriter.WriteByte(buffer, offset + 1, Reserved1);
            LittleEndianWriter.WriteUInt16(buffer, offset + 2, Reserved2);
            offset += 4;
            for (var index = 0; index < numberOfContextElements; index++)
            {
                this[index].WriteBytes(buffer, offset);
                offset += this[index].Length;
            }
        }

        public void WriteBytes(byte[] buffer, ref int offset)
        {
            WriteBytes(buffer, offset);
            offset += Length;
        }
    }
}