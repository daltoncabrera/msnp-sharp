#region Copyright (c) 2002-2011, Bas Geertsema, Xih Solutions (http://www.xihsolutions.net), Thiago.Sayao, Pang Wu, Ethem Evlice
/*
Copyright (c) 2002-2011, Bas Geertsema, Xih Solutions
(http://www.xihsolutions.net), Thiago.Sayao, Pang Wu, Ethem Evlice.
All rights reserved. http://code.google.com/p/msnp-sharp/

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

* Redistributions of source code must retain the above copyright notice,
  this list of conditions and the following disclaimer.
* Redistributions in binary form must reproduce the above copyright notice,
  this list of conditions and the following disclaimer in the documentation
  and/or other materials provided with the distribution.
* Neither the names of Bas Geertsema or Xih Solutions nor the names of its
  contributors may be used to endorse or promote products derived from this
  software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS 'AS IS'
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE
LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF
THE POSSIBILITY OF SUCH DAMAGE. 
*/
#endregion

using System;
using System.Net;
using System.Collections.Generic;
using System.Text;

namespace MSNPSharp.P2P
{
    using MSNPSharp.Core;

    public class DestinationAddressUpdatedEventHandler : EventArgs
    {
        private IPEndPoint[] ipEndPoints;

        public IPEndPoint[] IPEndPoints
        {
            get
            {
                return ipEndPoints;
            }
        }

        public DestinationAddressUpdatedEventHandler(IPEndPoint[] ipeps)
        {
            this.ipEndPoints = ipeps;
        }
    }

    public class TCPv1Bridge : P2PBridge
    {
        public event EventHandler<DestinationAddressUpdatedEventHandler> DestinationAddressUpdated;

        private NSMessageHandler nsMessageHandler = null;
        private P2PSession startupSession = null;
        private P2PDirectProcessor directConnection = null;

        public override bool IsOpen
        {
            get
            {
                return (directConnection != null &&
                    directConnection.DCState == DirectConnectionState.Established);
            }
        }

        public override int MaxDataSize
        {
            get
            {
                return 1352;
            }
        }

        private Contact remote = null;
        public override Contact Remote
        {
            get
            {
                if (remote != null)
                    return remote;

                if (startupSession != null)
                    return startupSession.Remote;

                foreach (P2PSession s in SendQueues.Keys)
                {
                    return s.Remote;
                }

                return null;
            }
        }

        public IPEndPoint RemoteEndPoint
        {
            get
            {
                if (directConnection != null)
                    return directConnection.RemoteEndPoint as IPEndPoint;

                return null;
            }
        }

        public IPEndPoint LocalEndPoint
        {
            get
            {
                if (directConnection != null)
                    return directConnection.LocalEndPoint as IPEndPoint;

                return null;
            }
        }
        public TCPv1Bridge(
            ConnectivitySettings connectivitySettings,
            P2PVersion p2pVersion,
            Guid replyNonce, Guid remoteNonce, bool isNeedHash,
            P2PSession p2pSession,
            NSMessageHandler ns,
            Contact remote)
            : base(0)
        {
            this.startupSession = p2pSession;
            this.nsMessageHandler = ns;
            this.remote = remote;

            directConnection = new P2PDirectProcessor(connectivitySettings, p2pVersion, replyNonce, remoteNonce, isNeedHash, p2pSession, nsMessageHandler);
            directConnection.HandshakeCompleted += new EventHandler<EventArgs>(directConnection_HandshakeCompleted);
            directConnection.P2PMessageReceived += new EventHandler<P2PMessageEventArgs>(directConnection_P2PMessageReceived);
            directConnection.SendCompleted += new EventHandler<MSNPSharp.Core.ObjectEventArgs>(directConnection_SendCompleted);

            directConnection.DirectNegotiationTimedOut += new EventHandler<EventArgs>(directConnection_DirectNegotiationTimedOut);
            directConnection.ConnectionClosed += new EventHandler<EventArgs>(directConnection_ConnectionClosed);
            directConnection.ConnectingException += new EventHandler<ExceptionEventArgs>(directConnection_ConnectingException);
            directConnection.ConnectionException += new EventHandler<ExceptionEventArgs>(directConnection_ConnectionException);
        }

        protected internal virtual void OnDestinationAddressUpdated(DestinationAddressUpdatedEventHandler e)
        {
            if (directConnection != null && directConnection.Connected && directConnection.RemoteEndPoint != null)
            {
                IPEndPoint remoteIpep = (IPEndPoint)directConnection.RemoteEndPoint;
                bool trustedPeer = false;

                foreach (IPEndPoint ipep in e.IPEndPoints)
                {
                    if (ipep.Address.Equals(remoteIpep.Address) && ipep.Port == remoteIpep.Port)
                    {
                        trustedPeer = true;
                        break;
                    }
                }

                if (trustedPeer == false)
                {
                    directConnection.Disconnect();
                    OnBridgeClosed(EventArgs.Empty);
                }
            }

            if (DestinationAddressUpdated != null)
                DestinationAddressUpdated(this, e);
        }

        private void directConnection_HandshakeCompleted(object sender, EventArgs e)
        {
            OnBridgeOpened(e);
        }

        private void directConnection_DirectNegotiationTimedOut(object sender, EventArgs e)
        {
            OnBridgeClosed(EventArgs.Empty);
        }

        private void directConnection_ConnectionException(object sender, ExceptionEventArgs e)
        {
            OnBridgeClosed(EventArgs.Empty);
        }

        private void directConnection_ConnectingException(object sender, ExceptionEventArgs e)
        {
            OnBridgeClosed(EventArgs.Empty);
        }

        private void directConnection_ConnectionClosed(object sender, EventArgs e)
        {
            OnBridgeClosed(EventArgs.Empty);
        }

        public void Listen(IPAddress address, int port)
        {
            directConnection.Listen(address, port);
        }

        public void Connect()
        {
            directConnection.Connect();
        }

        private void directConnection_P2PMessageReceived(object sender, P2PMessageEventArgs e)
        {
            nsMessageHandler.P2PHandler.ProcessP2PMessage(this, Remote, Remote.SelectRandomEPID(), e.P2PMessage);
        }

        protected override void SendOnePacket(P2PSession session, Contact remote, Guid remoteGuid, P2PMessage msg)
        {
            directConnection.SendMessage(msg, new P2PMessageSessionEventArgs(msg, session));
        }

        private void directConnection_SendCompleted(object sender, ObjectEventArgs e)
        {
            OnBridgeSent(e.Object as P2PMessageSessionEventArgs);
        }
    }
};