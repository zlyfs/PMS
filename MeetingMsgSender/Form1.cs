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
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MeetingMsgSender
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            DateTime lastTime = ReadTime("./data/lastTime.txt");
            WriteTime("./data/lastTime.txt", DateTime.Now);
            label2.Text = "上次检查时间：" + lastTime + " 本次检查时间：" + DateTime.Now;
            MsgContentBLL msgContentBLL = new MsgContentBLL();
            //查看最新收的信息
            var msgContent = msgContentBLL.CheckMsgContent(lastTime);
            foreach (var temp in msgContent)
            {
                if (SendMsg(temp)) 
                {
                    label1.Text = "Send: " + temp;
                }
                else label1.Text = "连接失败，放弃。";
            }        
        }

        private static bool SendMsg(string msgTxt)
        {
            string IPNum = "128.5.10.58";
            int IPPort = 2017;
            TcpClient tcpClient = new TcpClient();
            try
            {
                //tcpClient.Connect(IPAddress.Parse("170.0.0.78"), 2014);
                tcpClient.Connect(IPAddress.Parse(IPNum), IPPort);
            }
            catch
            {
                return false;
            }
            NetworkStream ntwStream = tcpClient.GetStream();
            if (ntwStream.CanWrite)
            {
                Byte[] bytSend = Encoding.UTF8.GetBytes(msgTxt);
                //Byte[] bytSend = Convert.(msgTxt);//ToHEX(msgTxt);
                ntwStream.Write(bytSend, 0, bytSend.Length);
                //ntwStream.Write(Convert.ToByte(msgTxt))
                Console.WriteLine("Send: " + msgTxt);
                WriteLog("./data/log.txt", "Send: " + msgTxt);

            }
            else
            {
                Console.WriteLine("无法写入数据流，请联系管理员");
                WriteLog("./data/log.txt", "无法写入数据流，请联系管理员");
                //MessageBox.Show("无法写入数据流，请联系管理员");

                ntwStream.Close();
                tcpClient.Close();

                return false;
            }

            ntwStream.Close();
            tcpClient.Close();
            return true;
        }

        /// <summary>
        /// 写日志
        /// </summary>
        /// <param name="str"></param>
        public static void WriteLog(string fullPath, string str)
        {
            try
            {
                StreamWriter sw = new StreamWriter(fullPath, true, Encoding.Default);
                sw.WriteLine(DateTime.Now + ":\t" + str);
                sw.Close();

            }
            catch (IOException ex)
            {
                throw ex;
            }
        }

        public static void WriteTime(string fullPath, DateTime str)
        {
            try
            {
                StreamWriter sw = new StreamWriter(fullPath, false, Encoding.Default);
                sw.WriteLine(str.ToString());
                sw.Close();

            }
            catch (IOException ex)
            {
                throw ex;
            }
        }

        public static DateTime ReadTime(string fullPath)
        {
            try
            {
                StreamReader sw = new StreamReader(fullPath, Encoding.Default);
                var temp = sw.ReadLine();
                sw.Close();
                return DateTime.Parse(temp);

            }
            catch (IOException ex)
            {
                throw ex;
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            timer1.Enabled = true;
        }
    }
}
