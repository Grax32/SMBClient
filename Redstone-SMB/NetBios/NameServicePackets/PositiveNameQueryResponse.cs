/* Copyright (C) 2014-2020 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */

using System.IO;
using RedstoneSmb.NetBios.NameServicePackets.Enums;
using RedstoneSmb.NetBios.NameServicePackets.EnumStructures;
using RedstoneSmb.NetBios.NameServicePackets.Structures;
using BigEndianReader = RedstoneSmb.Utilities.ByteUtils.BigEndianReader;
using BigEndianWriter = RedstoneSmb.Utilities.ByteUtils.BigEndianWriter;
using ByteReader = RedstoneSmb.Utilities.ByteUtils.ByteReader;
using ByteWriter = RedstoneSmb.Utilities.ByteUtils.ByteWriter;

namespace RedstoneSmb.NetBios.NameServicePackets
{
    /// <summary>
    ///     [RFC 1002] 4.2.13. POSITIVE NAME QUERY RESPONSE
    /// </summary>
    public class PositiveNameQueryResponse
    {
        public const int EntryLength = 6;

        // Resource Data:
        public Utilities.Generics.KeyValuePairList<byte[], NameFlags> Addresses = new Utilities.Generics.KeyValuePairList<byte[], NameFlags>();

        public NameServicePacketHeader Header;
        public ResourceRecord Resource;

        public PositiveNameQueryResponse()
        {
            Header = new NameServicePacketHeader();
            Header.Flags = OperationFlags.AuthoritativeAnswer | OperationFlags.RecursionDesired;
            Header.OpCode = NameServiceOperation.QueryResponse;
            Header.AnCount = 1;
            Resource = new ResourceRecord(NameRecordType.Nb);
        }

        public PositiveNameQueryResponse(byte[] buffer, int offset)
        {
            Header = new NameServicePacketHeader(buffer, ref offset);
            Resource = new ResourceRecord(buffer, ref offset);
            var position = 0;
            while (position < Resource.Data.Length)
            {
                var nameFlags = (NameFlags) BigEndianReader.ReadUInt16(Resource.Data, ref position);
                var address = ByteReader.ReadBytes(Resource.Data, ref position, 4);
                Addresses.Add(address, nameFlags);
            }
        }

        public byte[] GetBytes()
        {
            Resource.Data = GetData();

            var stream = new MemoryStream();
            Header.WriteBytes(stream);
            Resource.WriteBytes(stream);
            return stream.ToArray();
        }

        private byte[] GetData()
        {
            var data = new byte[EntryLength * Addresses.Count];
            var offset = 0;
            foreach (var entry in Addresses)
            {
                BigEndianWriter.WriteUInt16(data, ref offset, (ushort) entry.Value);
                ByteWriter.WriteBytes(data, ref offset, entry.Key, 4);
            }

            return data;
        }
    }
}