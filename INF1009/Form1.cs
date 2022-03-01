using System;
using System.Windows.Forms;
using System.Collections;
using System.Threading;
using System.IO;

namespace INF1009
{
    public partial class Form1 : Form
    {
        public static Form1 _UI;
        private int nbTest = 1;
        private static Queue N2TQ = new Queue();
        private static Queue N2TQS = Queue.Synchronized(N2TQ);
        private static Queue T2NQ = new Queue();
        private static Queue T2NQS = Queue.Synchronized(T2NQ);
        private static Queue N2PPQ = new Queue();
        private static Queue N2PPQS = Queue.Synchronized(N2PPQ);
        private static Queue PP2NQ = new Queue();
        private static Queue PP2NQS = Queue.Synchronized(PP2NQ);
        private Processing process = new Processing(ref PP2NQS, ref N2PPQS);
        private Network reseaux = new Network(ref T2NQS, ref N2TQS, ref PP2NQS, ref N2PPQS);
        private Transport transport = new Transport(ref T2NQS, ref N2TQS);
        private Thread ecritureReseauxThread, ecritureTransportThread, ecrireTransportThread, lectureReseauxThread, processThread;
        private const string S_lec = "s_lec.txt";
        private string d_msg;
        private string d_msgType;
        delegate void UIDisplayText(string text);

        public Form1()
        {
            InitializeComponent();
            _UI = this;
            ecritureReseauxThread = new Thread(new ThreadStart(transport.ecritureReseau));
            lectureReseauxThread = new Thread(new ThreadStart(transport.lectureReseaux));
            ecritureTransportThread = new Thread(new ThreadStart(reseaux.EcritureTransport));
            ecrireTransportThread = new Thread(new ThreadStart(reseaux.LectureTransport));
            processThread = new Thread(new ThreadStart(process.CommencerProccess));
        }


        private void OuvertureThreads()
        {
            ecritureReseauxThread = new Thread(new ThreadStart(transport.ecritureReseau));
            ecritureReseauxThread.Name = "networkWriteThread";
            ecritureReseauxThread.Start(); ;

            lectureReseauxThread = new Thread(new ThreadStart(transport.ecritureReseau));
            lectureReseauxThread.Name = "networkReadThread";
            lectureReseauxThread.Start(); ;

            ecritureTransportThread = new Thread(new ThreadStart(reseaux.EcritureTransport));
            ecritureTransportThread.Name = "transportWriteThread";
            ecritureTransportThread.Start();

            ecrireTransportThread = new Thread(new ThreadStart(reseaux.LectureTransport));
            ecrireTransportThread.Name = "transportReadThread";
            ecrireTransportThread.Start();

            processThread = new Thread(new ThreadStart(process.CommencerProccess));
            processThread.Name = "processingThread";
            processThread.Start();
        }

        private void buttonStart_Click(object sender, EventArgs e)
        {
            richTextBoxGen.Clear();
            try
            {
                reset();
                OuvertureThreads();
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message);
            }

        }

        public void reset()
        {
            if (processThread.ThreadState == ThreadState.Running ||
            ecritureReseauxThread.ThreadState == ThreadState.Running ||
            ecritureTransportThread.ThreadState == ThreadState.Running ||
            lectureReseauxThread.ThreadState == ThreadState.Running ||
            ecrireTransportThread.ThreadState == ThreadState.Running)
            {
                fermetureThreads();
            }
            T2NQS.Clear();
            N2TQS.Clear();
            PP2NQS.Clear();
            N2PPQS.Clear();
            reseaux.Commencer();
            transport.Commencer();
        }

        private void buttonReset_Click(object sender, EventArgs e)
        {
            nbTest = 1;
            transport.Stop();
            rtbL_ecr.Clear();
            rtbL_lec.Clear();
            rtbS_ecr.Clear();
            rtbS_lec.Clear();
            transport.Recommencer();
            reset();
            reseaux.resetFiles();
        }

        private void buttonQuit_Click(object sender, EventArgs e)
        {
            fermetureThreads();
            this.Close();
        }

        private void form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            fermetureThreads();
        }

        public void fermetureThreads()
        {
            processThread.Abort();
            lectureReseauxThread.Abort();
            ecritureReseauxThread.Abort();
            ecrireTransportThread.Abort();
            ecritureTransportThread.Abort();
        }

        private void buttonGenerate_Click(object sender, EventArgs e)
        {
            richTextBoxGen.Clear();
            string dest = transport.setDestAddress();
            int intDest = Int32.Parse(dest);
            string source = transport.setSourceAddress(intDest);
            d_msgType = "GenTest";

            d_msg = "N_CONNECT " + dest + " " + source + "\n" +
                    "N_DATA test no.: " + nbTest + "\n" +
                    "N_DISCONNECT " + dest + " " + source + "\n";

            richTextBoxGen.AppendText(d_msg);
            
        }

        private void buttonSend2File_Click(object sender, EventArgs e)
        {
            if (d_msgType == "GenTest")
                richTextBoxGen.AppendText("\n  Test sent to file !");
            else if (d_msgType == "TestFile")
                richTextBoxGen.AppendText("\n  Test file sent !");
            transport.Stop();
            File.AppendAllText(S_lec, d_msg + Environment.NewLine);
            nbTest++;
            transport.Recommencer();
        }

        private void buttonTest_Click(object sender, EventArgs e)
        {
            richTextBoxGen.Clear();
            richTextBoxGen.AppendText("\n  Test file loaded !");
            d_msgType = "TestFile";

            d_msg = "N_CONNECT 1 11\n" +
                    "N_DATA Start testing INF1009\n" +
                    "N_DISCONNECT 1 11\n" +
                    "N_CONNECT 47 15\n" +
                    "N_DATA negative Acknoledgment\n" + 
                    "N_DISCONNECT 47 15\n" +
                    "N_CONNECT 200 27\n" +
                    "N_DATA declined by Network\n" +
                    "N_DISCONNECT 200 27\n" +
                    "N_CONNECT 200 500\n" +
                    "N_DATA declined by Network - no route\n" +
                    "N_DISCONNECT 200 500\n" +
                    "N_CONNECT 9 19\n" +
                    "N_DATA no awnser\n" +
                    "N_DISCONNECT 9 19\n" +
                    "N_CONNECT 12 13\n" +
                    "N_DATA multiple of 13 - connection declined by destination\n" +
                    "N_DISCONNECT 12 13\n" +
                    "N_CONNECT 58 217\n" +
                    "N_DATA This is the last test, this program was written for the recognition of the achievements of Thibaud Mouffron Ismail Ouichri for the course INF1009 ...\n" +
                    "N_DISCONNECT 58 217\n";
        }

        public void write2L_lec(string text)
        {
            string txt = text + Environment.NewLine;

            if (this.rtbL_lec.InvokeRequired)
            {
                UIDisplayText displayText = new UIDisplayText(write2L_lec);
                this.Invoke(displayText, new object[] { text });
            }
            else
            {
                rtbL_lec.AppendText(txt);
            }
        }

        public void write2S_lec(string text)
        {
            string txt = text + Environment.NewLine;
            if (this.rtbS_lec.InvokeRequired)
            {
                UIDisplayText displayText = new UIDisplayText(write2S_lec);
                this.Invoke(displayText, new object[] { text });
            }
            else
            {
                rtbS_lec.AppendText(txt);
            }
        }


        public void write2S_ecr(string text)
        {
            string txt = text + Environment.NewLine;
            if (this.rtbL_lec.InvokeRequired)
            {
                UIDisplayText displayText = new UIDisplayText(write2S_ecr);
                this.Invoke(displayText, new object[] { text });
            }
            else
            {
                rtbS_ecr.AppendText(txt);
            }
        }

        public void write2L_ecr(string text)
        {
            string txt = text + Environment.NewLine;
            if (this.rtbL_lec.InvokeRequired)
            {
                UIDisplayText displayText = new UIDisplayText(write2L_ecr);
                this.Invoke(displayText, new object[] { text });
            }
            else
            {
                rtbL_ecr.AppendText(txt);
            }
        }
    }
}
