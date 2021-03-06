/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

using Azos.Conf;
using Azos.Serialization.Slim;

namespace Azos.Glue.Native
{
    /// <summary>
    /// Provides synchronous communication pattern based on TCP blocking sockets and Slim serializer
    /// for maximum serialization performance and lowest latency.
    /// This binding is usable for interconnection between Azos-native components on LANs (i.e. server clusters)
    /// in scenarios when low latency is more important than total call invocation throughput
    /// </summary>
    public class SyncBinding : Binding
    {
        #region CONSTS

            public const int DEFAULT_RCV_TIMEOUT = 0;
            public const int DEFAULT_SND_TIMEOUT = 0;

            public const int DEFAULT_PORT = 8000;

            public static readonly TypeRegistry KNOWN_SERIALIZER_TYPES = new TypeRegistry(
                                                                 TypeRegistry.GlueProtocolTypes,
                                                                 TypeRegistry.BoxedCommonTypes,
                                                                 TypeRegistry.BoxedCommonNullableTypes,
                                                                 TypeRegistry.CommonCollectionTypes,
                                                                 TypeRegistry.DataAccessCRUDTypes);

    #endregion

        public SyncBinding(IGlueImplementation glue, string name = null, Provider provider = null) : base(glue, name, provider)
        {
        }


        #region Fields

          private int m_MaxMsgSize = Consts.DEFAULT_MAX_MSG_SIZE;

          private int m_ServerReceiveBufferSize = Consts.DEFAULT_RCV_BUFFER_SIZE;
          private int m_ServerSendBufferSize = Consts.DEFAULT_SND_BUFFER_SIZE;
          private int m_ClientReceiveBufferSize = Consts.DEFAULT_RCV_BUFFER_SIZE;
          private int m_ClientSendBufferSize = Consts.DEFAULT_SND_BUFFER_SIZE;

          private int m_ServerReceiveTimeout = DEFAULT_RCV_TIMEOUT;
          private int m_ServerSendTimeout = DEFAULT_SND_TIMEOUT;
          private int m_ClientReceiveTimeout = DEFAULT_RCV_TIMEOUT;
          private int m_ClientSendTimeout = DEFAULT_SND_TIMEOUT;

        #endregion

        #region Properties
            /// <summary>
            /// Sync binding is synchronous by definition
            /// </summary>
            public override OperationFlow OperationFlow
            {
                get { return OperationFlow.Synchronous; }
            }

            public override string EncodingFormat
            {
              get { return Consts.SLIM_FORMAT; }
            }

            /// <summary>
            /// Override in conjunction with DoEncodeRequest/DoDecodeRsponse
            /// </summary>
            public virtual int FrameFormat
            {
              get{ return WireFrame.SLIM_FORMAT; }
            }

            /// <summary>
            /// Imposes a limit on maximum message size in bytes
            /// </summary>
            [Config("$" + Consts.CONFIG_MAX_MSG_SIZE_ATTR, Consts.DEFAULT_MAX_MSG_SIZE)]
            public int MaxMsgSize
            {
               get { return m_MaxMsgSize; }
               set { m_MaxMsgSize = value < Consts.MAX_MSG_SIZE_LOW_BOUND ? Consts.MAX_MSG_SIZE_LOW_BOUND : value;}
            }


            [Config(CONFIG_SERVER_TRANSPORT_SECTION + ATTR_SLASH_PATH + Consts.CONFIG_RCV_BUF_SIZE_ATTR, Consts.DEFAULT_RCV_BUFFER_SIZE)]
            public int ServerReceiveBufferSize
            {
               get { return m_ServerReceiveBufferSize; }
               set { m_ServerReceiveBufferSize = value <=0 ? Consts.DEFAULT_RCV_BUFFER_SIZE : value;}
            }

            [Config(CONFIG_SERVER_TRANSPORT_SECTION + ATTR_SLASH_PATH + Consts.CONFIG_SND_BUF_SIZE_ATTR, Consts.DEFAULT_SND_BUFFER_SIZE)]
            public int ServerSendBufferSize
            {
               get { return m_ServerSendBufferSize; }
               set { m_ServerSendBufferSize = value <=0 ? Consts.DEFAULT_SND_BUFFER_SIZE : value;}
            }

            [Config(CONFIG_CLIENT_TRANSPORT_SECTION + ATTR_SLASH_PATH + Consts.CONFIG_RCV_BUF_SIZE_ATTR, Consts.DEFAULT_RCV_BUFFER_SIZE)]
            public int ClientReceiveBufferSize
            {
               get { return m_ClientReceiveBufferSize; }
               set { m_ClientReceiveBufferSize = value <=0 ? Consts.DEFAULT_RCV_BUFFER_SIZE : value;}
            }

            [Config(CONFIG_CLIENT_TRANSPORT_SECTION + ATTR_SLASH_PATH + Consts.CONFIG_SND_BUF_SIZE_ATTR, Consts.DEFAULT_SND_BUFFER_SIZE)]
            public int ClientSendBufferSize
            {
               get { return m_ClientSendBufferSize; }
               set { m_ClientSendBufferSize = value <=0 ? Consts.DEFAULT_SND_BUFFER_SIZE : value;}
            }

            [Config(CONFIG_SERVER_TRANSPORT_SECTION + ATTR_SLASH_PATH + Consts.CONFIG_RCV_TIMEOUT_ATTR, DEFAULT_RCV_TIMEOUT)]
            public int ServerReceiveTimeout
            {
               get { return m_ServerReceiveTimeout; }
               set { m_ServerReceiveTimeout = value <0 ? DEFAULT_RCV_TIMEOUT : value;}
            }

            [Config(CONFIG_SERVER_TRANSPORT_SECTION + ATTR_SLASH_PATH + Consts.CONFIG_SND_TIMEOUT_ATTR, DEFAULT_SND_TIMEOUT)]
            public int ServerSendTimeout
            {
               get { return m_ServerSendTimeout; }
               set { m_ServerSendTimeout = value <0 ? DEFAULT_SND_TIMEOUT : value;}
            }

            [Config(CONFIG_CLIENT_TRANSPORT_SECTION + ATTR_SLASH_PATH + Consts.CONFIG_RCV_TIMEOUT_ATTR, DEFAULT_RCV_TIMEOUT)]
            public int ClientReceiveTimeout
            {
               get { return m_ClientReceiveTimeout; }
               set { m_ClientReceiveTimeout = value <0 ? DEFAULT_RCV_TIMEOUT : value;}
            }

            [Config(CONFIG_CLIENT_TRANSPORT_SECTION + ATTR_SLASH_PATH + Consts.CONFIG_SND_TIMEOUT_ATTR, DEFAULT_SND_TIMEOUT)]
            public int ClientSendTimeout
            {
               get { return m_ClientSendTimeout; }
               set { m_ClientSendTimeout = value <0 ? DEFAULT_SND_TIMEOUT : value;}
            }



        #endregion

        #region Public
            public override bool AreNodesIdentical(Node left, Node right)
            {
                return  left.Assigned && right.Assigned && left.ConnectString.EqualsIgnoreCase(right.ConnectString);
            }
        #endregion

        #region Protected

            protected internal static IPEndPoint ToIPEndPoint(Node node)
            {
                return (node.Host + ':' + node.Service).ToIPEndPoint(DEFAULT_PORT);
            }


            protected override void DoStart()
            {
                base.DoStart();
            }

            protected override void DoWaitForCompleteStop()
            {
                base.DoWaitForCompleteStop();
            }

            protected override ClientTransport MakeNewClientTransport(ClientEndPoint client)
            {
               return new SyncClientTransport(this, client.Node);
            }

            protected internal override ServerTransport OpenServerEndpoint(ServerEndPoint epoint)
            {
                var cfg = ConfigNode.NavigateSection(CONFIG_SERVER_TRANSPORT_SECTION);
                if (!cfg.Exists) cfg = ConfigNode;

                var ipep = SyncBinding.ToIPEndPoint(epoint.Node);
                var transport = new SyncServerTransport(this, epoint, ipep.Address, ipep.Port);
                transport.Configure(cfg);
                transport.Start();

                return transport;
            }

            protected internal override void CloseServerEndpoint(ServerEndPoint epoint)
            {
                var t = epoint.Transport;
                if (t!=null) t.Dispose();
            }

            internal static void socketRead(NetworkStream nets, byte[] buffer, int offset, int total)
            {
              int cnt = 0;
              while(cnt<total)
              {
                var got = nets.Read(buffer, offset + cnt, total - cnt);
                if (got<=0) throw new SocketException();
                cnt += got;
              }
            }
       #endregion

    }
}
