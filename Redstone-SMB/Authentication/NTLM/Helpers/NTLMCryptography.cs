/* Copyright (C) 2014-2017 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */

using System;
using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using RedstoneSmb.Authentication.NTLM.Structures.Enums;
using ByteReader = RedstoneSmb.Utilities.ByteUtils.ByteReader;
using ByteUtils = RedstoneSmb.Utilities.ByteUtils.ByteUtils;

namespace RedstoneSmb.Authentication.NTLM.Helpers
{
    public static class NtlmCryptography
    {
        private static bool _isReady = InitCodePage();

        private static bool InitCodePage()
        {
            CodePagesEncodingProvider.Instance.GetEncoding(437);
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            return true;
        }

        public static byte[] ComputeLMv1Response(byte[] challenge, string password)
        {
            var hash = LmowFv1(password);
            return DesLongEncrypt(hash, challenge);
        }

        public static byte[] ComputeNtlMv1Response(byte[] challenge, string password)
        {
            var hash = NtowFv1(password);
            return DesLongEncrypt(hash, challenge);
        }

        public static byte[] ComputeNtlMv1ExtendedSessionSecurityResponse(byte[] serverChallenge,
            byte[] clientChallenge, string password)
        {
            var passwordHash = NtowFv1(password);
            var challengeHash = MD5.Create().ComputeHash(ByteUtils.Concatenate(serverChallenge, clientChallenge));
            var challengeHashShort = new byte[8];
            Array.Copy(challengeHash, 0, challengeHashShort, 0, 8);
            return DesLongEncrypt(passwordHash, challengeHashShort);
        }

        public static byte[] ComputeLMv2Response(byte[] serverChallenge, byte[] clientChallenge, string password,
            string user, string domain)
        {
            var key = LmowFv2(password, user, domain);
            var bytes = ByteUtils.Concatenate(serverChallenge, clientChallenge);
            var hmac = new HMACMD5(key);
            var hash = hmac.ComputeHash(bytes, 0, bytes.Length);

            return ByteUtils.Concatenate(hash, clientChallenge);
        }

        /// <summary>
        ///     [MS-NLMP] https://msdn.microsoft.com/en-us/library/cc236700.aspx
        /// </summary>
        /// <param name="clientChallengeStructurePadded">ClientChallengeStructure with 4 zero bytes padding, a.k.a. temp</param>
        public static byte[] ComputeNtlMv2Proof(byte[] serverChallenge, byte[] clientChallengeStructurePadded,
            string password, string user, string domain)
        {
            var key = NtowFv2(password, user, domain);
            var temp = clientChallengeStructurePadded;

            var hmac = new HMACMD5(key);
            var ntProof = hmac.ComputeHash(ByteUtils.Concatenate(serverChallenge, temp), 0,
                serverChallenge.Length + temp.Length);
            return ntProof;
        }

        public static byte[] DesEncrypt(byte[] key, byte[] plainText)
        {
            return DesEncrypt(key, plainText, 0, plainText.Length);
        }

        public static byte[] DesEncrypt(byte[] key, byte[] plainText, int inputOffset, int inputCount)
        {
            var encryptor = CreateWeakDesEncryptor(CipherMode.ECB, key, new byte[key.Length]);
            var result = new byte[inputCount];
            encryptor.TransformBlock(plainText, inputOffset, inputCount, result, 0);
            return result;
        }

        public static ICryptoTransform CreateWeakDesEncryptor(CipherMode mode, byte[] rgbKey, byte[] rgbIv)
        {
            var des = DES.Create();
            des.Mode = mode;
            var trans = des.CreateEncryptor(rgbKey, rgbIv);
            return trans;
        }

        /// <summary>
        ///     DESL()
        /// </summary>
        public static byte[] DesLongEncrypt(byte[] key, byte[] plainText)
        {
            if (key.Length != 16) throw new ArgumentException("Invalid key length");

            if (plainText.Length != 8) throw new ArgumentException("Invalid plain-text length");
            var padded = new byte[21];
            Array.Copy(key, padded, key.Length);

            var k1 = new byte[7];
            var k2 = new byte[7];
            var k3 = new byte[7];
            Array.Copy(padded, 0, k1, 0, 7);
            Array.Copy(padded, 7, k2, 0, 7);
            Array.Copy(padded, 14, k3, 0, 7);

            var r1 = DesEncrypt(ExtendDesKey(k1), plainText);
            var r2 = DesEncrypt(ExtendDesKey(k2), plainText);
            var r3 = DesEncrypt(ExtendDesKey(k3), plainText);

            var result = new byte[24];
            Array.Copy(r1, 0, result, 0, 8);
            Array.Copy(r2, 0, result, 8, 8);
            Array.Copy(r3, 0, result, 16, 8);

            return result;
        }

        public static Encoding GetOemEncoding()
        {
            while (!_isReady)
            {
                Debug.WriteLine("Should not get here.");
                Thread.Sleep(1000);
            }

            return Encoding.GetEncoding(CultureInfo.CurrentCulture.TextInfo.OEMCodePage);
        }

        /// <summary>
        ///     LM Hash
        /// </summary>
        public static byte[] LmowFv1(string password)
        {
            var plainText = Encoding.ASCII.GetBytes("KGS!@#$%");
            var passwordBytes = GetOemEncoding().GetBytes(password.ToUpper());
            var key = new byte[14];
            Array.Copy(passwordBytes, key, Math.Min(passwordBytes.Length, 14));

            var k1 = new byte[7];
            var k2 = new byte[7];
            Array.Copy(key, 0, k1, 0, 7);
            Array.Copy(key, 7, k2, 0, 7);

            var part1 = DesEncrypt(ExtendDesKey(k1), plainText);
            var part2 = DesEncrypt(ExtendDesKey(k2), plainText);

            return ByteUtils.Concatenate(part1, part2);
        }

        /// <summary>
        ///     NTLM hash (NT hash)
        /// </summary>
        public static byte[] NtowFv1(string password)
        {
            var passwordBytes = Encoding.Unicode.GetBytes(password);
            return new Md4().GetByteHashFromBytes(passwordBytes);
        }

        /// <summary>
        ///     LMOWFv2 is identical to NTOWFv2
        /// </summary>
        public static byte[] LmowFv2(string password, string user, string domain)
        {
            return NtowFv2(password, user, domain);
        }

        public static byte[] NtowFv2(string password, string user, string domain)
        {
            var passwordBytes = Encoding.Unicode.GetBytes(password);
            var key = new Md4().GetByteHashFromBytes(passwordBytes);
            var text = user.ToUpper() + domain;
            var bytes = Encoding.Unicode.GetBytes(text);
            var hmac = new HMACMD5(key);
            return hmac.ComputeHash(bytes, 0, bytes.Length);
        }

        /// <summary>
        ///     Extends a 7-byte key into an 8-byte key.
        ///     Note: The DES key ostensibly consists of 64 bits, however, only 56 of these are actually used by the algorithm.
        ///     Eight bits are used solely for checking parity, and are thereafter discarded
        /// </summary>
        private static byte[] ExtendDesKey(byte[] key)
        {
            var result = new byte[8];
            int i;

            result[0] = (byte)((key[0] >> 1) & 0xff);
            result[1] = (byte)((((key[0] & 0x01) << 6) | (((key[1] & 0xff) >> 2) & 0xff)) & 0xff);
            result[2] = (byte)((((key[1] & 0x03) << 5) | (((key[2] & 0xff) >> 3) & 0xff)) & 0xff);
            result[3] = (byte)((((key[2] & 0x07) << 4) | (((key[3] & 0xff) >> 4) & 0xff)) & 0xff);
            result[4] = (byte)((((key[3] & 0x0F) << 3) | (((key[4] & 0xff) >> 5) & 0xff)) & 0xff);
            result[5] = (byte)((((key[4] & 0x1F) << 2) | (((key[5] & 0xff) >> 6) & 0xff)) & 0xff);
            result[6] = (byte)((((key[5] & 0x3F) << 1) | (((key[6] & 0xff) >> 7) & 0xff)) & 0xff);
            result[7] = (byte)(key[6] & 0x7F);
            for (i = 0; i < 8; i++) result[i] = (byte)(result[i] << 1);

            return result;
        }

        /// <summary>
        ///     [MS-NLMP] 3.4.5.1 - KXKEY - NTLM v1
        /// </summary>
        /// <remarks>
        ///     If NTLM v2 is used, KeyExchangeKey MUST be set to the value of SessionBaseKey.
        /// </remarks>
        public static byte[] KxKey(byte[] sessionBaseKey, NegotiateFlags negotiateFlags, byte[] lmChallengeResponse,
            byte[] serverChallenge, byte[] lmowf)
        {
            if ((negotiateFlags & NegotiateFlags.ExtendedSessionSecurity) == 0)
            {
                if ((negotiateFlags & NegotiateFlags.LanManagerSessionKey) > 0)
                {
                    var k1 = ByteReader.ReadBytes(lmowf, 0, 7);
                    var k2 = ByteUtils.Concatenate(ByteReader.ReadBytes(lmowf, 7, 1),
                        new byte[] { 0xBD, 0xBD, 0xBD, 0xBD, 0xBD, 0xBD });
                    var temp1 = DesEncrypt(ExtendDesKey(k1), ByteReader.ReadBytes(lmChallengeResponse, 0, 8));
                    var temp2 = DesEncrypt(ExtendDesKey(k2), ByteReader.ReadBytes(lmChallengeResponse, 0, 8));
                    var keyExchangeKey = ByteUtils.Concatenate(temp1, temp2);
                    return keyExchangeKey;
                }

                if ((negotiateFlags & NegotiateFlags.RequestLmSessionKey) > 0)
                {
                    var keyExchangeKey = ByteUtils.Concatenate(ByteReader.ReadBytes(lmowf, 0, 8), new byte[8]);
                    return keyExchangeKey;
                }

                return sessionBaseKey;
            }

            {
                var buffer = ByteUtils.Concatenate(serverChallenge, ByteReader.ReadBytes(lmChallengeResponse, 0, 8));
                var keyExchangeKey = new HMACMD5(sessionBaseKey).ComputeHash(buffer);
                return keyExchangeKey;
            }
        }
    }
}