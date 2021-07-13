/* Copyright (C) 2014-2017 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */

using RedstoneSmb.NetBios.SessionPackets.Enums;
using BigEndianConverter = RedstoneSmb.Utilities.Conversion.BigEndianConverter;
using BigEndianWriter = RedstoneSmb.Utilities.ByteUtils.BigEndianWriter;

namespace RedstoneSmb.NetBios.SessionPackets
{
    /// <summary>
    ///     [RFC 1002] 4.3.5. SESSION RETARGET RESPONSE PACKET
    /// </summary>
    public class SessionRetargetResponsePacket : SessionPacket
    {
        private readonly uint _iPAddress;
        private readonly ushort _port;

        public SessionRetargetResponsePacket() : base()
        {
            Type = SessionPacketTypeName.RetargetSessionResponse;
        }

        public SessionRetargetResponsePacket(byte[] buffer, int offset) : base(buffer, offset)
        {
            _iPAddress = BigEndianConverter.ToUInt32(Trailer, offset + 0);
            _port = BigEndianConverter.ToUInt16(Trailer, offset + 4);
        }

        public override int Length => HeaderLength + 6;

        public override byte[] GetBytes()
        {
            Trailer = new byte[6];
            BigEndianWriter.WriteUInt32(Trailer, 0, _iPAddress);
            BigEndianWriter.WriteUInt16(Trailer, 4, _port);
            return base.GetBytes();
        }
    }
}