using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Data.Sql;
using System.Data.SqlClient;
using System.Threading;

namespace Server_PC
{
    public partial class Form1 : Form
    {
        SqlConnection con = new SqlConnection(@"Data Source=.\SQLEXPRESS;AttachDbFilename=C:\Users\Lalit\Documents\Visual Studio 2010\Projects\server\Server_PC\Database1.mdf;Integrated Security=True;User Instance=True");
        SqlCommand cmd;
        string command;
        string clientIP;
        bool flag = false;
        private IPAddress hostIP = null;
        private int hostPort = -1;
        TcpClient tcpClient = null;
        private Thread checkForConnection = null;
        private Thread[] recieve = new Thread[5];
        private string recvDt = null;
        public delegate void rcvData();
        static public int MAX_CONN = 5;
        private string[] ipList;
        NetworkStream[] nStream = null;
        static int thrd = -1;
        private String currentMsg = null;
        private Boolean stopRecieving = false;
        private Boolean sameIP = false;
        private string file = "";
        private string ipselected = "";
        string roll;
        string login;
        TcpListener myList = null;

        public Form1()
        {
            InitializeComponent();
            con.Open();
            cmd_send.Enabled = false;
            textBox2.Enabled = false;
            button4.Enabled = false;
            button6.Enabled = false;
            hostIP = getHostIP();
            for (int i = 0; i < recieve.Length; i++)
            {
                recieve[i] = new Thread(new ThreadStart(recieveData));
            }
        }

        public void addToOutput()
        {
            if (recvDt != null && recvDt != "")
            {
                output.Text += "\nRecieved " + recvDt;
                recvDt = null;
            }
        }

        public void addNotification()
        {
            if (currentMsg != null)
            {
                output.Text += currentMsg + Environment.NewLine;
            }
        }

        private IPAddress getHostIP()
        {
            return Dns.GetHostByName(Dns.GetHostName()).AddressList[0];
        }

        private void cmd_connect_Click(object sender, EventArgs e)
        {
            hostPort = int.Parse(textBox1.Text);
            ipList = new string[MAX_CONN];
            nStream = new NetworkStream[MAX_CONN];
            myList = new TcpListener(hostIP, hostPort);
            myList.Start();
            output.Text += "Listening server started @ " + hostIP.ToString() + Environment.NewLine;
            output.Text += "Listening on " + hostPort + " port" + Environment.NewLine;
            cmd_connect.Enabled = false;
            textBox1.Enabled = false;
            pictureBox1.Visible = false;
            checkForConnection = new Thread(new ThreadStart(performConnect));
            checkForConnection.Start();
            output.Text += "Waiting for connection . . ." + Environment.NewLine;
        }

        private void performConnect()
        {
            while (true)
            {
                if (myList.Pending())
                {
                    thrd = thrd + 1;
                    tcpClient = myList.AcceptTcpClient();
                    IPEndPoint ipEndPoint = (IPEndPoint)tcpClient.Client.RemoteEndPoint;
                    clientIP = ipEndPoint.Address.ToString();
                    nStream[thrd] = tcpClient.GetStream();
                    currentMsg = "\n New IP client found :" + clientIP;
                    recieve[thrd].Start();
                    this.Invoke(new rcvData(addNotification));
                    try
                    {
                        addToIPList(clientIP);
                        login = DateTime.Now.TimeOfDay.ToString();
                        //this.Invoke(new rcvData(addtoData));
                        MessageBox.Show(clientIP + " IS CONNECTED");
                    }
                    catch (InvalidOperationException exp)
                    {
                        Console.Error.WriteLine(exp.Message);
                    }
                    Thread.Sleep(1000);
                }
            }
        }

        public bool addToIPList(string IP)
        {
            int i = 0;
            for (i = 0; i < MAX_CONN; i++)
            {
                if (IP.CompareTo(ipList[i]) == 0)
                {
                    sameIP = true;
                    break;
                }
            }
            if (sameIP == false)
            {
                ipList[thrd] = IP;
                Console.WriteLine(ipList[thrd]);
                currentMsg = IP;
                this.Invoke(new rcvData(addToCombo));
                return true;
            }
            else
            {
                MessageBox.Show(IP + " is again trying to connect the server");
                return false;
            }
            throw new InvalidOperationException("connecting pc is already in the thread list\nconnection refused");
        }

        public void addToCombo()
        {
            comboBox1.Items.Add(currentMsg);
        }

        private void cmd_send_Click(object sender, EventArgs e)
        {
            try
            {
                String str = textBox2.Text.ToString();
                sendData(str);
                output.Text += "\nSent to " + comboBox1.SelectedItem.ToString() + " : " + str;
                textBox2.Clear();
            }
            catch (Exception ex)
            {
                output.Text += "Error.....\n " + ex.StackTrace;
            }
        }

        private void recieveData()
        {
            NetworkStream nStream = tcpClient.GetStream();
            ASCIIEncoding ascii = null;
            while (!stopRecieving)
            {
                if (nStream.CanRead && nStream.DataAvailable)
                {
                    byte[] buffer = new byte[64];
                    nStream.Read(buffer, 0, buffer.Length);
                    ascii = new ASCIIEncoding();
                    recvDt = ascii.GetString(buffer);
                    /*Received message checks if it has +@@+ then the ip is disconnected*/
                    bool f = false;
                    f = recvDt.Contains("+@@+");
                    if (f)
                    {
                        string d = "+@@+";
                        recvDt = recvDt.TrimStart(d.ToCharArray());
                        clientDis();
                        stopRecieving = true;
                        flag = false;
                    }
                    else if (recvDt.Contains("^^"))
                    {
                        new Transmit_File().transfer_file(file, ipselected);
                    }
                    /* ++-- shutsdown/restrt/logoff/abort*/
                    else if (recvDt.Contains("++--"))
                    {
                        string d = "++--";
                        recvDt = recvDt.TrimStart(d.ToCharArray());
                        this.Invoke(new rcvData(addToOutput));
                        clientDis();
                    }
                    /*--++ Normal msg*/
                    else if (recvDt.Contains("--++"))
                    {
                        string d = "--++";
                        recvDt = recvDt.TrimStart(d.ToCharArray());
                        this.Invoke(new rcvData(addToOutput));
                    }
                    else if(recvDt.Contains("!!"))
                    {
                        string d = "!!";
                        recvDt = recvDt.TrimStart(d.ToCharArray());
                        int lastLt = recvDt.LastIndexOf("}");
                        recvDt = recvDt.Substring(0, lastLt);
                        NetworkStream ns = tcpClient.GetStream();
                        if (ns.CanWrite)
                        {
                            string dataS = "%%Y";
                            byte[] bf = new ASCIIEncoding().GetBytes(dataS);
                            ns.Write(bf, 0, bf.Length);
                            ns.Flush();
                        }
                        try
                        {
                            new Recieve_File().recieve_file(recvDt);
                        }
                        catch (Exception ec)
                        {
                            System.Console.WriteLine(ec.Message);
                        }
                    }
                    else if (recvDt.Contains("115"))
                    {
                        roll = recvDt.Substring(0, 11);
                        //this.Invoke(new rcvData(addtoData));
                    }
                }
                Thread.Sleep(1000);
            }
        }

        public void updatetoData()
        {
            command = "INSERT INTO TB(IP,RollNo,Login,Logout) values('" + recvDt + "','" + roll + "','" + login + "','" + DateTime.Now.TimeOfDay.ToString() + "');";
            cmd = new SqlCommand(command,con);
            cmd.ExecuteNonQuery();
            MessageBox.Show("Database Updated");
        }
        public void clientDis()
        {
            int index;
            if (recvDt != null)
            {
                if (flag == false)
                {
                    index = recvDt.IndexOf('\0');
                    recvDt = recvDt.Substring(0, index);
                    this.Invoke(new rcvData(updatetoData));
                }
                System.Console.WriteLine("\n Client Disconnected : " + recvDt);
                this.Invoke(new rcvData(clientDisconnected));
                this.Invoke(new rcvData(comboBox1.Items.Clear));
                
                for (int a = 0; a < ipList.Length; a++)
                {
                    Console.WriteLine(ipList[a]);
                    string tr = "\0";
                    string aa = recvDt.TrimEnd(tr.ToCharArray());
                    if (ipList[a].Equals(aa))
                    {
                        for (int b = a; ipList[b] != null; b++)
                        {
                            ipList[b] = ipList[b + 1];
                        }
                        break;
                    }
                }
                this.Invoke(new rcvData(addCom));
            }
        }

        public void addCom()
        {
            if (ipList[0] != null)
            {
                for (int c = 0; ipList[c] != null; c++)
                {
                    Console.WriteLine(ipList[c]);
                    comboBox1.Items.Add(ipList[c]);
                }
            }
        }
        public void clientDisconnected()
        {
            output.Text += "\n Client Disconnected : " + recvDt;
        }

        private void sendData(String data)
        {
            IPAddress ipep = IPAddress.Parse(comboBox1.SelectedItem.ToString());
            Socket server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint ipept = new IPEndPoint(ipep, hostPort);
            NetworkStream nStream = tcpClient.GetStream();
            ASCIIEncoding asciidata = new ASCIIEncoding();
            byte[] buffer = asciidata.GetBytes(data);
            if (nStream.CanWrite)
            {
                nStream.Write(buffer, 0, buffer.Length);
                nStream.Flush();
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            textBox2.Enabled = true;
            cmd_send.Enabled = true;
            groupBox1.Enabled = true;
            button6.Enabled = true;
            listBox1.Items.Clear();
            label8.Text = "0";
            leetSocket1.AutoListening = false;
            count = 0;
            ipselected = comboBox1.SelectedItem.ToString();
        }

        /*File transfer*/

        private void button6_Click(object sender, EventArgs e)
        {
            FileDialog fDg = new OpenFileDialog();
            if (fDg.ShowDialog() == DialogResult.OK)
            {
                file = fDg.FileName;

                string[] f = file.Split('\\');

                string fnm = f[f.Length - 1];
                fnm = "##" + fnm + "|";
                NetworkStream ns = tcpClient.GetStream();
                if (ns.CanWrite)
                {
                    byte[] bf = new ASCIIEncoding().GetBytes(fnm);
                    ns.Write(bf, 0, bf.Length);
                    ns.Flush();
                }
            }
        }

        private void exit_Click(object sender, EventArgs e)
        {
            int i = 0;
            for (i = 0; i < MAX_CONN; i++)
            {
                if (recieve != null)
                    if (recieve[i] != null)
                        recieve[i].Abort();
            }
            if (checkForConnection != null)
                checkForConnection.Abort();
            if (myList != null)
                myList.Stop();
            con.Close();
            Application.Exit();
        }

        private void cmd_comand_Click(object sender, EventArgs e)
        {
            flag = true;
            try
            {
                if (radioButton1.Checked)
                {
                    currentMsg = "*+*-shutdown";
                    sendData(currentMsg);
                    currentMsg = "\nShutdown initiating to " + comboBox1.SelectedItem.ToString() + " Ip";
                    this.Invoke(new rcvData(addNotification));
                    recvDt = comboBox1.SelectedItem.ToString();
                    clientDis();

                }
                else if (radioButton2.Checked)
                {
                    currentMsg = "*+*-restart";
                    sendData(currentMsg);
                    currentMsg = "\nRestart initiating to " + comboBox1.SelectedItem.ToString() + " Ip";
                    this.Invoke(new rcvData(addNotification));
                    recvDt = comboBox1.SelectedItem.ToString();
                    clientDis();

                }
                else if (radioButton3.Checked)
                {
                    currentMsg = "*+*-logoff";
                    sendData(currentMsg);
                    currentMsg = "\nLogoff to " + comboBox1.SelectedItem.ToString() + " Ip";
                    this.Invoke(new rcvData(addNotification));
                    recvDt = comboBox1.SelectedItem.ToString();
                    clientDis();
                }
                else if (radioButton4.Checked)
                {
                    currentMsg = "*+*-abort";
                    sendData(currentMsg);
                    currentMsg = "\nShutdown / Restart aborted to " + comboBox1.SelectedItem.ToString() + " Ip";
                    this.Invoke(new rcvData(addNotification));
                    recvDt = comboBox1.SelectedItem.ToString();
                    clientDis();
                }
            }

            catch (NullReferenceException exp)
            {
                Console.WriteLine(exp.Message);
            }
            Thread.Sleep(2000);
        }

        /*Task Manager*/
        Bitmap bitmap;
        public static int count = 1;
        string folder_name;

        private void button4_Click(object sender, EventArgs e)
        {
            leetSocket1.sendObject(listBox1.SelectedIndex);
            listBox1.Items.RemoveAt(listBox1.SelectedIndex);
        }

        private void leetSocket1_OnReceiveCompletedDataEVENT(object value, byte[] bArray)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new MethodInvoker(delegate() { leetSocket1_OnReceiveCompletedDataEVENT(value, bArray); }));
                return;
            }
            if (tabControl1.SelectedIndex == 1)
            {
                bitmap = (Bitmap)value;
                pictureBox2.Image = bitmap;
            }
            else if (tabControl1.SelectedIndex == 2)
            {
                string rec = (string)value;
                if (rec == "Clear")
                {
                    listBox1.Items.Clear();
                }
                else
                {
                    listBox1.Items.Add(rec);
                    label8.Text = listBox1.Items.Count.ToString();
                }
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (ipselected != String.Empty)
            {
                leetSocket1.ServerIpAsDNS = ipselected;
                leetSocket1.AutoListening = true;
                leetSocket1.sendObject(1000);
                button7.Enabled = true;
                button4.Enabled = true;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (ipselected != String.Empty)
            {
                leetSocket1.ServerIpAsDNS = ipselected;
                leetSocket1.AutoListening = true;
                leetSocket1.sendObject(2000);
                button2.Enabled = true;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            leetSocket1.sendObject(3000);
            timer1.Stop();
            MessageBox.Show("Stopped retrieving images");
        }

        private void button5_Click(object sender, EventArgs e)
        {
            folder_name = @"C:\Desktop\" + comboBox1.SelectedItem.ToString() + "\\";
            Directory.CreateDirectory(folder_name);
            timer1.Start();
        }

        private void button7_Click(object sender, EventArgs e)
        {
            leetSocket1.sendObject(4000);
            listBox1.Items.Clear();
            label8.Text = "0";
            MessageBox.Show("Stopped Recieving Processes");
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            bitmap.Save(folder_name + count.ToString() + ".jpg");
            count++;
        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabControl1.SelectedTab == tabControl1.TabPages["tabPage4"])
            {
                DataTable dt = new DataTable();
                SqlDataAdapter da = new SqlDataAdapter("Select * from TB", con);
                da.Fill(dt);
                dataGridView1.DataSource = dt;
            }
        }
    }

    /*File Transmission*/
    #region transmit

    class Transmit_File
    {
        StreamWriter writeImageData;
        NetworkStream nStream;

        string Base64ImageData;
        string BlockData;
        int RemainingStringLength = 0;

        bool Done = false;
        public void transfer_file(string filename, string ip_addr)
        {
            Thread.Sleep(1000);
            try
            {
                Console.WriteLine("transfering file to :" + ip_addr);
                TcpClient tcpClient = new TcpClient(ip_addr, 7890);
                nStream = tcpClient.GetStream();
                writeImageData = new StreamWriter(nStream);


                //Change the filename here. If you change the file type, 
                //you must change it on the Server Project too.

                FileStream fs = File.OpenRead(filename);
                byte[] ImageData = new byte[fs.Length];
                fs.Read(ImageData, 0, ImageData.Length);

                Base64ImageData = Convert.ToBase64String(ImageData);

                int startIndex = 0;

                Console.WriteLine("Transfering Data...");

                while (Done == false)
                {
                    while (startIndex < Base64ImageData.Length)
                    {
                        try
                        {
                            BlockData = Base64ImageData.Substring(startIndex, 100);
                            writeImageData.WriteLine(BlockData);
                            writeImageData.Flush();
                            startIndex += 100;
                        }
                        catch (Exception)
                        {
                            RemainingStringLength = Base64ImageData.Length - startIndex;
                            BlockData = Base64ImageData.Substring(startIndex, RemainingStringLength);
                            writeImageData.WriteLine(BlockData);
                            writeImageData.Flush();
                            Done = true;
                            break;
                        }
                    }
                }
                writeImageData.Close();
                tcpClient.Close();
                fs.Close();
                fs.Dispose();
                nStream.Close();
                nStream.Dispose();
                nStream = null;
                tcpClient.Close();
                ImageData = null;
                Base64ImageData = null;
                Console.WriteLine("Transfer Complete");
                MessageBox.Show("File Transfer to " + ip_addr + " is Completed");
            }
            catch (Exception er)
            {
                Console.WriteLine("Unable to connect to server");
                MessageBox.Show("Unable to connect to server");
                Console.WriteLine(er.Message);
            }
        }
    }
    #endregion

    /*File Reciever*/
    #region file_transer

    class Recieve_File
    {
        NetworkStream nStream;
        StreamReader readImageData;

        StringBuilder BlockData = new StringBuilder();
        bool Done = false;

        public void recieve_file(string path)
        {

            string host = Dns.GetHostName();
            Console.WriteLine("Host Name = " + host);
            IPHostEntry localip = Dns.GetHostByName(host);

            Console.WriteLine("IPAddress = " + localip.AddressList[0].ToString());

            IPAddress ipAddress = localip.AddressList[0];
            TcpListener tcpListener = new TcpListener(ipAddress, 7890);
            tcpListener.Start();

            Console.WriteLine("Waiting for resposne . . .");


            TcpClient tcpClient = null;

            while (tcpClient == null)
            {
                tcpClient = tcpListener.AcceptTcpClient();
            }

            Console.WriteLine("Connection Made");
            nStream = tcpClient.GetStream();
            readImageData = new StreamReader(nStream);

            string data;

            while (Done == false)
            {
                while ((data = readImageData.ReadLine()) != null)
                {
                    BlockData.Append(data);
                }

                Done = true;
            }
            byte[] byte_image = Convert.FromBase64String(BlockData.ToString());
            Console.WriteLine("->" + path + "<--");
            //path = "new.png";
            // Change File Name Here 
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            FileStream fs = new FileStream(path, FileMode.Create);
            fs.Write(byte_image, 0, byte_image.Length);
            fs.Flush();
            fs.Close();
            fs.Dispose();
            fs = null;
            Console.WriteLine("File has been recieved!");
            MessageBox.Show("Connection Made\n" + path + "\nFile has been recieved!");
            //System.Diagnostics.Process.Start("run.exe");

            readImageData.Close();
            tcpClient.Close();
            nStream.Dispose();
            tcpListener.Stop();
        }
    }

    #endregion
}