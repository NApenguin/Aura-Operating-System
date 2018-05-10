﻿using System;
using Sys = System;
using System.Collections.Generic;
using System.Text;

namespace Aura_OS.System.Network.IPV4.TCP
{
    public class TCPClient
    {
        // TODO: Once we support more than just IPv4, we really need to base all the IPv4 classes on abstract classes
        // that represent the required functionality, then we can generalize the stack to be independent from IPv4 or IPv6
        internal class DataGram
        {
            internal byte[] data;
            internal IPV4.EndPoint source;

            internal DataGram(byte[] data, IPV4.EndPoint src)
            {
                this.data = data;
                this.source = src;
            }
        }

        private static TempDictionary<TCPClient> clients;

        protected Int32 localPort;
        protected IPV4.Address destination;
        protected Int32 destinationPort;

        private Queue<DataGram> rxBuffer;

        static TCPClient()
        {
            clients = new TempDictionary<TCPClient>();
        }

        internal static TCPClient Client(ushort destPort)
        {
            if (clients.ContainsKey((UInt32)destPort) == true)
            {
                return clients[(UInt32)destPort];
            }

            return null;
        }

        public TCPClient()
            :this(0)
        { }

        public TCPClient(Int32 localPort)
        {
            this.rxBuffer = new Queue<DataGram>(8);

            this.localPort = localPort;
            if (localPort > 0)
            {
                TCPClient.clients.Add((UInt32)localPort, this);
            }
        }

        public TCPClient(IPV4.Address dest, Int32 destPort)
            : this(0)
        {
            this.destination = dest;
            this.destinationPort = destPort;
        }

        public void Connect(IPV4.Address dest, Int32 destPort)
        {
            this.destination = dest;
            this.destinationPort = destPort;
        }

        public void Send(byte[] data)
        {
            if ((this.destination == null) ||
                (this.destinationPort == 0))
            {
                throw new Exception("Must establish a default remote host by calling Connect() before using this Send() overload");
            }

            Send(data, this.destination, this.destinationPort);
            NetworkStack.Update();
        }

        public void Send(byte[] data, IPV4.Address dest, Int32 destPort)
        {
            IPV4.Address source = IPV4.Config.FindNetwork(dest);
            IPV4.TCP.TCPPacket packet = new IPV4.TCP.TCPPacket(source, dest, (UInt16)this.localPort, (UInt16)destPort, data, 0x10F41EE2, 0xEF93E62E, 0x50, 0x18, 0x0100, 0xBE74, 0x0000);

            Console.WriteLine("Sending " + packet.ToString());
            IPV4.OutgoingBuffer.AddPacket(packet);
        }

        public void Close()
        {
            if (TCPClient.clients.ContainsKey((UInt32)this.localPort) == true)
            {
                TCPClient.clients.Remove((UInt32)this.localPort);
            }
        }

        public byte[] Receive(ref IPV4.EndPoint source)
        {
            if (this.rxBuffer.Count < 1)
            {
                return null;
            }

            DataGram packet = rxBuffer.Dequeue();
            source.address = packet.source.address;
            source.port = packet.source.port;

            return packet.data;
        }

        internal void receiveData(IPV4.TCP.TCPPacket packet)
        {
            byte[] data = packet.TCP_Data;
            IPV4.EndPoint source = new IPV4.EndPoint(packet.SourceIP, packet.SourcePort);

            Console.WriteLine("\nReceived TCP Packet (" + data.Length + "bytes) from " + source.ToString());
            Console.WriteLine("Content: " + Encoding.ASCII.GetString(data));

            this.rxBuffer.Enqueue(new DataGram(data, source));
        }
    }
}
