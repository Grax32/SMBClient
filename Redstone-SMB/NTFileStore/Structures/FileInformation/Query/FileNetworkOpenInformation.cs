/* Copyright (C) 2017 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */

using System;
using RedstoneSmb.Helpers;
using RedstoneSmb.NTFileStore.Enums.FileInformation;
using LittleEndianConverter = RedstoneSmb.Utilities.Conversion.LittleEndianConverter;
using LittleEndianWriter = RedstoneSmb.Utilities.ByteUtils.LittleEndianWriter;

namespace RedstoneSmb.NTFileStore.Structures.FileInformation.Query
{
    /// <summary>
    ///     [MS-FSCC] 2.4.12 - FileNetworkOpenInformation
    /// </summary>
    public class FileNetworkOpenInformation : FileInformation
    {
        public const int FixedLength = 56;
        public long AllocationSize;
        public DateTime? ChangeTime;

        public DateTime? CreationTime;
        public long EndOfFile;
        public FileAttributes FileAttributes;
        public DateTime? LastAccessTime;
        public DateTime? LastWriteTime;
        public uint Reserved;

        public FileNetworkOpenInformation()
        {
        }

        public FileNetworkOpenInformation(byte[] buffer, int offset)
        {
            CreationTime = FileTimeHelper.ReadNullableFileTime(buffer, offset + 0);
            LastAccessTime = FileTimeHelper.ReadNullableFileTime(buffer, offset + 8);
            LastWriteTime = FileTimeHelper.ReadNullableFileTime(buffer, offset + 16);
            ChangeTime = FileTimeHelper.ReadNullableFileTime(buffer, offset + 24);
            AllocationSize = LittleEndianConverter.ToInt64(buffer, offset + 32);
            EndOfFile = LittleEndianConverter.ToInt64(buffer, offset + 40);
            FileAttributes = (FileAttributes) LittleEndianConverter.ToUInt32(buffer, offset + 48);
            Reserved = LittleEndianConverter.ToUInt32(buffer, offset + 52);
        }

        public bool IsDirectory
        {
            get => (FileAttributes & FileAttributes.Directory) > 0;
            set
            {
                if (value)
                    FileAttributes |= FileAttributes.Directory;
                else
                    FileAttributes &= ~FileAttributes.Directory;
            }
        }

        public override FileInformationClass FileInformationClass => FileInformationClass.FileNetworkOpenInformation;

        public override int Length => FixedLength;

        public override void WriteBytes(byte[] buffer, int offset)
        {
            FileTimeHelper.WriteFileTime(buffer, offset + 0, CreationTime);
            FileTimeHelper.WriteFileTime(buffer, offset + 8, LastAccessTime);
            FileTimeHelper.WriteFileTime(buffer, offset + 16, LastWriteTime);
            FileTimeHelper.WriteFileTime(buffer, offset + 24, ChangeTime);
            LittleEndianWriter.WriteInt64(buffer, offset + 32, AllocationSize);
            LittleEndianWriter.WriteInt64(buffer, offset + 40, EndOfFile);
            LittleEndianWriter.WriteUInt32(buffer, offset + 48, (uint) FileAttributes);
            LittleEndianWriter.WriteUInt32(buffer, offset + 52, Reserved);
        }
    }
}