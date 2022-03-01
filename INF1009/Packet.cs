using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace INF1009
{
    public struct PACKET
    {
        public byte nombreConnection;
        public byte packetType;
        public byte sourceAddr;
        public byte destAddr;
        public byte cible;
        public byte[] dataArray;
    }

    public class Packet
    {
        public static PACKET encapsulateRequest(byte nombreConnection, byte sourceAddr, byte destAddr)
        {
            PACKET currentPacket = new PACKET();

            currentPacket.nombreConnection = nombreConnection;

            bool[] currentType = new bool[8] { true, true, false, true, false, false, false, false };
            BitArray type = new BitArray(currentType);
            byte[] temp = new byte[1];
            type.CopyTo(temp, 0);
            currentPacket.packetType = temp[0];

            currentPacket.sourceAddr = sourceAddr;
            currentPacket.destAddr = destAddr;

            return currentPacket;
        }

        public static PACKET encapsulateConnectionEstablished(byte nombreConnection, byte sourceAddr, byte destAddr)
        {
            PACKET currentPacket = new PACKET();

            currentPacket.nombreConnection = nombreConnection;

            bool[] currentType = new bool[8] { true, true, true, true, false, false, false, false };
            BitArray type = new BitArray(currentType);
            byte[] temp = new byte[1];
            type.CopyTo(temp, 0);
            currentPacket.packetType = temp[0];

            currentPacket.sourceAddr = sourceAddr;
            currentPacket.destAddr = destAddr;

            return currentPacket;
        }

        public static PACKET encapsulateRelease(byte nombreConnection, byte sourceAddr, byte destAddr, bool cible)
        {
            PACKET currentPacket = new PACKET();

            currentPacket.nombreConnection = nombreConnection;

            bool[] currentType = new bool[8] { true, true, false, false, true, false, false, false };
            BitArray type = new BitArray(currentType);
            byte[] temp = new byte[1];
            type.CopyTo(temp, 0);
            currentPacket.packetType = temp[0];

            currentPacket.sourceAddr = sourceAddr;
            currentPacket.destAddr = destAddr;

            BitArray currentcible = new BitArray(8);
            if (cible)
            {
                currentcible.SetAll(false);
                currentcible.Set(0, true);
            }
            else
            {
                currentcible.SetAll(false);
                currentcible.Set(1, true);
            }
            byte[] result = new byte[1];
            currentcible.CopyTo(result, 0);
            currentPacket.cible = result[0];

            return currentPacket;
        }

        public static PACKET encapsulateAcknowledge(byte nombreConnection, byte prochain, bool acquitte)
        {
            PACKET currentPacket = new PACKET();

            currentPacket.nombreConnection = nombreConnection;

            BitArray temp = new BitArray(new byte[] { prochain });
            BitArray next = new BitArray(8);
            next.SetAll(false);
            next.Set(5, temp.Get(0));
            next.Set(6, temp.Get(1));
            next.Set(7, temp.Get(2));
            next.Set(0, true);
            if (!acquitte)
            {
                next.Set(3, true);
            }
            byte[] result = new byte[1];
            next.CopyTo(result, 0);
            currentPacket.cible = result[0];

            return currentPacket;
        }

        public static PACKET encapsulateData(byte nombreConnection, byte pr, byte ps, bool m, byte[] dataArray)
        {
            PACKET currentPacket = new PACKET();

            BitArray type = new BitArray(8);
            type.SetAll(false);

            BitArray temp = new BitArray(new byte[] { pr });
            type.Set(5, temp.Get(0));
            type.Set(6, temp.Get(1));
            type.Set(7, temp.Get(2));
            temp = new BitArray(new byte[] { ps });
            type.Set(1, temp.Get(0));
            type.Set(2, temp.Get(1));
            type.Set(3, temp.Get(2));

            type.Set(4, m);

            byte[] result = new byte[1];
            type.CopyTo(result, 0);

            currentPacket.nombreConnection = nombreConnection;
            currentPacket.packetType = result[0];
            currentPacket.dataArray = dataArray;

            return currentPacket;
        }

        public static byte[] encapsulateDataBytes(PACKET currentPacket)
        {
            byte[] send = new byte[2 + currentPacket.dataArray.Length];
            send[0] = currentPacket.nombreConnection;
            send[1] = currentPacket.packetType;
            Buffer.BlockCopy(currentPacket.dataArray, 0, send, 2, currentPacket.dataArray.Length);

            return send;
        }

        private static byte[] GetBytes(string str)
        {
            byte[] bytes = new byte[str.Length * sizeof(char)];
            System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }

        private static string GetString(byte[] bytes)
        {
            char[] chars = new char[bytes.Length / sizeof(char)];
            System.Buffer.BlockCopy(bytes, 0, chars, 0, bytes.Length);
            return new string(chars);
        }

        public static PACKET[] encapsulateFullData(string lesdonnees, byte nombreConnection, byte pr)
        {
            PACKET[] currentPackets;
            byte[] convertedData = GetBytes(lesdonnees);
            int nbBytes = convertedData.Length;
            int nbpackets = (nbBytes / 128) + 1;
            currentPackets = new PACKET[nbpackets];

            byte ps = 0;
            for (int i = 0; i < nbpackets; i++)
            {
                int size = 128;
                if (i * 128 + size > nbBytes)
                    size = nbBytes - i * 128;
                byte[] temp = new byte[size];
                int k = 0;
                for (int j = i * 128; j < i * 128 + size; j++)
                {
                    temp[k] = convertedData[j];
                    k++;
                }

                bool m = true;
                if (size < 128 && i * 128 + size + 1 > nbBytes)
                    m = false;

                int l = i % 8;
                ps = (byte)l;
                currentPackets[i] = encapsulateData(nombreConnection, pr, ps, m, temp);
            }

            return currentPackets;
        }

        public static byte[] encapsulateBytes(PACKET currentPacket, string type)
        {
            byte[] sending;
            switch(type)
            {
                case "request":
                case "established":
                    sending = new byte[4];
                    sending[0] = currentPacket.nombreConnection;
                    sending[1] = currentPacket.packetType;
                    sending[2] = currentPacket.sourceAddr;
                    sending[3] = currentPacket.destAddr;
                    break;
                case "release":
                    sending = new byte[5];
                    sending[0] = currentPacket.nombreConnection;
                    sending[1] = currentPacket.packetType;
                    sending[2] = currentPacket.sourceAddr;
                    sending[3] = currentPacket.destAddr;
                    sending[4] = currentPacket.cible;
                    break;
                case "acknowledge":
                    sending = new byte[2];
                    sending[0] = currentPacket.nombreConnection;
                    sending[1] = currentPacket.cible;
                    break;
                case "NACK":
                    sending = new byte[2];
                    sending[0] = currentPacket.nombreConnection;
                    sending[1] = currentPacket.cible ;
                    break;
                default:
                    sending = new byte[0];
                    break;
                
            }
            return sending;
        }


        public static PACKET decapBytes(byte[] received)
        {

            PACKET currentPacket = new PACKET();
            if (received.Length == 2)
            {
                currentPacket.nombreConnection = received[0];
                currentPacket.cible = received[1];
                currentPacket.packetType = 255;
            }
            else if (received.Length == 4)
            {
                currentPacket.nombreConnection = received[0];
                currentPacket.packetType = received[1];
                currentPacket.sourceAddr = received[2];
                currentPacket.destAddr = received[3];
            }
            else if (received.Length == 5)
            {
                currentPacket.nombreConnection = received[0];
                currentPacket.packetType = received[1];
                currentPacket.sourceAddr = received[2];
                currentPacket.destAddr = received[3];
                currentPacket.cible = received[4];
            }
            else if (received.Length == 0);
            else
            {
                currentPacket.nombreConnection = received[0];
                currentPacket.packetType = received[1];
                byte[] temp = new byte[received.Length - 2];
                Buffer.BlockCopy(received, 2, temp, 0, received.Length - 2);

                currentPacket.dataArray = temp;
            }
              
            return currentPacket;
        }

        public static Npdu decapPacket(PACKET currentPacket)
        {
            Npdu _4Transport = new Npdu();

            BitArray type = new BitArray(new byte[] { currentPacket.packetType });
            BitArray connectionInit, connectionEstablished, releasing;
            bool[] boolArray = new bool[8] { true, true, false, true, false, false, false, false };
            connectionInit = new BitArray(boolArray);
            boolArray = new bool[8] { true, true, true, true, false, false, false, false };
            connectionEstablished = new BitArray(boolArray);
            boolArray = new bool[8] { true, true, false, false, true, false, false, false };
            releasing = new BitArray(boolArray);


            if (currentPacket.packetType == 255)
                return decapAcknowledge(currentPacket);
            else if (isEqualBitArrays(type, releasing))
                return decapRelease(currentPacket);
            else if (isEqualBitArrays(type, connectionEstablished))
                return decapConnectionEstablished(currentPacket);
            else if (isEqualBitArrays(type, connectionInit))
                return decapRequest(currentPacket);
            else if (!type[0])
                return decapData(currentPacket);


            _4Transport.type = "WrongPacketFormat";
            return _4Transport;
        }

        public static bool isEqualBitArrays(BitArray a, BitArray b)
        {
            if (a.Length != b.Length)
                return false;
            for (int i = 0; i < a.Length; i++)
                if (a[i] != b[i])
                    return false;
            return true;
        }

        private static Npdu decapAcknowledge(PACKET currentPacket)
        {
            Npdu currentNpdu = new Npdu();
            BitArray type = new BitArray(new byte[] { currentPacket.cible });

            if (type[0])
            {
                if (type[3])
                    currentNpdu.type = "NACK";
                else
                    currentNpdu.type = "ACK";
                BitArray pr = new BitArray(3);
                pr.Set(0, type.Get(5));
                pr.Set(1, type.Get(6));
                pr.Set(2, type.Get(7));
                int[] next = new int[1];
                pr.CopyTo(next, 0);
                currentNpdu.pr = next[0];
                currentNpdu.connection = currentPacket.nombreConnection.ToString();
            }
            else
                currentNpdu.type = "WrongPacketFormat";


            return currentNpdu;
        }

        private static Npdu decapData(PACKET currentPacket)
        {
            Npdu currentNpdu = new Npdu();

            currentNpdu.type = "N_DATA.ind";
            currentNpdu.connection = currentPacket.nombreConnection.ToString();

            BitArray type = new BitArray(new byte[] { currentPacket.packetType });
            BitArray prps = new BitArray(3);
            prps.Set(0, type.Get(5));
            prps.Set(1, type.Get(6));
            prps.Set(2, type.Get(7));
            int[] num = new int[1];
            prps.CopyTo(num, 0);
            currentNpdu.pr = num[0];
            prps.Set(0, type.Get(1));
            prps.Set(1, type.Get(2));
            prps.Set(2, type.Get(3));
            prps.CopyTo(num, 0);
            currentNpdu.ps = num[0];

            if (currentPacket.dataArray != null)
                currentNpdu.data = GetString(currentPacket.dataArray);

            currentNpdu.flag = type[4];

            return currentNpdu;
        }

        private static Npdu decapRelease(PACKET currentPacket)
        {
            Npdu currentNpdu = new Npdu();

            currentNpdu.type = "N_DISCONNECT.ind";
            currentNpdu.sourceAddr = currentPacket.sourceAddr.ToString();
            currentNpdu.destAddr = currentPacket.destAddr.ToString();
            currentNpdu.connection = currentPacket.nombreConnection.ToString();

            BitArray cible = new BitArray(new byte[] { currentPacket.cible });
            if (cible[0])
                currentNpdu.cible = "Closed by Client";
            else if (cible[1])
                currentNpdu.cible = "Closed by Provider";
            else
                currentNpdu.cible = "Unknown";

            return currentNpdu;
        }

        private static Npdu decapConnectionEstablished(PACKET currentPacket)
        {
            Npdu currentNpdu = new Npdu();

            currentNpdu.type = "N_CONNECT.ind";
            currentNpdu.sourceAddr = currentPacket.sourceAddr.ToString();
            currentNpdu.destAddr = currentPacket.destAddr.ToString();
            currentNpdu.connection = currentPacket.nombreConnection.ToString();

            return currentNpdu;
        }

        private static Npdu decapRequest(PACKET currentPacket)
        {
            Npdu currentNpdu = new Npdu();

            currentNpdu.type = "N_CONNECT.ind";
            currentNpdu.sourceAddr = currentPacket.sourceAddr.ToString();
            currentNpdu.destAddr = currentPacket.destAddr.ToString();
            currentNpdu.connection = currentPacket.nombreConnection.ToString();

            return currentNpdu;
        }

        public static byte ConvertToByte(BitArray bits)
        {
            if (bits.Count == 0)
            {
                throw new ArgumentException("bits");
            }
            byte[] bytes = new byte[1];
            bits.CopyTo(bytes, 0);
            return bytes[0];
        }
    }
}