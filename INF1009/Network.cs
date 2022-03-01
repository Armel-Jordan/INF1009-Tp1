using System.Threading;
using System.Collections;
using System.IO;
using System;
using System.Timers;

namespace INF1009
{
    class Network
    {
        private Queue transport2Reseaux;
        private Queue Reseaux2Transport;
        private Queue network2PacketProcess;
        private Queue packetProcess2Reseaux;
        private const string L_lec = "l_lec.txt";
        private const string L_ecr = "l_ecr.txt";
        private FileStream fichierTransport;
        private FileStream fichier2Transport;
        private StreamWriter ecritureTransport;
        private StreamWriter ecriture2Transport;
        private int envoieNombre;
        byte[] sourceAddr;
        byte[] destAddr;
        byte[] SortiNo;
        byte pr;
        bool expire, rejette, accepte, deconnecte, connecte;
        System.Timers.Timer timer;
        string donneeRecu;

        public Network(ref Queue transport2Reseaux, ref Queue Reseaux2Transport, ref Queue packetProcess2Reseaux, ref Queue Reseaux2PacketProcess)
        {
            this.transport2Reseaux = transport2Reseaux;
            this.Reseaux2Transport = Reseaux2Transport;
            this.network2PacketProcess = Reseaux2PacketProcess;
            this.packetProcess2Reseaux = packetProcess2Reseaux;

            fichierTransport = new FileStream(L_ecr, FileMode.OpenOrCreate, FileAccess.Write);
            ecritureTransport = new StreamWriter(fichierTransport);
            fichier2Transport = new FileStream(L_lec, FileMode.OpenOrCreate, FileAccess.Write);
            ecriture2Transport = new StreamWriter(fichier2Transport);
            Commencer();
        }

        public void resetFiles()
        {
            fichierTransport.Position = 0;
            fichier2Transport.Position = 0;
        }

        public void Commencer()
        {
            Random rnd = new Random();

            ecritureTransport.Flush();
            ecriture2Transport.Flush();

            sourceAddr = new byte[1];
            destAddr = new byte[1];
            SortiNo = new byte[1];
            NouvelleConnection(SortiNo);

            pr = 0;
            donneeRecu = "";
            deconnecte = false;
            connecte = false;

            timer = new System.Timers.Timer();
            timer.Interval = 10000;
            timer.Elapsed += onTimeEvent;
            timer.AutoReset = true;
        }

        public void EcritureTransport()
        {
            while (true)
            {
                try
                {
                    if (!deconnecte)
                    {
                        if (packetProcess2Reseaux.Count > 0)
                        {
                            if (packetProcess2Reseaux.Peek().GetType() == typeof(byte[]))
                            {
                                string msg = "";
                                byte[] received = (byte[])packetProcess2Reseaux.Dequeue();
                                PACKET receivedPacket = Packet.decapBytes(received);
                                Npdu _4Transport = Packet.decapPacket(receivedPacket);

                                switch (_4Transport.type)
                                {
                                        case "WrongPacketFormat":
                                        msg = "Wrong Packet Format";
                                        break;
                                    case "release":
                                        msg = "released packet: " + receivedPacket.packetType.ToString();
                                        rejette = true;
                                        break;
                                    
                                    case "N_DISCONNECT.ind":
                                        msg = "N_DISCONNECT " + _4Transport.cible;
                                        Reseaux2Transport.Enqueue(_4Transport);
                                        accepte = true;
                                        deconnecte = true;
                                        break;
                                    case "N_CONNECT.ind":
                                        msg = "N_CONNECT  dest Address :" + _4Transport.destAddr + " source Address: " + _4Transport.sourceAddr;
                                        Reseaux2Transport.Enqueue(_4Transport);
                                        accepte = true;
                                        break;
                                    case "N_DATA.ind":
                                        msg = "N_DATA  transferring network data";
                                        donneeRecu += _4Transport.data;
                                        if (!_4Transport.flag)
                                        {
                                        _4Transport.data = donneeRecu;
                                            Reseaux2Transport.Enqueue(_4Transport);
                                        }
                                        break;
                                default:
                                break;
                                }


                                ecriture2Transport.WriteLine(msg);
                                Form1._UI.write2L_ecr(msg);
                            }
                        }
                    }
                }
                catch (ThreadAbortException)
                {

                }
            }
        }

        public void LectureTransport()
        {
            while (true)
            {
                try
                {
                    if (!deconnecte)
                    {
                        if (transport2Reseaux.Count > 0)
                        {
                            if (transport2Reseaux.Peek().GetType() == typeof(Npdu))
                            {
                                Npdu transportNpdu = (Npdu)transport2Reseaux.Dequeue();
                                Npdu npdu2Transport;
                                PACKET packet4Processing;
                                string msg;

                                if (transportNpdu.type != "N_CONNECT.req" && !connecte) { }
                                else
                                {
                                    if (transportNpdu.type == "N_CONNECT.req")
                                        
                                    {
                                        int intSource = int.Parse(transportNpdu.sourceAddr);
                                        int intDest = int.Parse(transportNpdu.destAddr);
                                        sourceAddr[0] = (byte)intSource;
                                        destAddr[0] = (byte)intDest;

                                        if (intSource > 249 || intDest > 249)
                                        {
                                            transportNpdu.routeAddr = " Error, not found !";
                                        }
                                        else
                                        {
                                            if (intSource >= 0 && intSource <= 99)
                                            {
                                                if (intDest >= 0 && intDest <= 99)
                                                    transportNpdu.routeAddr = "" + intDest;
                                                else if (intDest >= 100 && intDest <= 199)
                                                    transportNpdu.routeAddr = "" + 255;
                                                else if (intDest >= 200 && intDest <= 249)
                                                    transportNpdu.routeAddr = "" + 254;
                                            }
                                            else if (intSource >= 100 && intSource <= 199)
                                            {
                                                if (intDest >= 0 && intDest <= 99)
                                                    transportNpdu.routeAddr = "" + 250;
                                                else if (intDest >= 100 && intDest <= 199)
                                                    transportNpdu.routeAddr = "" + intDest;
                                                else if (intDest >= 200 && intDest <= 249)
                                                    transportNpdu.routeAddr = "" + 253;
                                            }
                                            else if (intSource >= 200 && intSource <= 249)
                                            {
                                                if (intDest >= 0 && intDest <= 99)
                                                    transportNpdu.routeAddr = "" + 251;
                                                else if (intDest >= 100 && intDest <= 199)
                                                    transportNpdu.routeAddr = "" + 252;
                                                else if (intDest >= 200 && intDest <= 249)
                                                    transportNpdu.routeAddr = "" + intDest;
                                            }

                                        }

                                        msg = "N_CONNECT " + transportNpdu.destAddr + " " + transportNpdu.sourceAddr + "  route:" + transportNpdu.routeAddr;
                                        ecritureTransport.WriteLine(msg);
                                        Form1._UI.write2L_lec(msg);

                                        if (sourceAddr[0] % 27 == 0 || int.Parse(transportNpdu.sourceAddr) > 249 || int.Parse(transportNpdu.destAddr) > 249)
                                        {
                                            deconnecte = true;
                                            connecte = false;

                                            npdu2Transport = new Npdu();
                                            npdu2Transport.type = "N_DISCONNECT.ind";
                                            npdu2Transport.cible = "00000010";
                                            npdu2Transport.connection = "255";
                                            Reseaux2Transport.Enqueue(npdu2Transport);
                                        }
                                        else
                                        {
                                            connecte = true;

                                            npdu2Transport = new Npdu();
                                            npdu2Transport.type = "N_CONNECT.conf";
                                            npdu2Transport.sourceAddr = sourceAddr[0].ToString();
                                            npdu2Transport.destAddr = destAddr[0].ToString();
                                            npdu2Transport.connection = SortiNo[0].ToString();
                                            Reseaux2Transport.Enqueue(npdu2Transport);

                                            packet4Processing = Packet.encapsulateRequest(SortiNo[0], sourceAddr[0], destAddr[0]);                                          
                                            byte[] sending = Packet.encapsulateBytes(packet4Processing, "request");

                                            envoieNombre = 0;
                                            rejette = false;
                                            expire = true;
                                            accepte = false;
                                            timer.Start();
                                            while (!accepte && envoieNombre < 2)
                                            {
                                                if (expire || rejette)
                                                {
                                                    network2PacketProcess.Enqueue(sending);
                                                    expire = false;
                                                    rejette = false;
                                                    envoieNombre++;
                                                }
                                            }
                                            timer.Stop();
                                        }
                                    }
                                    else if (transportNpdu.type == "N_DATA.req")
                                    {
                                        msg = "N_DATA " + transportNpdu.data;
                                        ecritureTransport.WriteLine(msg);
                                        Form1._UI.write2L_lec(msg);

                                        PACKET[] packets4Processing = Packet.encapsulateFullData(transportNpdu.data, SortiNo[0], pr);
                                        foreach (PACKET packet in packets4Processing)
                                        {
                                            byte[] sending = Packet.encapsulateDataBytes(packet);

                                            envoieNombre = 0;
                                            rejette = false;
                                            expire = true;
                                            accepte = false;
                                            timer.Start();
                                            while (!accepte && envoieNombre < 2)
                                            {
                                                if (expire || rejette)
                                                {
                                                    network2PacketProcess.Enqueue(sending);
                                                    expire = false;
                                                    rejette = false;
                                                    envoieNombre++;
                                                }
                                            }
                                            timer.Stop();
                                        }
                                    }
                                    else if (transportNpdu.type == "N_DISCONNECT.req")
                                    {
                                        msg = "N_DISCONNECT " + transportNpdu.routeAddr;
                                        ecritureTransport.WriteLine(msg);
                                        Form1._UI.write2L_lec(msg);

                                        npdu2Transport = new Npdu();
                                        npdu2Transport.type = "N_DISCONNECT.ind";

                                        packet4Processing = Packet.encapsulateRelease(SortiNo[0], sourceAddr[0], destAddr[0], true);
                                        string packetType = "release";
                                        byte[] sending = Packet.encapsulateBytes(packet4Processing, packetType);

                                        envoieNombre = 0;
                                        rejette = false;
                                        expire = true;
                                        accepte = false;
                                        timer.Start();
                                        while (!accepte && envoieNombre < 2)
                                        {
                                            if (expire || rejette)
                                            {
                                                network2PacketProcess.Enqueue(sending);
                                                expire = false;
                                                rejette = false;
                                                envoieNombre++;
                                            }
                                        }
                                        timer.Stop();
                                    }
                                }
                            }
                        }
                    }
                }
                catch (ThreadAbortException) { }
            }
        }

        private void onTimeEvent(object sender, ElapsedEventArgs e)
        {
            expire = true;
        }

        private void NouvelleConnection(byte[] NombreConnection)
        {
            Random rnd = new Random();

            rnd.NextBytes(NombreConnection);
            NombreConnection[0] %= 8;
        }
    }
}
