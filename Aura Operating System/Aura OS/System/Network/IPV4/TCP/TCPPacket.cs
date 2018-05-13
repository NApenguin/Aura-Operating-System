﻿/*
* PROJECT:          Aura Operating System Development
* CONTENT:          TCP Packet
* PROGRAMMERS:      Valentin Charbonnier <valentinbreiz@gmail.com>
*/

using System;

namespace Aura_OS.System.Network.IPV4.TCP
{
    public class TCPPacket : IPPacket
    {

        protected UInt16 sourcePort;
        protected UInt16 destPort;
        protected UInt16 tcpLen;
        protected UInt16 tcpCRC;
        int sequencenumber; int acknowledgmentnb; int Headerlenght; int Flags; int WSValue; int Checksum; int UrgentPointer;

        internal static void TCPHandler(byte[] packetData)
        {
            
            bool SYN = (packetData[47] & (1 << 1)) != 0;
            bool ACK = (packetData[47] & (1 << 4)) != 0;
            bool FIN = (packetData[47] & (1 << 0)) != 0;
            bool PSH = (packetData[47] & (1 << 3)) != 0;
            bool RST = (packetData[47] & (1 << 2)) != 0;

            TCPPacket tcp_packet = new TCPPacket(packetData);
            Kernel.debugger.Send("Received TCP packet from " + tcp_packet.SourceIP.ToString() + ":" + tcp_packet.SourcePort.ToString());
            Kernel.debugger.Send(tcp_packet.tcpLen.ToString());


            if (CheckCRC(tcp_packet))
            {
                Kernel.debugger.Send("Checksum correct!");
            }
            else
            {
                Kernel.debugger.Send("Checksum incorrect!");
            }

            if (tcp_packet.TCP_DataLength >= 1)
            {
                Kernel.debugger.Send("Content: ");
                foreach (byte bytes in tcp_packet.TCP_Data)
                {
                    Kernel.debugger.Send("0x" + Utils.Conversion.DecToHex(bytes));
                }
            }

            ulong CID = tcp_packet.SourceIP.Hash + tcp_packet.sourcePort + tcp_packet.destPort;

            TCPConnection connection = new TCPConnection();

            if (SYN && !ACK)
            {
                Kernel.debugger.Send("New connection");

                if (TCPClient.Connections.Contains(CID))
                {

                    Kernel.debugger.Send("Connection already exists");

                    connection.dest = tcp_packet.sourceIP;
                    connection.source = tcp_packet.destIP;

                    connection.localPort = tcp_packet.destPort;
                    connection.destPort = tcp_packet.sourcePort;

                    connection.sequencenumber++;

                    connection.isClosing = true;

                    connection.sequencenumber = tcp_packet.sequencenumber++;

                    connection.Checksum = (ushort)tcp_packet.Checksum;

                    connection.Send(true);

                    TCPClient.Connections.Remove(CID);

                    Kernel.debugger.Send("Removed.");

                }
                else
                {

                    Kernel.debugger.Send("Starting response...");

                    connection.dest = tcp_packet.sourceIP;
                    connection.source = tcp_packet.destIP;

                    connection.localPort = tcp_packet.destPort;
                    connection.destPort = tcp_packet.sourcePort;

                    connection.sequencenumber++;

                    connection.acknowledgmentnb = 2380;

                    connection.WSValue = 1024;

                    connection.sequencenumber = tcp_packet.sequencenumber++;

                    connection.Checksum = 0x0000;

                    connection.Send(false);

                    Kernel.debugger.Send("Response sent!");
                }

            }
            else if (TCPClient.Connections.Contains(CID) && (ACK || FIN || PSH || RST))
            {
                Kernel.debugger.Send("Connection exists");
            }
            else if ((FIN || RST) && ACK)
            {
                Kernel.debugger.Send("Hum?");
                return;
            }

        }

        /// <summary>
        /// Work around to make VMT scanner include the initFields method
        /// </summary>
        public static void VMTInclude()
        {
            new TCPPacket();
        }

        internal TCPPacket()
            : base()
        { }

        public TCPPacket(byte[] rawData)
            : base(rawData)
        {}

        public TCPPacket(Address source, Address dest, UInt16 srcPort, UInt16 destPort, byte[] data, int sequencenumber, int acknowledgmentnb, UInt16 Headerlenght, UInt16 Flags, UInt16 WSValue, UInt16 UrgentPointer)
            : base((UInt16)(data.Length + 20), 0x06, source, dest)
        {
            Kernel.debugger.Send("Creading TCP Packet.");
            mRawData[this.dataOffset + 0] = (byte)((srcPort >> 8) & 0xFF);
            mRawData[this.dataOffset + 1] = (byte)((srcPort >> 0) & 0xFF);

            mRawData[this.dataOffset + 2] = (byte)((destPort >> 8) & 0xFF);
            mRawData[this.dataOffset + 3] = (byte)((destPort >> 0) & 0xFF);

            tcpLen = (UInt16)(data.Length + 20);

            //sequencenumber
            mRawData[this.dataOffset + 4] = (byte)((sequencenumber >> 24) & 0xFF);
            mRawData[this.dataOffset + 5] = (byte)((sequencenumber >> 16) & 0xFF);
            mRawData[this.dataOffset + 6] = (byte)((sequencenumber >> 8) & 0xFF);
            mRawData[this.dataOffset + 7] = (byte)((sequencenumber >> 0) & 0xFF);

            //Acknowledgment number
            mRawData[this.dataOffset + 8] = (byte)((acknowledgmentnb >> 24) & 0xFF);
            mRawData[this.dataOffset + 9] = (byte)((acknowledgmentnb >> 16) & 0xFF);
            mRawData[this.dataOffset + 10] = (byte)((acknowledgmentnb >> 8) & 0xFF);
            mRawData[this.dataOffset + 11] = (byte)((acknowledgmentnb >> 0) & 0xFF);

            //Header lenght
            mRawData[this.dataOffset + 12] = (byte)((Headerlenght >> 0) & 0xFF);

            //Flags
            mRawData[this.dataOffset + 13] = (byte)((Flags >> 0) & 0xFF);

            //Window size value
            mRawData[this.dataOffset + 14] = (byte)((WSValue >> 8) & 0xFF);
            mRawData[this.dataOffset + 15] = (byte)((WSValue >> 0) & 0xFF);


            byte[] header = MakeHeader(source.address, dest.address, tcpLen, srcPort, destPort, sequencenumber, acknowledgmentnb, Headerlenght, Flags, WSValue, UrgentPointer, data);
            UInt16 calculatedcrc = Check(header, 0, header.Length);

            //Checksum
            mRawData[this.dataOffset + 16] = (byte)((calculatedcrc >> 8) & 0xFF);
            mRawData[this.dataOffset + 17] = (byte)((calculatedcrc >> 0) & 0xFF);

            //Urgent Pointer
            mRawData[this.dataOffset + 18] = (byte)((UrgentPointer >> 8) & 0xFF);
            mRawData[this.dataOffset + 19] = (byte)((UrgentPointer >> 0) & 0xFF);

            for (int b = 0; b < data.Length - 20; b++)
            {
                mRawData[this.dataOffset + 20 + b] = data[b];
            }
            
            initFields();
            Kernel.debugger.Send("TCP Packet finished.");
        }

        protected override void initFields()
        {
            base.initFields();

            sourcePort = (UInt16)((mRawData[dataOffset] << 8) | mRawData[dataOffset + 1]);
            destPort = (UInt16)((mRawData[dataOffset + 2] << 8) | mRawData[dataOffset + 3]);
            sequencenumber = (mRawData[dataOffset + 4] << 24) | (mRawData[dataOffset + 5] << 16) | (mRawData[dataOffset + 6] << 8) | mRawData[dataOffset + 7];
            acknowledgmentnb = (mRawData[dataOffset + 8] << 24) | (mRawData[dataOffset + 9] << 16) | (mRawData[dataOffset + 10] << 8) | mRawData[dataOffset + 11];
            Headerlenght = mRawData[dataOffset + 12];
            Flags = mRawData[dataOffset + 13];
            WSValue = (UInt16)((mRawData[dataOffset + 14] << 8) | mRawData[dataOffset + 15]);
            Checksum = (UInt16)((mRawData[dataOffset + 16] << 8) | mRawData[dataOffset + 17]);
            UrgentPointer = (UInt16)((mRawData[dataOffset + 18] << 8) | mRawData[dataOffset + 19]);
            tcpLen = (UInt16)(mRawData.Length - 34 + 2);
        }

        internal UInt16 DestinationPort
        {
            get { return this.destPort; }
        }
        internal UInt16 SourcePort
        {
            get { return this.sourcePort; }
        }
        internal UInt16 TCP_Length
        {
            get { return this.tcpLen; }
        }
        internal UInt16 TCP_DataLength
        {
            get { return (UInt16)(this.tcpLen - 20); }
        }
        internal byte[] TCP_Data
        {
            get
            {
                byte[] data = new byte[this.tcpLen - 20];

                for (int b = 0; b < data.Length; b++)
                {
                    data[b] = this.mRawData[this.dataOffset + 20 + b];
                }

                return data;
            }
        }

        public static byte[] MakeHeader(byte[] sourceIP, byte[] destIP, UInt16 tcpLen, UInt16 sourcePort, UInt16 destPort, int sequencenumber, int acknowledgmentnb, int Headerlenght, int Flags, int WSValue, int UrgentPointer, byte[] TCP_Data)
        {

            byte[] header = new byte[30 + TCP_Data.Length];

            header[0] = sourceIP[0];
            header[1] = sourceIP[1];
            header[2] = sourceIP[2];
            header[3] = sourceIP[3];

            header[4] = destIP[0];
            header[5] = destIP[1];
            header[6] = destIP[2];
            header[7] = destIP[3];

            header[8] = 0x00;

            header[9] = 0x06;

            header[10] = (byte)((tcpLen >> 8) & 0xFF);
            header[11] = (byte)((tcpLen >> 0) & 0xFF);

            header[12] = (byte)((sourcePort >> 8) & 0xFF);
            header[13] = (byte)((sourcePort >> 0) & 0xFF);

            header[14] = (byte)((destPort >> 8) & 0xFF);
            header[15] = (byte)((destPort >> 0) & 0xFF);

            //sequencenumber
            header[16] = (byte)((sequencenumber >> 24) & 0xFF);
            header[17] = (byte)((sequencenumber >> 16) & 0xFF);
            header[18] = (byte)((sequencenumber >> 8) & 0xFF);
            header[19] = (byte)((sequencenumber >> 0) & 0xFF);

            //Acknowledgment number
            header[20] = (byte)((acknowledgmentnb >> 24) & 0xFF);
            header[21] = (byte)((acknowledgmentnb >> 16) & 0xFF);
            header[22] = (byte)((acknowledgmentnb >> 8) & 0xFF);
            header[23] = (byte)((acknowledgmentnb >> 0) & 0xFF);

            //Header lenght
            header[24] = (byte)((Headerlenght >> 0) & 0xFF);

            //Flags
            header[25] = (byte)((Flags >> 0) & 0xFF);

            //Window size value
            header[26] = (byte)((WSValue >> 8) & 0xFF);
            header[27] = (byte)((WSValue >> 0) & 0xFF);

            //Urgent Pointer
            header[28] = (byte)((UrgentPointer >> 8) & 0xFF);
            header[29] = (byte)((UrgentPointer >> 0) & 0xFF);

            for (int i = 0; i < TCP_Data.Length; i++)
            {
                header[30 + i] = TCP_Data[i];
            }

            return header;
        }

        public static bool CheckCRC(TCPPacket packet)
        {

            byte[] header = MakeHeader(packet.sourceIP.address, packet.destIP.address, packet.tcpLen, packet.sourcePort, packet.destPort, packet.sequencenumber, packet.acknowledgmentnb, packet.Headerlenght, packet.Flags, packet.WSValue, packet.UrgentPointer, packet.TCP_Data);
            UInt16 calculatedcrc = Check(header, 0, header.Length);
            Kernel.debugger.Send("tcpLen: " + packet.tcpLen.ToString());
            Kernel.debugger.Send("Header bytes begin");
            foreach (byte bytes in header)
            {
                Kernel.debugger.Send("0x" + Utils.Conversion.DecToHex(bytes));
            }
            Kernel.debugger.Send("Header bytes end");
            Kernel.debugger.Send("Calculated: 0x" + Utils.Conversion.DecToHex(calculatedcrc));
            Kernel.debugger.Send("Received:  0x" + Utils.Conversion.DecToHex(packet.Checksum));
            if (calculatedcrc == packet.Checksum)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        protected static UInt16 Check(byte[] buffer, UInt16 offset, int length)
        {
            UInt32 crc = 0;

            for (UInt16 w = offset; w < offset + length; w += 2)
            {
                crc += (UInt16)((buffer[w] << 8) | buffer[w + 1]);
            }

            crc = (~((crc & 0xFFFF) + (crc >> 16)));
            return (UInt16)crc;

        }

        public override string ToString()
        {
            return "TCP Packet Src=" + sourceIP + ":" + sourcePort + ", Dest=" + destIP + ":" + destPort + ", DataLen=" + TCP_DataLength;
        }
    }
}
