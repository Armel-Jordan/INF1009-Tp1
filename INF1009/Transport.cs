using System.IO;
using System.Threading;
using System.Collections;
using System;
using System.Security.AccessControl;

namespace INF1009
{
    /**
    * Structure public Npdu qui represente les pacquets
    */
    public struct Npdu
    {
        public string type;
		public string destAddr;
        public string sourceAddr;
        public string routeAddr;
        public string data;
        public string cible;
        public string connection;
        public int ps, pr;
        public bool flag;
    }

    /**
    * Classe Transport qui représente la couche transport des systèmes A et B
    */
    class Transport
    {
        private const string S_lec = "s_lec.txt";
        private const string S_ecr = "s_ecr.txt";
        private StreamReader lecture;
        private StreamWriter ecriture;
        private FileStream fichierEntree;
        private FileStream fichierSorti;
        private Queue transport2Reseaux;
        private Queue Reseaux2Transport;
        string msg;
        bool deconnecte;
        bool fin;
        ArrayList connecte;

        /**
        * Constructeur prenant en parametre les files (FIFO) transport2Network et network2Transport
        */
        public Transport(ref Queue transport2Reseaux, ref Queue Reseaux2Transport)
        {

            this.transport2Reseaux = transport2Reseaux;
            this.Reseaux2Transport = Reseaux2Transport;

            fichierEntree = new FileStream(S_lec, FileMode.OpenOrCreate, FileAccess.Read);
            lecture = new StreamReader(fichierEntree);
            fichierSorti = new FileStream(S_ecr, FileMode.OpenOrCreate, FileAccess.Write);
            ecriture = new StreamWriter(fichierSorti);

            connecte = new ArrayList();
            Commencer();
        }

 
        /**
        * Methode Start appelé au demarrage
        */
        public void Commencer()
        {
            ecriture.Flush();
            deconnecte = false;
            msg = "";
        }

        /**
        * Methode Stop appelé au changement du fichier source
        */
        public void Stop()
        {
            fin = true;
            fichierEntree.Close();
            fichierSorti.Close();
            lecture.Close();
            System.IO.File.WriteAllText(S_lec, string.Empty);
        }

        /**
        * Methode Restart appelé au changement du fichier source
        */
        public void Recommencer()
        {
            fin = false;
            fichierEntree = new FileStream(S_lec, FileMode.OpenOrCreate, FileAccess.Read);
            lecture = new StreamReader(fichierEntree);
            fichierSorti = new FileStream(S_ecr, FileMode.OpenOrCreate, FileAccess.Write);
            ecriture = new StreamWriter(fichierSorti);
            resetFiles();
            Commencer();
        }

        /**
        * Methode resetFiles qui remets différents paramètres a leurs 
        * valeurs initiales pour la lecture et l'écriture des fichiers.
        */
        public void resetFiles()
        {

            fichierEntree.Position = 0;
            fichierSorti.Position = 0;
            connecte.Clear();
            lecture.DiscardBufferedData();
        }

        /**
        * Methode getRandomInt qui retourne une valeur aleatoire entre 
        * min et max fournis en parametres.
        */
        private int getRandomInt(int min, int max)
        {
            Random rnd = new Random();
            return rnd.Next(min, max);
        }

        /**
        * Methode setDestAddress qui retourne une string
        * contenant une adresse aleatoire entre 0 et 249.
        */
        public string setDestAddress()
        {
            string result = null;

            int dest = getRandomInt(0, 249);
            result = "" + dest;

            return result;
        }

        /**
        * Methode setSourceAddress qui retourne une string contenant
        * une adresse aleatoire source entre 0 et 249 et
        * differente de intDest fourni en parametre.
        */
        public string setSourceAddress(int intDest)
        {
            string result = null;
            int source;

            do
            {
                source = getRandomInt(0, 249);
            } while(intDest == source);

            result = "" + source;

            return result;
        }


        /**
        * Methode networkWrite (ecrire_vers_reseau) qui lit le fichier s_lec.txt 
        * jusqu'a une ligne qui contiens la String N_DISCONNECT ou jusqu'a ce que 
        * la fin du fichier soit atteint.
        * 
        * Si la ligne lu contiens N_CONNECT, N_DATA ou N_DISCONNECT un pacquet (Npdu)
        * est créer et ajouté a la file transport2Network
        */
        public void ecritureReseau()
        {
            string ligneLu;
            Npdu networkNNpdu;
            string[] parametre;
            bool valid;
            fin = false;

            while (!fin && !deconnecte)
            {
                if ((ligneLu = lecture.ReadLine()) != null)
                {
                    networkNNpdu = new Npdu();
                    try
                    {
                        valid = false;
                        parametre = ligneLu.Split(' ');
                        Form1._UI.write2S_lec(ligneLu);

                        if (parametre[0] == "N_CONNECT")
                        {
                            networkNNpdu.type = "N_CONNECT.req";
                            networkNNpdu.destAddr = parametre[1];
                            networkNNpdu.sourceAddr = parametre[2];
                            networkNNpdu.routeAddr = "";
                            valid = true;
                        }
                        else if (parametre[0] == "N_DATA")
                        {
                            networkNNpdu.type = "N_DATA.req";
                            for (int i = 1; i < parametre.Length; i++)
                                networkNNpdu.data += parametre[i] + " ";
                            valid = true;
                        }
                        else if (parametre[0] == "N_DISCONNECT")
                        {
                            networkNNpdu.type = "N_DISCONNECT.req";
                            networkNNpdu.routeAddr = parametre[1];
                            valid = true;
                            deconnecte = true;
                        }
                        if (valid)
                            transport2Reseaux.Enqueue(networkNNpdu);
                    }
                    catch (ThreadAbortException)
                    {

                    }
                }
                else
                {
                    fin = true;
                }
            }
        }


        /**
        * click sur le bouton start
        * Methode networkRead (lire_de_reseau) qui verifie si la file network2Transport
        * contiens un Npdu a lire, si tel est le cas le type du Npdu est verifié et un
        * traitement est effectué selon le type trouvé. Ecriture dan sle fichier S.ecr.
        */
        public void lectureReseaux()
        {
            while (!fin)
            {
                try
                {
                    // si il y a quelque chose
                    if (Reseaux2Transport.Count > 0)
                    {
                        //  si c'est un pacquet
                        if (Reseaux2Transport.Peek().GetType() == typeof(Npdu))
                        {
                            // oon "extrait" le paquet afin de pouvoir le lire 
                            Npdu Npdu4Network = (Npdu)Reseaux2Transport.Dequeue();

                            // on choisit l'action a effectué en fonction du type
                            if(Npdu4Network.type == "N_CONNECT.ind")
                            {
                                // si la connexion est établie
                                if (connecte.Contains(Npdu4Network.connection))
                                { 
                                msg = "connection: " + Npdu4Network.connection + " dest Address: " + Npdu4Network.destAddr + "  source Address: " + Npdu4Network.sourceAddr;
                                    ecriture.WriteLine(msg);
                                Form1._UI.write2S_ecr(msg);
                                }
                            }
                            // si la connection est établie avec "l'utilisateur b"
                            else if (Npdu4Network.type == "N_CONNECT.conf")
                            {
                                msg =  "connection: " + Npdu4Network.connection + " Connection established ";
                                ecriture.WriteLine(msg);
                                Form1._UI.write2S_ecr(msg);
                                connecte.Add(Npdu4Network.connection);
                            }
                            // indication de transfert de donnees
                            else if (Npdu4Network.type == "N_DATA.ind")
                            {
                                if (connecte.Contains(Npdu4Network.connection))
                                {
                                    msg = Npdu4Network.data;
                                    ecriture.WriteLine(msg);
                                    Form1._UI.write2S_ecr(msg);
                                }
                            }
                            // fin de connexion
                            else if (Npdu4Network.type == "N_DISCONNECT.ind")
                            {
                                if (connecte.Contains(Npdu4Network.connection))
                                {
                                    connecte.Remove(Npdu4Network.connection);
                                    msg = "connection: " + Npdu4Network.connection + " " + Npdu4Network.routeAddr + " disconnected " + Npdu4Network.cible;
                                    ecriture.WriteLine(msg);
                                    Form1._UI.write2S_ecr(msg);
                                    Form1._UI.closeThreads();

                                }
                                // réponse du négative du fournisseur
                                else if(Npdu4Network.connection.Equals("255"))
                                {
                                    Form1._UI.write2S_ecr("connection: declined by Network! ");
                                    Form1._UI.closeThreads();
                                }                               
                            }
                        }
                    }
                }
                catch (ThreadAbortException)
                {

                }
            }
        }
    }
}
