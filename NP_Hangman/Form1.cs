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
        TcpClient activeClient;
        bool isPlaying = false;
        bool isGuessing = false;
        int lifeCounter = 0;
        bool isSinglePlayer = false;

        bool isHost = false;

        /// <summary> FOR CLIENT SIDE ONLY
        string chosenClient = "";
        bool clientChosen = false;
        string clientName ;
        NetworkStream hostStream;
        /// </summary>



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
            else if (!clientChosen && activeClient == null)
            {
                DialogResult ans = MessageBox.Show("No Player Selected! Do You want to play as single Player", "Request", MessageBoxButtons.YesNo);
                if (ans == DialogResult.Yes)
                {
                   
                    reseter();
                    picturePnlReseter();
                    wordTxt.Enabled = hintTxt.Enabled = RandomBTN.Enabled = false;

                    wordTxt.Enabled = hintTxt.Enabled = RandomBTN.Enabled = false;
                    isSinglePlayer = true;
                    //selects Random word
                    string filePath = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "Words.txt");
                    using (TextReader tr = new StreamReader(filePath, Encoding.ASCII))
                    {
                        Random r = new Random();
                        var allWords = tr.ReadToEnd().Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                        string[] Chosenwords = allWords[r.Next(0, allWords.Length - 1)].Split('~');
                        word = Chosenwords[0].Trim().ToUpper();
                        hintTxt.Text = Chosenwords[1];
                    }
                    isGuessing = true;
                    lifeMeterlbl.Text = "5";
                    this.KeyPreview = true;
                    isPlaying = true;
                    lenLbl.Text = word.Length.ToString();
                    wordInitiator(word.Length);
                    Thread.Sleep(1500);
                }
            }
             //validations checking
            else if (wordTxt.Text.Trim(' ') == "")
            {
               wordTxt.LineIdleColor = Color.Red;
            }
            else if(hintTxt.Text.Trim(' ') == "")
            {
                hintTxt.LineIdleColor = Color.Red;
            }
            else
            {
                isGuessing = false; // keyboard background listening is off for the stater
                word = wordTxt.Text.Trim().ToUpper();
                // if the host is the starter
                if (isHost)
                {
                   
                    //a request is sent to the guesser
                    sendToClient("play," + word + "~" + hintTxt.Text, activeClient);
                }
                else
                {
                    // means it wants to play with the host
                    if (clientChosen)
                    {
                        if (chosenClient == "Host\r\n")
                        {
                            clientWriter("play,"+ word + "~" + hintTxt.Text);
                        }
                        else
                        {
                            clientWriter("playClient,"+ nameTxt.Text + "\r\n," + chosenClient + "," + word + "~" + hintTxt.Text); // this means client wants to play with other client
                        }
                    }
                    else
                    {
                 
                   
                        
                    }
                }

            }
        }


        private void clientWriter(string msg)
        {
            StreamWriter sw = new StreamWriter(hostStream);
            sw.WriteLine(msg);
            sw.Flush();
        }





        Dictionary<string, TcpClient> lstClients = new Dictionary<string, TcpClient>();
        byte[] b = new byte[1024];
        //private Hangman hangman;//

        private void ClientConnect(IAsyncResult ar)
        {
            TcpListener listener = (TcpListener)ar.AsyncState;
            TcpClient client = listener.EndAcceptTcpClient(ar);

            clients.Add(client); //maintain clients count
            activeClient = client; // new client is made active client by default
            
            

           
            NetworkStream ns = client.GetStream();
            object[] a = new object[2];                              
            a[0] = ns;
            a[1] = client;

            //to start reading form the client
            ns.BeginRead(b, 0, b.Length, new AsyncCallback(ReadMsg), a);


            listener.BeginAcceptTcpClient(new AsyncCallback(ClientConnect), listener);
        }
        private void ReadMsg(IAsyncResult ar)
        {

            if (isHost) //serverside recvmsg
            {
                object[] a = (object[])ar.AsyncState;
                NetworkStream ns = (NetworkStream)a[0];
                TcpClient client = (TcpClient)a[1]; //a[1] is client sent from ns.BeginRead from above
                int count = ns.EndRead(ar);
                string data = ASCIIEncoding.ASCII.GetString(b, 0, count); // store the recieved data

                string[] msg = data.Split(',');

                //the first message the server would recieve is the name of the client
                if (msg[0].Contains("@name@"))
                {
                    string name = msg[0].Replace("@name@", "");

                    messageCmnt.Text = msg[1] + "Connected";

                    //generate checkboxes for new clients
                    MetroCheckBox chkBox = checkBoxMaker(msg[1]);

                    //this is the first confirmation msg from server with prefix "wc" ie welcome
                    sendToClient("wc,You are connected to " + nameTxt.Text, client);
                    
                    lstClients.Add(msg[1], client); //name and client is stored in dictionary
                    sendClientCLlist();
                }
                else if (msg[0].Contains("playClient")) //player vs player
                {
                    TcpClient play1 = lstClients[msg[1]]; //player 1;
                    TcpClient play2 = lstClients[msg[2]]; //       player2  lstClients.Keys.Where((x) => x.Contains(msg[1]));
                    
                                             //p1           //p2        //word 
                    sendToClient("playC2C," +msg[1] + "," + msg[2] +"," +msg[3], play2);
                }
                else if(msg[0]=="PVPplayyes")
                {
                    TcpClient play1 = lstClients[msg[1]]; 
                    TcpClient play2 = lstClients[msg[2]]; 
                    sendToClient("PVPplayyes," + "," + msg[2] + "," + msg[3], play1);
                }
                else if (msg[0] == "play" )
                {
                    DialogResult ans = MessageBox.Show("You have play request! Start Game " , "Request", MessageBoxButtons.YesNo);
                    if (ans == DialogResult.Yes)
                    {
                        StreamWriter sw = new StreamWriter(ns);
                        activeClient = client;
                        
                        //this is the first message to the server 
                        sw.WriteLine("playyes," + nameTxt.Text);
                        sw.Flush();
                        reseter();
                        picturePnlReseter();
                        wordTxt.Enabled = hintTxt.Enabled = RandomBTN.Enabled = false;


                        string[] chosenWords = msg[1].Split('~');
                        word = chosenWords[0].Trim('\n').Trim('\r').ToUpper();
                        // word = chosenWords[0].Remove(msg[1].Length - 2, 2).Trim().ToUpper();
                        hintTxt.Text = chosenWords[1];

                        isGuessing = true;
                        lifeMeterlbl.Text = "5";
                        this.KeyPreview = true;
                        isPlaying = true;
                        lenLbl.Text = word.Length.ToString();
                        wordInitiator(word.Length);
                        Thread.Sleep(1500);
                    }
                    else
                    {
                        StreamWriter sw = new StreamWriter(ns);

                        //this is the first message to the server 
                        sw.WriteLine("playno," + nameTxt.Text);
                        sw.Flush();

                    }
                }
                else if (msg[0].Contains("playno"))
                {
                    playApprove = false;
                    MessageBox.Show("Request is denied");

                }
                else if (msg[0].Contains("playyes"))
                {
                    playApprove = true;
                    startPlay();
                }
                else if (msg[0]== "PVPguess")
                {
                    TcpClient play1 = lstClients[msg[1]];
                    TcpClient play2 = lstClients[msg[2]];
                    sendToClient( msg[3] + "," + msg[2], play1);
                }
                else
                {
                    // recieved msg is here !!! keys from A to Z
                    char letter = Convert.ToChar(msg[0][0]);
                    if (((letter >= 'A' && letter <= 'Z') || (letter >= 'a' && letter <= 'z')) && !pressed.Contains(letter))
                    {
                        checker(letter);
                    }
                }
                ns.BeginRead(b, 0, b.Length, new AsyncCallback(ReadMsg), a);
            }
            else //clientSide Readmsg
            {
                
                NetworkStream ns = (NetworkStream)ar.AsyncState;
                hostStream = ns;
                int count = ns.EndRead(ar);
                string data = ASCIIEncoding.ASCII.GetString(b, 0, count); //recieving data
                string[] msg = data.Split(','); // msg[0] is prefix like wc , play etc


                if (msg[0].Contains("wc"))
                {
                    messageCmnt.Text = msg[1]; //welcome msg from server
                }
                else if (msg[0].Contains("CCL"))
                {
                    string clientsNames = msg[1];
                    if (clientsNames != "")
                    {
                        string[] Temp = clientsNames.Split('~');
                        flowLayoutPanel1.Controls.Clear();
                        for (int i = 0; i < Temp.Length; i++)
                        {
                            if (nameTxt.Text != Temp[i].Remove(Temp[i].Length - 2))
                            {
                                MetroCheckBox chkBox = checkBoxMaker(Temp[i]);
                            }
                            
                        }
                    }
                }
                else if (msg[0].Contains("playC2C"))
                {
                    PVPStart(msg);
                }
                else if (msg[0] == "play")
                {
                    DialogResult ans =  MessageBox.Show("You have play request! Start Game","Request",MessageBoxButtons.YesNo);
                    if (ans == DialogResult.Yes)
                    {
                        StreamWriter sw = new StreamWriter(ns);

                        //this is the first message to the server 
                        sw.WriteLine("playyes," + nameTxt.Text);
                        sw.Flush();

                        reseter();                        //Reseting Panels
                        picturePnlReseter();              //
                        wordTxt.Enabled = hintTxt.Enabled = RandomBTN.Enabled = false;



                        string[] chosenWords = msg[1].Split('~');
                        word = chosenWords[0].Trim('\n').Trim('\r').ToUpper();
                        //word = chosenWords[0].Remove(msg[1].Length - 2, 2).Trim().ToUpper();
                        hintTxt.Text = chosenWords[1];

                        isGuessing = true;
                        lifeMeterlbl.Text = "5";
                        this.KeyPreview = true;
                        isPlaying = true;
                        lenLbl.Text = word.Length.ToString();
                        wordInitiator(word.Length);
                        Thread.Sleep(1500);
                    }
                    else
                    {
                        StreamWriter sw = new StreamWriter(ns);
                        sw.WriteLine("playno," + nameTxt.Text);
                        sw.Flush();
                    }
                }
                else if (msg[0]== "PVPplayyes")
                {
                    playApprove = true;
                    startPlay();
                }
                else if (msg[0] == "playno" || msg[0] == "PVPplayno")
                {
                    playApprove = false;
                    MessageBox.Show("Request is denied");
                }
                else if (msg[0]== "playyes")
                {
                    playApprove = true;
                    startPlay();
                }
                else //if there is no prefix then it must be a pressed key
                {
                    // recieved msg is here !!! keys from A to Z
                    char letter = Convert.ToChar(msg[0][0]);
                    if (((letter >= 'A' && letter <= 'Z') || (letter >= 'a' && letter <= 'z')) && !pressed.Contains(letter))
                    {
                        checker(letter);
                    }
                }
                ns.BeginRead(b, 0, b.Length, ReadMsg, ns);
            }
            
           
           
            

        }

        string player1 = "";
        string player2 = "";
        bool isPVP = false;

        private void PVPStart(string[] msg)
        {
            player1 = msg[1];
            player2 = msg[2];
            //confimation

            DialogResult ans = MessageBox.Show("You have play request! Start Game", "Request", MessageBoxButtons.YesNo);
            if (ans == DialogResult.Yes)
            {
               
                StreamWriter sw = new StreamWriter(hostStream);
                //this is the first message to the server 
                sw.WriteLine("PVPplayyes," + player1 + "," + player2 + "," + msg[3]);
                sw.Flush();

                reseter();                        //Reseting Panels
                picturePnlReseter();              //
                isPVP = true;
                wordTxt.Enabled = hintTxt.Enabled = RandomBTN.Enabled = false;

                string[] chosenWords = msg[3].Split('~');
                word = chosenWords[0].Trim('\n').Trim('\r').ToUpper();
                //word = chosenWords[0].Remove(msg[1].Length - 2, 2).Trim().ToUpper();
                hintTxt.Text = chosenWords[1];


               // word = msg[3].Remove(msg[3].Length - 2, 2);
                isGuessing = true;
                lifeMeterlbl.Text = "5";
                this.KeyPreview = true;
                isPlaying = true;
                lenLbl.Text = word.Length.ToString();
                wordInitiator(word.Length);
                Thread.Sleep(1500);
            }
            else
            {
                StreamWriter sw = new StreamWriter(hostStream);
                sw.WriteLine("PVPplayno," + nameTxt.Text);
                sw.Flush();
            }

        }

        private void startPlay()
        {
            foreach (PictureBox item in picturepnl.Controls)
                item.Hide();
            wordTxt.Enabled = hintTxt.Enabled = RandomBTN.Enabled = false;
            MessageBox.Show("Game is Starting");
            if (playApprove)
            {
                //validations
                nameTxt.LineIdleColor = Color.FromArgb(64, 64, 64);
                wordTxt.LineIdleColor = Color.FromArgb(0, 170, 173);
                hintTxt.LineIdleColor = Color.FromArgb(0, 170, 173);

                int lenTxt = Convert.ToInt32(wordTxt.Text.Length);
                lenLbl.Text = wordTxt.Text.Length.ToString();
                lifeMeterlbl.Text = maxWrongGuess.ToString();
                word = wordTxt.Text.Trim().ToUpper();

                //To populate and display the chosen words on display wordpanel on the top
                wordInitiator(word.Length);

                guessWrtxt.ResetText();
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
                        if (isPVP)
                        {
                            clientWriter("PVPguess," + player1 + "," + player2+ "," +letter.ToString());
                            guesser(letter);
                        }
                        else if(isSinglePlayer)
                        {
                           singlePlayer(letter);
                        }
                        else if (!isHost)
                        {
                            StreamWriter sw = new StreamWriter(hostStream);
                            //this is the first message to the server 
                            sw.WriteLine(letter.ToString());
                            sw.Flush();
                            guesser(letter);
                        }
                        else
                        {
                            sendToClient(letter.ToString(), activeClient);
                            guesser(letter);
                        }                   
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
                    messageCmnt.Text = "You Lost :( ";   //GUesser wins

                    reseter();
                }
            }
            else
            {
                lifeCounter++;
                guessWrtxt.Text += letter.ToString();
                lifeMeterlbl.Text = (maxWrongGuess - lifeCounter).ToString();
                int index = picturepnl.Controls.IndexOfKey("HG"+(lifeCounter).ToString());
                picturepnl.Controls[index].Visible = true;
                if (lifeCounter >= maxWrongGuess)
                {
                    isPlaying = false;
                    messageCmnt.Text = "You Win :)";  //starter wins //guesser lost
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
                lifeCounter++;
                guessWrtxt.Text += letter.ToString();
                lifeMeterlbl.Text = (maxWrongGuess - lifeCounter).ToString();
                int index = picturepnl.Controls.IndexOfKey("HG" + (lifeCounter).ToString());
                picturepnl.Controls[index].Visible = true;
                //picturepnl.Controls[(guessWrtxt.Text.Length) - 1].Visible = true;
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
            wordTxt.Enabled = hintTxt.Enabled = RandomBTN.Enabled = true;
            isSinglePlayer = false;
            isPVP = false;
            isPlaying = false;
            lifeCounter = 0;
            isGuessing = false;
            guessWrtxt.ResetText();
            playApprove = false;
            lifeMeterlbl.ResetText();
            lenLbl.ResetText();
            hintTxt.ResetText();
            wordTxt.ResetText();
            guessWrtxt.ResetText();
            word = rightGuess = "";
            maxWrongGuess = 5 ;
            btns.Clear();
            KeyPreview = false;
            isPlaying = false;
            pressed = "";
        }

        private void picturePnlReseter()
        {
            foreach (PictureBox item in picturepnl.Controls)
            {
                item.Hide();
            }
        }

        private void Lost()
        {
            messageCmnt.Text = "Booo!!! You Lost!!! \n The word was " + word;
            
        }
       

        List<MetroCheckBox> boxes = new List<MetroCheckBox>();
        public MetroCheckBox checkBoxMaker(string name)
        {

            MetroCheckBox  chkbox = new MetroCheckBox();

            chkbox.AutoSize = true;
            chkbox.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            chkbox.CustomBackground = true;
            chkbox.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            chkbox.FontSize = MetroFramework.MetroLinkSize.Tall;
            chkbox.FontWeight = MetroFramework.MetroLinkWeight.Bold;
            chkbox.ForeColor = System.Drawing.Color.Transparent;
            //chkbox.Location = new System.Drawing.Point(735, 401);
            chkbox.Margin = new System.Windows.Forms.Padding(15, 20, 15, 15);
            chkbox.Name = "CBtn" + name;
            chkbox.Size = new System.Drawing.Size(95, 24);
            chkbox.Style = MetroFramework.MetroColorStyle.Teal;
            chkbox.TabIndex = 17;
            chkbox.TabStop = true;
            chkbox.Checked = isHost ? true: false ;
            chkbox.Text = name;
            chkbox.UseStyleColors = true;
            chkbox.UseVisualStyleBackColor = true;
            chkbox.CheckStateChanged += new System.EventHandler(this.metroRadioButton1_CheckedChanged);

            boxes.Add(chkbox);
            this.BeginInvoke((Action)(() =>
            {
                //perform on the UI thread
                this.flowLayoutPanel1.Controls.Add(chkbox);
            }));
            return chkbox;
        }


        private void ConnectBtn_Click(object sender, EventArgs e)
        {
            isHost = true;
            nameTxt.Enabled = false;
            //cnctbtn.Enabled = cncttxt.Enabled = false;
            cnctbtn.Hide();
            cncttxt.Hide();

            CheckForIllegalCrossThreadCalls = false;

            IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName()); // `Dns.Resolve()` method is deprecated.
            IPAddress ipAddress = ipHostInfo.AddressList[1];
            //TcpListener listener = new TcpListener(IPAddress.Loopback, 11000);
            TcpListener listener = new TcpListener(ipAddress,11000);
            listener.Start(10);
            playStatuslbl.Text = ipHostInfo.AddressList[1].ToString();
            listener.BeginAcceptTcpClient(new AsyncCallback(ClientConnect), listener);
        }

        private void sendToAll(object sender, EventArgs e)
        {
            foreach (var item in clients)
            {
                NetworkStream stream = activeClient.GetStream();
                StreamWriter sdr = new StreamWriter(stream);
                sdr.WriteLine("Leave");
                sdr.Flush();
            }
        }

        private void sendClientCLlist()
        {
            String ClientsToSend = "";
            foreach (String item in lstClients.Keys)
            {
                ClientsToSend += item + "~";
            }
            foreach (var item in clients)
            {
                NetworkStream stream = item.GetStream();
                StreamWriter sdr = new StreamWriter(stream);
                sdr.WriteLine("CCL," + ClientsToSend + "Host");
                sdr.Flush();
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
        private bool playApprove = false;

        private void cnctbtn_Click(object sender, EventArgs e)
        {
            try
            {
                if (nameTxt.Text.Trim() == "" )
                {
                    nameTxt.LineIdleColor = Color.Red;
                }
                else if (cncttxt.Text.Trim() == "")
                {
                    cncttxt.LineIdleColor = Color.Red;
                }
                else
                {
                    nameTxt.LineIdleColor = Color.Teal;
                    cncttxt.LineIdleColor = Color.Teal;

                    clientName = isHost ? "Host": nameTxt.Text.Trim(); // if client then set the name to the chosen name
                    string myIP = "";
                    if (cncttxt.Text.Trim().Contains("192"))
                    {
                        myIP = cncttxt.Text;
                    }
                    else
                    {
                        string hostname = cncttxt.Text;
                        try
                        {
                           // myIP = Dns.GetHostByName(hostname).AddressList[0].ToString();
                            IPHostEntry ipHostInfo = Dns.GetHostEntry(hostname); // `Dns.Resolve()` method is deprecated.
                            myIP = ipHostInfo.AddressList[1].ToString();
                        }
                        catch (Exception err)
                        {

                            //MessageBox.Show(err.ToString());
                            MessageBox.Show("IP is Invalid please enter a valid IP");
                        }
                        
                    }
                    
                   //isGuessing = true; // means k is ne word guess karna he
                    CheckForIllegalCrossThreadCalls = false;
                    client.Connect(IPAddress.Parse(myIP), 11000);
                    NetworkStream ns = client.GetStream();
                    StreamWriter sw = new StreamWriter(ns); 

                    //this is the first message to the server 
                    sw.WriteLine("@name@," + nameTxt.Text);

                    nameTxt.Enabled = false;
                    HostBtn.Enabled = isHost = false;
                    //playBTN.Hide();
                    HostBtn.Hide();
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
            //MessageBox.Show("Tata babu mushai :)");
        }

        private void metroRadioButton1_CheckedChanged(object sender, EventArgs e)
        {
            
            MetroCheckBox m = (MetroCheckBox)sender;
            foreach (MetroCheckBox item in flowLayoutPanel1.Controls)
            {
                if(item != m)
                    item.Checked = false;
            }
            //m.Checked = m.Checked ? false : true;
            if (m.Checked)
            {
                if (isHost)
                {

                    int len = m.Name.Length - 2;
                    string name = m.Name.Replace("CBtn", "");
                    activeClient = lstClients[name];
                    currentPlayerLbl.Text = m.Name.Substring(4);
                }
                else
                {
                    string name = m.Name.Replace("CBtn", "");
                    clientChosen = true;
                    chosenClient = name;
                    currentPlayerLbl.Text = m.Name.Substring(4);
                }
            }
            else
            {
                if (isHost)
                {
                    activeClient = null;
                    currentPlayerLbl.Text = "";
                }

                else
                {
                    chosenClient = null;
                    clientChosen = false;
                    currentPlayerLbl.Text = "";
                }
            }
        }



        private void singlePlayer(char letter)
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
                lifeCounter++;
                guessWrtxt.Text += letter.ToString();
                lifeMeterlbl.Text = (maxWrongGuess - lifeCounter).ToString();
                int index = picturepnl.Controls.IndexOfKey("HG" + (lifeCounter).ToString());
                picturepnl.Controls[index].Visible = true;
                //picturepnl.Controls[(guessWrtxt.Text.Length) - 1].Visible = true;
                if (guessWrtxt.Text.Length >= maxWrongGuess)
                {
                    isPlaying = false;
                    Lost();
                    reseter();
                }
            }
            pressed += letter;
        }

        private void RandomBTN_Click(object sender, EventArgs e)
        {
            string filePath = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "Words.txt");
            using (TextReader tr = new StreamReader(filePath, Encoding.ASCII))
            {
                Random r = new Random();
                var allWords = tr.ReadToEnd().Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                string[] chosenWords = allWords[r.Next(0, allWords.Length - 1)].Split('~');
                wordTxt.Text =chosenWords[0].Trim().ToUpper() ;
                hintTxt.Text = chosenWords[1].Trim();
            }
        }
    }
}
