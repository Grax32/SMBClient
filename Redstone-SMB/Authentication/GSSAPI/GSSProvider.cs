/* Copyright (C) 2017-2018 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */

using System.Collections.Generic;
using RedstoneSmb.Authentication.GSSAPI.Enums;
using RedstoneSmb.Authentication.GSSAPI.SPNEGO;
using RedstoneSmb.Authentication.NTLM.Helpers;
using RedstoneSmb.Authentication.NTLM.Structures;
using RedstoneSmb.Authentication.NTLM.Structures.Enums;
using RedstoneSmb.Enums;
using ByteUtils = RedstoneSmb.Utilities.ByteUtils.ByteUtils;

namespace RedstoneSmb.Authentication.GSSAPI
{
    public class GssContext
    {
        internal IGssMechanism Mechanism;
        internal object MechanismContext;

        internal GssContext(IGssMechanism mechanism, object mechanismContext)
        {
            Mechanism = mechanism;
            MechanismContext = mechanismContext;
        }
    }

    public class GssProvider
    {
        public static readonly byte[] NtlmsspIdentifier = {0x2b, 0x06, 0x01, 0x04, 0x01, 0x82, 0x37, 0x02, 0x02, 0x0a};

        private readonly List<IGssMechanism> _mMechanisms;

        public GssProvider(IGssMechanism mechanism)
        {
            _mMechanisms = new List<IGssMechanism>();
            _mMechanisms.Add(mechanism);
        }

        public GssProvider(List<IGssMechanism> mechanisms)
        {
            _mMechanisms = mechanisms;
        }

        public byte[] GetSpnegoTokenInitBytes()
        {
            var token = new SimpleProtectedNegotiationTokenInit();
            token.MechanismTypeList = new List<byte[]>();
            foreach (var mechanism in _mMechanisms) token.MechanismTypeList.Add(mechanism.Identifier);
            return token.GetBytes(true);
        }

        public virtual NtStatus AcceptSecurityContext(ref GssContext context, byte[] inputToken, out byte[] outputToken)
        {
            outputToken = null;
            SimpleProtectedNegotiationToken spnegoToken = null;
            try
            {
                spnegoToken = SimpleProtectedNegotiationToken.ReadToken(inputToken, 0, false);
            }
            catch
            {
            }

            if (spnegoToken != null)
            {
                if (spnegoToken is SimpleProtectedNegotiationTokenInit)
                {
                    var tokenInit = (SimpleProtectedNegotiationTokenInit) spnegoToken;
                    if (tokenInit.MechanismTypeList.Count == 0) return NtStatus.SecEInvalidToken;

                    // RFC 4178: Note that in order to avoid an extra round trip, the first context establishment token
                    // of the initiator's preferred mechanism SHOULD be embedded in the initial negotiation message.
                    var preferredMechanism = tokenInit.MechanismTypeList[0];
                    var mechanism = FindMechanism(preferredMechanism);
                    var isPreferredMechanism = mechanism != null;
                    if (!isPreferredMechanism) mechanism = FindMechanism(tokenInit.MechanismTypeList);

                    if (mechanism != null)
                    {
                        NtStatus status;
                        context = new GssContext(mechanism, null);
                        if (isPreferredMechanism)
                        {
                            byte[] mechanismOutput;
                            status = mechanism.AcceptSecurityContext(ref context.MechanismContext,
                                tokenInit.MechanismToken, out mechanismOutput);
                            outputToken = GetSpnegoTokenResponseBytes(mechanismOutput, status, mechanism.Identifier);
                        }
                        else
                        {
                            status = NtStatus.SecIContinueNeeded;
                            outputToken = GetSpnegoTokenResponseBytes(null, status, mechanism.Identifier);
                        }

                        return status;
                    }

                    return NtStatus.SecESecpkgNotFound;
                }
                else // SimpleProtectedNegotiationTokenResponse
                {
                    if (context == null) return NtStatus.SecEInvalidToken;
                    var mechanism = context.Mechanism;
                    var tokenResponse = (SimpleProtectedNegotiationTokenResponse) spnegoToken;
                    byte[] mechanismOutput;
                    var status = mechanism.AcceptSecurityContext(ref context.MechanismContext,
                        tokenResponse.ResponseToken, out mechanismOutput);
                    outputToken = GetSpnegoTokenResponseBytes(mechanismOutput, status, null);
                    return status;
                }
            }

            // [MS-SMB] The Windows GSS implementation supports raw Kerberos / NTLM messages in the SecurityBlob.
            // [MS-SMB2] Windows [..] will also accept raw Kerberos messages and implicit NTLM messages as part of GSS authentication.
            if (AuthenticationMessageUtils.IsSignatureValid(inputToken))
            {
                var messageType = AuthenticationMessageUtils.GetMessageType(inputToken);
                var ntlmAuthenticationProvider = FindMechanism(NtlmsspIdentifier);
                if (ntlmAuthenticationProvider != null)
                {
                    if (messageType == MessageTypeName.Negotiate)
                        context = new GssContext(ntlmAuthenticationProvider, null);

                    if (context == null) return NtStatus.SecEInvalidToken;

                    var status = ntlmAuthenticationProvider.AcceptSecurityContext(ref context.MechanismContext,
                        inputToken, out outputToken);
                    return status;
                }

                return NtStatus.SecESecpkgNotFound;
            }

            return NtStatus.SecEInvalidToken;
        }

        public virtual object GetContextAttribute(GssContext context, GssAttributeName attributeName)
        {
            if (context == null) return null;
            var mechanism = context.Mechanism;
            return mechanism.GetContextAttribute(context.MechanismContext, attributeName);
        }

        public virtual bool DeleteSecurityContext(ref GssContext context)
        {
            if (context != null)
            {
                var mechanism = context.Mechanism;
                return mechanism.DeleteSecurityContext(ref context.MechanismContext);
            }

            return false;
        }

        /// <summary>
        ///     Helper method for legacy implementation.
        /// </summary>
        public virtual NtStatus GetNtlmChallengeMessage(out GssContext context, NegotiateMessage negotiateMessage,
            out ChallengeMessage challengeMessage)
        {
            var ntlmAuthenticationProvider = FindMechanism(NtlmsspIdentifier);
            if (ntlmAuthenticationProvider != null)
            {
                context = new GssContext(ntlmAuthenticationProvider, null);
                byte[] outputToken;
                var result = ntlmAuthenticationProvider.AcceptSecurityContext(ref context.MechanismContext,
                    negotiateMessage.GetBytes(), out outputToken);
                challengeMessage = new ChallengeMessage(outputToken);
                return result;
            }

            context = null;
            challengeMessage = null;
            return NtStatus.SecESecpkgNotFound;
        }

        /// <summary>
        ///     Helper method for legacy implementation.
        /// </summary>
        public virtual NtStatus NtlmAuthenticate(GssContext context, AuthenticateMessage authenticateMessage)
        {
            if (context != null && ByteUtils.AreByteArraysEqual(context.Mechanism.Identifier, NtlmsspIdentifier))
            {
                var mechanism = context.Mechanism;
                byte[] outputToken;
                var result = mechanism.AcceptSecurityContext(ref context.MechanismContext,
                    authenticateMessage.GetBytes(), out outputToken);
                return result;
            }

            return NtStatus.SecESecpkgNotFound;
        }

        public IGssMechanism FindMechanism(List<byte[]> mechanismIdentifiers)
        {
            foreach (var identifier in mechanismIdentifiers)
            {
                var mechanism = FindMechanism(identifier);
                if (mechanism != null) return mechanism;
            }

            return null;
        }

        public IGssMechanism FindMechanism(byte[] mechanismIdentifier)
        {
            foreach (var mechanism in _mMechanisms)
                if (ByteUtils.AreByteArraysEqual(mechanism.Identifier, mechanismIdentifier))
                    return mechanism;
            return null;
        }

        private static byte[] GetSpnegoTokenResponseBytes(byte[] mechanismOutput, NtStatus status,
            byte[] mechanismIdentifier)
        {
            var tokenResponse = new SimpleProtectedNegotiationTokenResponse();
            if (status == NtStatus.StatusSuccess)
                tokenResponse.NegState = NegState.AcceptCompleted;
            else if (status == NtStatus.SecIContinueNeeded)
                tokenResponse.NegState = NegState.AcceptIncomplete;
            else
                tokenResponse.NegState = NegState.Reject;
            tokenResponse.SupportedMechanism = mechanismIdentifier;
            tokenResponse.ResponseToken = mechanismOutput;
            return tokenResponse.GetBytes();
        }
    }
}