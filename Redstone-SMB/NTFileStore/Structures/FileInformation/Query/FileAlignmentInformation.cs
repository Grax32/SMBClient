/* Copyright (C) 2017 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */

using RedstoneSmb.NTFileStore.Enums.FileInformation;
using LittleEndianConverter = RedstoneSmb.Utilities.Conversion.LittleEndianConverter;
using LittleEndianWriter = RedstoneSmb.Utilities.ByteUtils.LittleEndianWriter;

namespace RedstoneSmb.NTFileStore.Structures.FileInformation.Query
{
    /// <summary>
    ///     [MS-FSCC] 2.4.3 - FileAlignmentInformation
    /// </summary>
    public class FileAlignmentInformation : FileInformation
    {
        public const int FixedLength = 4;

        public uint AlignmentRequirement;

        public FileAlignmentInformation()
        {
        }

        public FileAlignmentInformation(byte[] buffer, int offset)
        {
            AlignmentRequirement = LittleEndianConverter.ToUInt32(buffer, offset + 0);
        }

        public override FileInformationClass FileInformationClass => FileInformationClass.FileAlignmentInformation;

        public override int Length => FixedLength;

        public override void WriteBytes(byte[] buffer, int offset)
        {
            LittleEndianWriter.WriteUInt32(buffer, offset + 0, AlignmentRequirement);
        }
    }
}