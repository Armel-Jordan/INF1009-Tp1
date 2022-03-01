using System.Collections;
using System.Threading;
using System;

namespace INF1009
{
    class Processing
    {
        private Queue packetProcess2Reseaux;
        private Queue Reseaux2PacketProcess;
        private ArrayList packets;

        public Processing(ref Queue packetProcess2Reseaux, ref Queue Reseaux2PacketProcess)
        {
            this.packetProcess2Reseaux = packetProcess2Reseaux;
            this.Reseaux2PacketProcess = Reseaux2PacketProcess;

            packets = new ArrayList();
        }

        public void CommencerProccess()
        {
            while (true)
            {
                if (Reseaux2PacketProcess.Count > 0)
                    LectureReseaux();
            }
        }

        public void LectureReseaux()
        {

                if (Reseaux2PacketProcess.Peek().GetType() == typeof(byte[]))
                {
                    byte[] packetFromNetwork = (byte[])Reseaux2PacketProcess.Dequeue();
                    BitArray type = new BitArray(new byte[] { packetFromNetwork[1] });
                    PACKET returnPacket;
                    byte[] packet2Network;
                    if (!type[0])
                    {

                        IEnumerator enumerateur = packets.GetEnumerator();
                        PACKET packet;
                        bool found = false;
                        BitArray ps = new BitArray(3);
                        BitArray pr = new BitArray(3);
                        packet.sourceAddr = 0;
                        ps.Set(0, type.Get(1));
                        ps.Set(1, type.Get(2));
                        ps.Set(2, type.Get(3));
                        pr.Set(0, type.Get(5));
                        pr.Set(1, type.Get(6));
                        pr.Set(2, type.Get(7));

                        while (enumerateur.MoveNext())
                        {
                            PACKET currentPacket = (PACKET)enumerateur.Current;
                            if (currentPacket.nombreConnection == packetFromNetwork[0])
                            {
                                packet = currentPacket;
                                found = true;
                                break;
                            }
                        }

                        if (found)
                        {
                            if (packet.sourceAddr % 15 != 0)
                            {
                                Random rnd = new Random();
                                byte defect = (byte)rnd.Next(0, 7);
                                BitArray defectBits = new BitArray(new byte[] { defect });

                            byte temp = Packet.ConvertToByte(ps);
                            BitArray current = new BitArray(new byte[] { temp });
                                if (Packet.isEqualBitArrays(defectBits, current))
                                {
                                    returnPacket = Packet.encapsulateAcknowledge(packetFromNetwork[0], Packet.ConvertToByte(pr), false);
                                    packet2Network = Packet.encapsulateBytes(returnPacket, "NACK");
                                }
                                else
                                {
                                    returnPacket = Packet.encapsulateAcknowledge(packetFromNetwork[0], Packet.ConvertToByte(pr), true);
                                    packet2Network = Packet.encapsulateBytes(returnPacket, "ACK");
                                }

                                packetProcess2Reseaux.Enqueue(packet2Network);
                            }
                        }
                        else
                        {
                            returnPacket = Packet.encapsulateAcknowledge(packetFromNetwork[0], Packet.ConvertToByte(pr), false);
                            packet2Network = Packet.encapsulateBytes(returnPacket, "release");
                            packetProcess2Reseaux.Enqueue(packet2Network);
                        }
                        
                    }
                    else if (packetFromNetwork.Length == 4)
                    {
                        if (packetFromNetwork[2] % 19 != 0)
                        {
                            if (packetFromNetwork[2] % 13 == 0)
                            {
                                returnPacket = Packet.encapsulateRelease(packetFromNetwork[0], packetFromNetwork[2], packetFromNetwork[3], true);
                                packet2Network = Packet.encapsulateBytes(returnPacket, "release");
                            }
                            else
                            {
                                returnPacket = Packet.encapsulateConnectionEstablished(packetFromNetwork[0], packetFromNetwork[2], packetFromNetwork[3]);
                                packet2Network = Packet.encapsulateBytes(returnPacket, "established");
                                packets.Add(returnPacket);
                            }

                            packetProcess2Reseaux.Enqueue(packet2Network);
                        }
                        else
                        {
                        returnPacket = Packet.encapsulateRelease(packetFromNetwork[0], packetFromNetwork[2], packetFromNetwork[3], true);
                        packet2Network = Packet.encapsulateBytes(returnPacket, "release");

                        packetProcess2Reseaux.Enqueue(packet2Network);
                        }
                    }
                    else if (packetFromNetwork.Length == 5)
                    {
                        returnPacket = Packet.encapsulateRelease(packetFromNetwork[0], packetFromNetwork[2], packetFromNetwork[3], true);
                        packet2Network = Packet.encapsulateBytes(returnPacket, "release");
                        packetProcess2Reseaux.Enqueue(packet2Network);
                        packets.Remove(returnPacket);
                    }
                }
        }

    }
}
