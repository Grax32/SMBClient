/* Copyright (C) 2017 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */

using System.Collections.Generic;
using ByteReader = RedstoneSmb.Utilities.ByteUtils.ByteReader;
using ByteWriter = RedstoneSmb.Utilities.ByteUtils.ByteWriter;

namespace RedstoneSmb.RPC.Structures
{
    /// <summary>
    ///     p_rt_versions_supported_t
    /// </summary>
    public class VersionsSupported
    {
        public List<Version> Entries = new List<Version>(); // p_protocols

        public VersionsSupported()
        {
        }

        public VersionsSupported(byte[] buffer, int offset)
        {
            var protocols = ByteReader.ReadByte(buffer, offset + 0);
            Entries = new List<Version>();
            for (var index = 0; index < protocols; index++)
            {
                var version = new Version(buffer, offset + 1 + index * Version.Length);
                Entries.Add(version);
            }
        }

        public int Count => Entries.Count;

        public int Length => 1 + Count * Version.Length;

        public void WriteBytes(byte[] buffer, int offset)
        {
            ByteWriter.WriteByte(buffer, offset + 0, (byte) Count);
            for (var index = 0; index < Entries.Count; index++)
                Entries[index].WriteBytes(buffer, offset + 1 + index * Version.Length);
        }
    }
}