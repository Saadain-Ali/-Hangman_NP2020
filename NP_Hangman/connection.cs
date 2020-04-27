//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Net;
//using System.Net.Sockets;
//using System.Text;
//using System.Threading.Tasks;
//using System.Windows.Forms;

//namespace NP_Hangman
//{
//    public class connection
//    {
//        List<TcpClient> clients = new List<TcpClient>(); //ToolBar store connected clients
//        TcpListener server;
//        TcpClient activeClient;

//        public connection(string ip, Hangman hangman)
//        {
//            this.hangman = hangman;
//            //CheckForIllegalCrossThreadCalls = false;
//            TcpListener listener = new TcpListener(IPAddress.Parse(ip), 11000);
//            listener.Start(10);
//            listener.BeginAcceptTcpClient(new AsyncCallback(ClientConnect), listener);
//        }
//        public connection(Hangman hangman)
//        {
//            this.hangman = hangman;
//            //CheckForIllegalCrossThreadCalls = false;
//            TcpListener listener = new TcpListener(IPAddress.Loopback, 11000);
//            listener.Start(10);
//            listener.BeginAcceptTcpClient(new AsyncCallback(ClientConnect), listener);

//        }


//        //Accept clients

//        Dictionary<string, TcpClient> lstClients = new Dictionary<string, TcpClient>();
//        byte[] b = new byte[1024];
//        private Hangman hangman;

//        private void ClientConnect(IAsyncResult ar)
//        {
//            TcpListener listener = (TcpListener)ar.AsyncState;
//            TcpClient client = listener.EndAcceptTcpClient(ar);
//            clients.Add(client);

//            //generate checkboxes for new clients
//            MetroFramework.Controls.MetroRadioButton chkBox = checkBoxMaker(clients.Count.ToString());
            
//            activeClient = client;
//            NetworkStream ns = client.GetStream();
//            object[] a = new object[2];
//            a[0] = ns;
//            a[1] = client;
//            ns.BeginRead(b, 0, b.Length, new AsyncCallback(ReadMsg), a);
//            listener.BeginAcceptTcpClient(new AsyncCallback(ClientConnect), listener);
//        }
//        private void ReadMsg(IAsyncResult ar)
//        {
//            object[] a = (object[])ar.AsyncState;
//            NetworkStream ns = (NetworkStream)a[0];
//            TcpClient client = (TcpClient)a[1];
//            int count = ns.EndRead(ar);
//            string msg = ASCIIEncoding.ASCII.GetString(b, 0, count);

//            if (msg.Contains("@name@"))
//            {
//                string name = msg.Replace("@name@", "");
//                lstClients.Add(name + lstClients.Count, client);
//            }
//            else
//            {
                
//                // recieved msg is here !!!
//                //textBox1.AppendText(Environment.NewLine + "Client" + (clients.IndexOf(client) + 1) + ": " + msg);
//            }
//            ns.BeginRead(b, 0, b.Length, new AsyncCallback(ReadMsg), a);

//        }

//        private void sendToActiveClient(string msg)
//        { 
//            NetworkStream stream = activeClient.GetStream();
//            StreamWriter sdr = new StreamWriter(stream);
//            sdr.WriteLine(msg);
//            sdr.Flush();
//        }

//    }
//}
