/* Copyright (C) 2014 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */

using RedstoneSmb.Enums;
using RedstoneSmb.RPC.NDR;
using RedstoneSmb.Services.ServerService.Structures.ServerInfo;

namespace RedstoneSmb.Services.ServerService
{
    /// <summary>
    ///     NetrServerGetInfo Response (opnum 21)
    /// </summary>
    public class NetrServerGetInfoResponse
    {
        public ServerInfo InfoStruct;
        public Win32Error Result;

        public NetrServerGetInfoResponse()
        {
        }

        public NetrServerGetInfoResponse(byte[] buffer)
        {
            var parser = new NdrParser(buffer);
            InfoStruct = new ServerInfo(parser);
            // 14.4 - If an operation returns a result, the representation of the result appears after all parameters in
            Result = (Win32Error) parser.ReadUInt32();
        }

        public byte[] GetBytes()
        {
            var writer = new NdrWriter();
            writer.WriteStructure(InfoStruct);
            writer.WriteUInt32((uint) Result);

            return writer.GetBytes();
        }
    }
}