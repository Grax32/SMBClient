using System;

namespace RedstoneSmb.SMB2.Enums
{
    [Flags]
    public enum Smb2TransformHeaderFlags : ushort
    {
        Encrypted = 0x0001
    }
}