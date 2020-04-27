using Bunifu.Framework.UI;
using MetroFramework.Controls;
using Microsoft.VisualBasic.Devices;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;

namespace NP_Hangman
{
    public partial class Hangman : MetroFramework.Forms.MetroForm
    {
        string word = "";
        List<BunifuThinButton2> btns = new List<BunifuThinButton2>();
        int maxWrongGuess = 5;
        string rightGuess = "";
        List<TcpClient> clients = new List<TcpClient>(); //ToolBar store connected clients
        TcpListener server;
        TcpClient activeClient;
        bool isPlaying = false;
        bool isGuessing = false;

        public Hangman()
        {
            InitializeComponent();
        }


        private void playBTN_Click(object sender, EventArgs e)
        {
            messageCmnt.Text = "";
            if (nameTxt.Text.Trim(' ') == "")
            {
                nameTxt.LineIdleColor = Color.Red;
            }
            if (wordTxt.Text.Trim(' ') == "")
            {
               wordTxt.LineIdleColor = Color.Red;
            }
            else if(hintTxt.Text.Trim(' ') == "")
            {
                hintTxt.LineIdleColor = Color.Red;
            }
            else
            {
                isGuessing = false;
                if (isHost)
                {
                    //send request to the active client
                    nameTxt.LineIdleColor = Color.FromArgb(64, 64, 64);
                    wordTxt.LineIdleColor = Color.FromArgb(0, 170, 173);
                    hintTxt.LineIdleColor = Color.FromArgb(0, 170, 173);
                    int lenTxt = Convert.ToInt32(wordTxt.Text.Length);
                    lenLbl.Text = wordTxt.Text.Length.ToString();
                    word = wordTxt.Text.ToUpper();
                    wordInitiator(word.Length);
                    guessWrtxt.ResetText();
                    sendToClient("play," + word ,activeClient);

                }


                //code here if client want to playe as startrer
                //isPlaying = true;
                //KeyPreview = true;
                //nameTxt.LineIdleColor = Color.FromArgb(64, 64, 64);
                //wordTxt.LineIdleColor = Color.FromArgb(0, 170, 173);
                //hintTxt.LineIdleColor = Color.FromArgb(0, 170, 173);
                //int lenTxt = Convert.ToInt32(wordTxt.Text.Length);
                //maxWrongGuess = lenTxt / 2;
                //lenLbl.Text = wordTxt.Text.Length.ToString();
                //word = wordTxt.Text.ToUpper();
                //guessWrtxt.ResetText();
            }
        }








        Dictionary<string, TcpClient> lstClients = new Dictionary<string, TcpClient>();
        byte[] b = new byte[1024];
        //private Hangman hangman;//

        private void ClientConnect(IAsyncResult ar)
        {
            TcpListener listener = (TcpListener)ar.AsyncState;
            TcpClient client = listener.EndAcceptTcpClient(ar);
            clients.Add(client);

            //generate checkboxes for new clients
            MetroRadioButton chkBox = checkBoxMaker(clients.Count.ToString());

            activeClient = client;
            NetworkStream ns = client.GetStream();
            object[] a = new object[2];
            a[0] = ns;
            a[1] = client;
            ns.BeginRead(b, 0, b.Length, new AsyncCallback(ReadMsg), a);
            listener.BeginAcceptTcpClient(new AsyncCallback(ClientConnect), listener);
        }
        private void ReadMsg(IAsyncResult ar)
        {

            if (isHost)
            {
                object[] a = (object[])ar.AsyncState;
                NetworkStream ns = (NetworkStream)a[0];
                TcpClient client = (TcpClient)a[1];
                int count = ns.EndRead(ar);
                string data = ASCIIEncoding.ASCII.GetString(b, 0, count);
                string[] msg = data.Split(',');
                if (msg[0].Contains("@name@"))
                {
                    string name = msg[0].Replace("@name@", "");
                    lstClients.Add(name + lstClients.Count, client);
                    messageCmnt.Text = msg[1] + "Connected";
                    //wordInitiator(Convert.ToInt32(msg[1]));
                    sendToClient("wc,You are connected to " + nameTxt.Text, client);
                }
                else
                {
                    // recieved msg is here !!! keys from A to Z
                    //pressed = msg[0];
                    char letter = Convert.ToChar(msg[0][0]);
                    if (((letter >= 'A' && letter <= 'Z') || (letter >= 'a' && letter <= 'z')) && !pressed.Contains(letter))
                    {
                        checker(letter);
                    }
                }
                ns.BeginRead(b, 0, b.Length, new AsyncCallback(ReadMsg), a);
            }
            else
            {
                NetworkStream ns = (NetworkStream)ar.AsyncState;
                int count = ns.EndRead(ar);
                string data = ASCIIEncoding.ASCII.GetString(b, 0, count);
                string[] msg = data.Split(',');
                if (msg[0].Contains("wc"))
                {
                    messageCmnt.Text = msg[1];
                }
                else if (msg[0].Contains("play"))
                {
                    DialogResult ans =  MessageBox.Show("You have play request! Start Game","Request",MessageBoxButtons.YesNo);
                    if (ans == DialogResult.Yes)
                    {
                        isGuessing = true;
                        this.KeyPreview = true;
                        isPlaying = true;
                        word = msg[1].Remove(msg[1].Length - 2, 2);
                        wordInitiator(word.Length);
                        Thread.Sleep(1500);
                    }
                   
                }
                ns.BeginRead(b, 0, b.Length, ReadMsg, ns);
            }
            
           
           
            

        }


























        //initiates words and button
        public void wordInitiator(int wordlen)
        {
           
            wordPanel.Controls.Clear();
            for (int i = 0; i < wordlen; i++)
            {
                string placeHolder = isGuessing ? "____   " : word[i].ToString();
                BunifuThinButton2 btn = wordMaker(placeHolder);
                btns.Add(btn);

                //wordPanel.Controls.Add(btn);
                this.BeginInvoke((Action)(() =>
                {
                    //perform on the UI thread
                    this.wordPanel.Controls.Add(btn);
                }));
            }
        }


        //button generator
        public BunifuThinButton2 wordMaker(string word)
        {
            BunifuThinButton2 bunifuThinButton21 = new BunifuThinButton2();
            bunifuThinButton21.ActiveBorderThickness = 1;
            bunifuThinButton21.ActiveCornerRadius = 60;
            bunifuThinButton21.ActiveFillColor = System.Drawing.Color.FromArgb(((int)(((byte)(133)))), ((int)(((byte)(102)))), ((int)(((byte)(170)))));
            bunifuThinButton21.ActiveForecolor = System.Drawing.Color.FromArgb(((int)(((byte)(217)))), ((int)(((byte)(236)))), ((int)(((byte)(235)))));
            bunifuThinButton21.ActiveLineColor = System.Drawing.Color.FromArgb(((int)(((byte)(217)))), ((int)(((byte)(236)))), ((int)(((byte)(235)))));
            bunifuThinButton21.BackColor = System.Drawing.SystemColors.ControlLightLight;
            //bunifuThinButton21.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("bunifuThinButton21.BackgroundImage")));
            bunifuThinButton21.ButtonText = word;
            bunifuThinButton21.Cursor = System.Windows.Forms.Cursors.Default;
            bunifuThinButton21.Font = new System.Drawing.Font("SF Pro Display", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            bunifuThinButton21.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(133)))), ((int)(((byte)(102)))), ((int)(((byte)(170)))));
            bunifuThinButton21.IdleBorderThickness = 1;
            bunifuThinButton21.IdleCornerRadius = 50;
            bunifuThinButton21.IdleFillColor = System.Drawing.Color.White;
            bunifuThinButton21.IdleForecolor = System.Drawing.Color.FromArgb(((int)(((byte)(133)))), ((int)(((byte)(102)))), ((int)(((byte)(170)))));
            bunifuThinButton21.IdleLineColor = System.Drawing.Color.FromArgb(((int)(((byte)(133)))), ((int)(((byte)(102)))), ((int)(((byte)(170)))));
            bunifuThinButton21.Location = new System.Drawing.Point(6, 5);
            bunifuThinButton21.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
            bunifuThinButton21.Name = "bunifuThinButton21";
            bunifuThinButton21.Size = new System.Drawing.Size(52, 61);
            bunifuThinButton21.TabIndex = 0;
            bunifuThinButton21.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            //bunifuThinButton21.Click += new System.EventHandler(this.bunifuThinButton21_Click);
            return bunifuThinButton21;
        }



        string pressed = "";


        //litsen for keypresses in background
        void Hangman_KeyDown(object sender, KeyEventArgs e)
        {
            if (isGuessing)
            {
                char letter = Convert.ToChar(e.KeyValue);
                if (isPlaying)
                {

                    if (((letter >= 'A' && letter <= 'Z') || (letter >= 'a' && letter <= 'z')) && !pressed.Contains(letter))
                    {
                        sendToClient(letter.ToString(),client);
                        //guessWrtxt.Text += e.KeyData.ToString();
                        guesser(letter);
                    }
                }
                else
                {
                    MessageBox.Show("Select a word", "Error", MessageBoxButtons.OK);
                }
            }           
        }


        public void checker(char letter)
        {
            if (word.Contains(letter))
            {
                for (int i = 0; i < word.Length; i++)
                {
                    if (word[i] == letter)
                    {
                        btns[i].ButtonText = letter.ToString();
                        btns[i].IdleForecolor = btns[i].IdleLineColor = Color.FromArgb(255, 189, 105);
                        rightGuess += letter;
                    }
                }
                if (rightGuess.Length == word.Length)
                {
                    messageCmnt.Text = "You Lost :( ";

                    reseter();
                }
            }
            else
            {
                guessWrtxt.Text += letter.ToString();
                if (guessWrtxt.Text.Length >= maxWrongGuess)
                {
                    isPlaying = false;
                    messageCmnt.Text = "You Win :)";
                    reseter();
                }
            }
            pressed += letter;
        }


        public void guesser(char letter)
        {
            if (word.Contains(letter))
            {
                for (int i = 0; i < word.Length; i++)
                {
                    if (word[i] == letter)
                    {
                        btns[i].ButtonText = letter.ToString();
                        rightGuess += letter;
                    }
                }
                if (rightGuess.Length == word.Length)
                {
                    won();
                    
                    reseter();
                }
            }
            else
            {
                guessWrtxt.Text += letter.ToString();
                if (guessWrtxt.Text.Length >= maxWrongGuess)
                {
                    isPlaying = false;
                    Lost();
                    reseter();
                }
            }
            pressed += letter;
        }

        private void won()
        {
            messageCmnt.Text = "Wow!! You Won!";
            
        }

        private void reseter()
        {
            guessWrtxt.ResetText();
          
            lenLbl.ResetText();
            hintTxt.ResetText();
            wordTxt.ResetText();
            word = rightGuess = "";
            maxWrongGuess = 5 ;
            btns.Clear();
            KeyPreview = false;
            isPlaying = false;
        }

        private void Lost()
        {
            messageCmnt.Text = "Booo!!! You Lost!!";
        }
       

        List<MetroRadioButton> boxes = new List<MetroRadioButton>();
        public MetroRadioButton checkBoxMaker(string name)
        {

            MetroRadioButton chkbox = new MetroRadioButton();

            chkbox.AutoSize = true;
            chkbox.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            chkbox.CustomBackground = true;
            chkbox.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            chkbox.FontSize = MetroFramework.MetroLinkSize.Tall;
            chkbox.FontWeight = MetroFramework.MetroLinkWeight.Bold;
            chkbox.ForeColor = System.Drawing.Color.Transparent;
            chkbox.Location = new System.Drawing.Point(735, 401);
            chkbox.Margin = new System.Windows.Forms.Padding(15, 20, 15, 15);
            chkbox.Name = "CBtn" + name;
            chkbox.Size = new System.Drawing.Size(95, 24);
            chkbox.Style = MetroFramework.MetroColorStyle.Teal;
            chkbox.TabIndex = 17;
            chkbox.TabStop = true;
            chkbox.Text = name;
            chkbox.UseStyleColors = true;
            chkbox.UseVisualStyleBackColor = true;
            //chkbox.CheckedChanged += new System.EventHandler(this.metroRadioButton1_CheckedChanged);

            boxes.Add(chkbox);
            this.BeginInvoke((Action)(() =>
            {
                //perform on the UI thread
                this.flowLayoutPanel1.Controls.Add(chkbox);
            }));
            return chkbox;
        }

        bool isHost = false;

        private void ConnectBtn_Click(object sender, EventArgs e)
        {
            isHost = true;
            cnctbtn.Enabled = cncttxt.Enabled = false;
            CheckForIllegalCrossThreadCalls = false;
            TcpListener listener = new TcpListener(IPAddress.Loopback, 11000);
            listener.Start(10);
            playStatuslbl.Text = "SERVER";
            listener.BeginAcceptTcpClient(new AsyncCallback(ClientConnect), listener);
        }

        private void sendToAll(object sender, EventArgs e)
        {
            foreach (var item in clients)
            {
            //    textBox1.AppendText(Environment.NewLine);
            //    textBox1.AppendText("Me: " + textBox3.Text);
            //    NetworkStream stream = item.GetStream();
            //    StreamWriter sdr = new StreamWriter(stream);
            //    sdr.WriteLine(textBox3.Text);
            //    sdr.Flush();
            }
        }


    //Send Message to Selected Client
    private void sendToClient(string data, TcpClient client)
         {
           
            NetworkStream stream = client.GetStream();
            StreamWriter sdr = new StreamWriter(stream);
            sdr.WriteLine(data);
            sdr.Flush();
        }

        byte[] clientb = new byte[1024];
        TcpClient client = new TcpClient();
        private void cnctbtn_Click(object sender, EventArgs e)
        {
            try
            {
                if (nameTxt.Text.Trim() == "")
                {
                    nameTxt.LineIdleColor = Color.Red;
                }
                else
                {
                    isGuessing = true; // means k is ne word guess karna he
                    CheckForIllegalCrossThreadCalls = false;
                    client.Connect(IPAddress.Loopback, 11000);
                    NetworkStream ns = client.GetStream();
                    StreamWriter sw = new StreamWriter(ns);
                    sw.WriteLine("@name@," + nameTxt.Text);
                    playBTN.Enabled = HostBtn.Enabled = isHost = false;
                    cncttxt.Hide();

                    playStatuslbl.Text = "Client";
                    sw.Flush();
                    ns.BeginRead(b, 0, b.Length, ReadMsg, ns);
                }
                
            }
            catch (Exception err)
            {

                MessageBox.Show(err.ToString(),"Error Occured",MessageBoxButtons.OK);
            }
           
        }

        private void Hangman_FormClosing(object sender, FormClosingEventArgs e)
        {
            MessageBox.Show("Tata babu mushai :)");
        }

        
    }
}
