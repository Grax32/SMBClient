/* Copyright (C) 2017 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */

using RedstoneSmb.NTFileStore.Enums.FileSystemInformation;
using LittleEndianConverter = RedstoneSmb.Utilities.Conversion.LittleEndianConverter;
using LittleEndianWriter = RedstoneSmb.Utilities.ByteUtils.LittleEndianWriter;

namespace RedstoneSmb.NTFileStore.Structures.FileSystemInformation
{
    /// <summary>
    ///     [MS-FSCC] 2.5.4 - FileFsFullSizeInformation
    /// </summary>
    public class FileFsFullSizeInformation : FileSystemInformation
    {
        public const int FixedLength = 32;
        public long ActualAvailableAllocationUnits;
        public uint BytesPerSector;
        public long CallerAvailableAllocationUnits;
        public uint SectorsPerAllocationUnit;

        public long TotalAllocationUnits;

        public FileFsFullSizeInformation()
        {
        }

        public FileFsFullSizeInformation(byte[] buffer, int offset)
        {
            TotalAllocationUnits = LittleEndianConverter.ToInt64(buffer, offset + 0);
            CallerAvailableAllocationUnits = LittleEndianConverter.ToInt64(buffer, offset + 8);
            ActualAvailableAllocationUnits = LittleEndianConverter.ToInt64(buffer, offset + 16);
            SectorsPerAllocationUnit = LittleEndianConverter.ToUInt32(buffer, offset + 24);
            BytesPerSector = LittleEndianConverter.ToUInt32(buffer, offset + 28);
        }

        public override FileSystemInformationClass FileSystemInformationClass =>
            FileSystemInformationClass.FileFsFullSizeInformation;

        public override int Length => FixedLength;

        public override void WriteBytes(byte[] buffer, int offset)
        {
            LittleEndianWriter.WriteInt64(buffer, offset + 0, TotalAllocationUnits);
            LittleEndianWriter.WriteInt64(buffer, offset + 8, CallerAvailableAllocationUnits);
            LittleEndianWriter.WriteInt64(buffer, offset + 16, ActualAvailableAllocationUnits);
            LittleEndianWriter.WriteUInt32(buffer, offset + 24, SectorsPerAllocationUnit);
            LittleEndianWriter.WriteUInt32(buffer, offset + 28, BytesPerSector);
        }
    }
}