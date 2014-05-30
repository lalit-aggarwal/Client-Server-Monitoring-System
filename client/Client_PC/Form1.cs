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
using System.Threading;
using System.Diagnostics;

namespace Client_PC
{
    public partial class Form1 : Form
    {
        TcpClient tcpclnt = new TcpClient();
        private int hostPort = -1;
        Thread wait = null;
        public delegate void setOutput();
        private String rcvMsg = null;
        Boolean hasNewData = false;
        public string s;
        string file;
        private string currentNotice;

        public Form1()
        {
            InitializeComponent();
            cmd_send.Enabled = false;
            textBox3.Enabled = false;
            textBox4.Enabled = false;
            cmd_dis.Enabled = false;
            button1.Enabled = false;

        }

        private const int CP_NOCLOSE_BUTTON = 0x200;
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams myCp = base.CreateParams;
                myCp.ClassStyle = myCp.ClassStyle | CP_NOCLOSE_BUTTON;
                return myCp;
            }
        }

        public void setOut()
        {
            if (hasNewData && rcvMsg != null)
            {
                output.Text += "\nRecieved data : " + rcvMsg;
                rcvMsg = null;
                hasNewData = false;
            }
        }

        private void cmd_con_Click(object sender, EventArgs e)
        {
            if (textBox1.Text == String.Empty || textBox2.Text == String.Empty)
                MessageBox.Show("Please provide Server IP and Port");
            else
            {
                try
                {
                    output.Text += "Connecting . . ." + Environment.NewLine;
                    hostPort = int.Parse(textBox2.Text);
                    tcpclnt.Connect(textBox1.Text.ToString(), hostPort);
                    leetSocket1.ServerIpAsDNS = textBox1.Text;
                    leetSocket1.AutoConnectSystem = true;
                    output.Text += "Connected" + Environment.NewLine;
                    cmd_send.Enabled = true;
                    cmd_con.Enabled = false;
                    textBox1.Enabled = false;
                    textBox2.Enabled = false;
                    textBox3.Enabled = false;
                    textBox4.Enabled = true;
                    cmd_dis.Enabled = true;
                    wait = new Thread(new ThreadStart(waitForData));
                    wait.Start();

                }
                catch (Exception error)
                {
                    MessageBox.Show("Server has not started yet");
                }
            }
        }
        private void waitForData()
        {
            try
            {
                NetworkStream read = tcpclnt.GetStream();
                while(read.CanRead)
                {
                    byte[] buffer = new byte[64];
                    read.Read(buffer, 0, buffer.Length);
                    s = new ASCIIEncoding().GetString(buffer);
                    System.Console.WriteLine("Recieved data:" + new ASCIIEncoding().GetString(buffer));
                    rcvMsg = new ASCIIEncoding().GetString(buffer) + "\n";
                    hasNewData = true;
                    bool f = false;
                    f = rcvMsg.Contains("##");
                    bool comand = false;
                    comand = rcvMsg.Contains("*+*-");
                    /*File receive*/
                    if (f)
                    {
                        string d = "##";
                        rcvMsg = rcvMsg.TrimStart(d.ToCharArray());
                        int lastLt = rcvMsg.LastIndexOf("|");
                        rcvMsg = rcvMsg.Substring(0, lastLt);
                        NetworkStream ns = tcpclnt.GetStream();
                        if (ns.CanWrite)
                        {
                            string dataS = "^^Y";
                            byte[] bf = new ASCIIEncoding().GetBytes(dataS);
                            ns.Write(bf, 0, bf.Length);
                            ns.Flush();
                        }
                        try
                        {
                            new Recieve_File().recieve_file(rcvMsg);
                        }
                        catch (Exception ec)
                        {
                            System.Console.WriteLine(ec.Message);
                        }
                    }
                    else if (rcvMsg.Contains("%%"))
                    {
                        new Transmit_File().transfer_file(file, textBox1.Text);
                    }
                    
                    /*Command-shutdown/restart/logoff*/
                    else if (comand)
                    {
                        string com = "*+*-";
                        rcvMsg = rcvMsg.TrimStart(com.ToCharArray());
                        execute_command(rcvMsg);
                    }
                    else
                    {
                        this.Invoke(new setOutput(setOut));
                        Thread.Sleep(1000);
                    }
                }
            }
            catch (Exception ex)
            {
                wait.Abort();
                output.Text += "Error..... " + ex.StackTrace;
            }
        }
        public delegate void addNotification();
        private void addNotice()
        {
            if (currentNotice != null)
            {
                output.Text += currentNotice;
                output.Text += Environment.NewLine;
                currentNotice = null;
            }
        }
        private void execute_command(String Comande)
        {
            if (Comande.CompareTo("shutdown") == 1)
            {
                System.Diagnostics.Process.Start("shutdown", "-s");
                currentNotice = "Shutdown initiating . . .";
                this.Invoke(new addNotification(addNotice));
                NetworkStream ns = tcpclnt.GetStream();
                String notify = "++--Shutdown Completed . . .";
                if (ns.CanWrite)
                {
                    byte[] bf = new ASCIIEncoding().GetBytes(notify);
                    ns.Write(bf, 0, bf.Length);
                    ns.Flush();
                }
            }

            else if (Comande.CompareTo("restart") == 1)
            {
                System.Diagnostics.Process.Start("shutdown", "-r");
                currentNotice = "Restart initiating . . .";
                this.Invoke(new addNotification(addNotice));
                NetworkStream ns = tcpclnt.GetStream();
                String notify = "++--Restart Completed . . .";
                if (ns.CanWrite)
                {
                    byte[] bf = new ASCIIEncoding().GetBytes(notify);
                    ns.Write(bf, 0, bf.Length);
                    ns.Flush();
                }
            }
            else if (Comande.CompareTo("logoff") == 1)
            {
                System.Diagnostics.Process.Start("shutdown", "-l");
                currentNotice = "Logoff initiating . . .";
                this.Invoke(new addNotification(addNotice));
                NetworkStream ns = tcpclnt.GetStream();
                String notify = "++--Logoff Completed . . .";
                if (ns.CanWrite)
                {
                    byte[] bf = new ASCIIEncoding().GetBytes(notify);
                    ns.Write(bf, 0, bf.Length);
                    ns.Flush();
                }
            }
            else if (Comande.CompareTo("abort") == 1)
            {
                System.Diagnostics.Process.Start("shutdown", "-a");
                currentNotice = "Abort initiating . . .";
                this.Invoke(new addNotification(addNotice));
                NetworkStream ns = tcpclnt.GetStream();
                String notify = "++--Abort Completed . . .";
                if (ns.CanWrite)
                {
                    byte[] bf = new ASCIIEncoding().GetBytes(notify);
                    ns.Write(bf, 0, bf.Length);
                    ns.Flush();
                }
            }
            tcpclnt.Close();
            Thread.Sleep(7000);
            Application.Restart();
        }


        private void cmd_send_Click_1(object sender, EventArgs e)
        {
            if (textBox3.Enabled == true && textBox3.Text == String.Empty)
            {
                MessageBox.Show("PLEASE ENTER SOME TEXT");
            }
            else if (textBox3.Enabled == true)
            {
                try
                {
                    String str = textBox3.Text.ToString();
                    output.Text += "\nSent data : " + str;
                }
                catch (Exception ex)
                {
                    wait.Abort();
                    output.Text += "Error..... " + ex.StackTrace;
                }
                NetworkStream ns = tcpclnt.GetStream();
                String data = "";
                IPAddress ipclient = Dns.GetHostByName(Dns.GetHostName()).AddressList[0];
                data = "--++" + ipclient.ToString() + " : " + textBox3.Text;
                if (ns.CanWrite)
                {
                    byte[] bf = new ASCIIEncoding().GetBytes(data);
                    ns.Write(bf, 0, bf.Length);
                    ns.Flush();
                }
                textBox3.Clear();
            }
            else if (textBox4.Enabled == true && textBox4.Text != String.Empty)
            {
                NetworkStream ns = tcpclnt.GetStream();
                String data = "";
                data = textBox4.Text;
                if (ns.CanWrite)
                {
                    byte[] bf = new ASCIIEncoding().GetBytes(data);
                    ns.Write(bf, 0, bf.Length);
                    ns.Flush();
                }
                textBox4.Enabled = false;
                textBox3.Enabled = true;
                button1.Enabled = true;
            }
            else if (textBox4.Enabled == true && textBox4.Text == String.Empty)
            {
                MessageBox.Show("PLEASE ENTER YOUR ROLL NO.");
            }
        }

        private void cmd_dis_Click(object sender, EventArgs e)
        {
            if (wait != null)
            {
                wait.Abort();
                //read.Close(2000);
            }

            IPAddress ipclient = Dns.GetHostByName(Dns.GetHostName()).AddressList[0];
            String ipclnt = "+@@+" + ipclient.ToString();
            NetworkStream ns = tcpclnt.GetStream();
            if (ns.CanWrite)
            {
                byte[] bf = new ASCIIEncoding().GetBytes(ipclnt);
                ns.Write(bf, 0, bf.Length);
                ns.Flush();
            }
            tcpclnt.Close();
            // read.Close();
            Application.Exit();
        }

        private void leetSocket1_OnReceiveCompletedDataEVENT_1(object value, byte[] bArray)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new MethodInvoker(delegate() { leetSocket1_OnReceiveCompletedDataEVENT_1(value, bArray); }));
                return;
            }
            int rec = (int)value;
            if (rec == 1000)
            {
                timer1.Enabled = true;
                timer1.Start();
            }
            else if (rec == 4000)
            {
                timer1.Stop();
            }
            else if (rec == 2000)
            {
                timer2.Start();
            }
            else if (rec == 3000)
            {
                timer2.Stop();
            }
            else
                processes[rec].Kill();
        }

        Process[] processes;
        int count = 0;
        public void getProcesses()
        {
            processes = Process.GetProcesses();
            if (count != processes.Length)
            {
                leetSocket1.sendObject("Clear");
                for (int i = 0; i < processes.Length; i++)
                {
                    leetSocket1.sendObject(processes[i].ProcessName + Environment.NewLine);
                }
                count = processes.Length;
            }
        }

        private Bitmap getScreen()
        {
            Size s = Screen.PrimaryScreen.Bounds.Size;
            Bitmap bmp = new Bitmap(s.Height, s.Width);
            Graphics g = Graphics.FromImage(bmp);
            g.CopyFromScreen(0, 0, 0, 0, s);
            return bmp;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            getProcesses();
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            leetSocket1.sendObject(getScreen());
        }

        private void button1_Click(object sender, EventArgs e)
        {
            FileDialog fDg = new OpenFileDialog();
            if (fDg.ShowDialog() == DialogResult.OK)
            {
                file = fDg.FileName;

                string[] f = file.Split('\\');

                string fnm = f[f.Length - 1];
                fnm = "!!" + fnm + "}";
                NetworkStream ns = tcpclnt.GetStream();
                if (ns.CanWrite)
                {
                    byte[] bf = new ASCIIEncoding().GetBytes(fnm);
                    ns.Write(bf, 0, bf.Length);
                    ns.Flush();
                }
            }
        }
    }

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

}
