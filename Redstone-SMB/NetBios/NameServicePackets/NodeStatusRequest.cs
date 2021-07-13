/* Copyright (C) 2014-2020 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */

using System.IO;
using RedstoneSmb.NetBios.NameServicePackets.Enums;
using RedstoneSmb.NetBios.NameServicePackets.Structures;

namespace RedstoneSmb.NetBios.NameServicePackets
{
    /// <summary>
    ///     [RFC 1002] 4.2.17. NODE STATUS REQUEST
    /// </summary>
    public class NodeStatusRequest
    {
        public NameServicePacketHeader Header;
        public QuestionSection Question;

        public NodeStatusRequest()
        {
            Header = new NameServicePacketHeader();
            Header.OpCode = NameServiceOperation.QueryRequest;
            Question = new QuestionSection();
            Question.Type = NameRecordType.NbStat;
        }

        public NodeStatusRequest(byte[] buffer, int offset)
        {
            Header = new NameServicePacketHeader(buffer, ref offset);
            Question = new QuestionSection(buffer, ref offset);
        }

        public byte[] GetBytes()
        {
            var stream = new MemoryStream();
            Header.WriteBytes(stream);
            Question.WriteBytes(stream);
            return stream.ToArray();
        }
    }
}