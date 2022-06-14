using DeviceManagerLKDS.Classes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Timers;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using static DeviceManagerLKDS.Classes.Enums;

namespace DeviceManagerLKDS
{
    public partial class Form1 : Form
    {
        //
        // SOME VARIABLES
        //

       //StreamWriter logWriter = new StreamWriter("C:\\DeviceManagerLKDS\\DeviceManagerLKDS\\DeviceManagerLKDS\\Logs\\Log.txt"); // ПУТЬ
        int[] connectedDevices = new int[64];
        byte[] query = new byte[]
                                   {
                                        0x01,
                                        0x04,
                                        0x1F,
                                        0xF0,
                                        0x00,
                                        0x10,
                                        0,
                                        0
                                   };
        byte[] query2 = new byte[]
                                   {
                                        0x01,
                                        0x04,
                                        0x00,
                                        0x00,
                                        0x00,
                                        0x73,
                                        0,
                                        0
                                   };
        byte[] query3 = new byte[]
                                   {
                                        0x01,
                                        0x04,
                                        0x12,
                                        0x00, // НУЖНО 0x00
                                        0x00,
                                        0x01,
                                        0,
                                        0
                                   };
        byte[] CRC = new byte[2];
        byte[] clone = new byte[34];
        DataReader dr = null;

        //
        // MAIN
        //
     


        public Form1()
        {
            InitializeComponent();
            getCOMports();
        }
        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void button4_Click(object sender, EventArgs e)
        {
            labelTimer.Text = i++.ToString();
            dr.Send(query);
            do
            {
                System.Threading.Thread.Sleep(10);
            } while (dr.setOfBytes == null);
       
            try
            {
                if (clone[clone.Length - 1] != dr.setOfBytes[dr.setOfBytes.Length - 1] || clone[clone.Length - 2] != dr.setOfBytes[dr.setOfBytes.Length - 2])
                {
                    for (int i = 0; i < 256; i++)
                    {
                        int b = dr.setOfBytes[(int)(i / 8)];
                        if ((b & (1 << (i % 8))) != 0)
                            rtbLog.Text += $"Address {i + 7} set\n";
                    }
                }
            }
            catch
            {
            }

        }

        private void button1_Click(object sender, EventArgs e)
        {
            string title = "Page " + (mainTabControl.TabCount + 1).ToString();
            TabPage myTabPage = new TabPage(title);
            mainTabControl.TabPages.Add(myTabPage);
        }

        //
        // CAN
        //


        private void bConnectPort_Click(object sender, EventArgs e)
        {
            bDisconnectPort.Enabled = true;
            try
            {
                for (int i = 0; i < cbConnectedPorts.Items.Count; i++)
                {
                    if (i == cbConnectedPorts.SelectedIndex)
                    {
                        dr = new DataReader(cbConnectedPorts.SelectedItem.ToString());

                        timer1.Start();
                    }
                }
                if (cbConnectedPorts.Text == "")
                {

                }
                rtbLog.Text += DataReader.log_conStatus;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Message", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            //bytePackets = DataReader.setData();
        }

        private void bDisconnectPort_Click(object sender, EventArgs e)
        {
            dr.Disconnect();
            bDisconnectPort.Enabled = false;
            rtbLog.Text += $"\nСоединение с портом {cbConnectedPorts.SelectedItem} разорвано";
            timer1.Enabled = false;
        }

        // FUNCTIONS

        public void PrintLog()
        {
            rtbLog.Text += DataReader.log_input;
            rtbLog.Text += DataReader.log_output;
            rtbLog.SelectionStart = rtbLog.Text.Length;
            rtbLog.ScrollToCaret();
            //logWriter.AutoFlush = true;
           // logWriter.Write(rtbLog.Text);

        }
        int i = 0;

        void SendQuery(byte[] query)
        {
            dr.Send(query);
            while (dr.setOfBytes == null)
            {

            }
            Union16 val = new Union16();
            val.Byte0 = dr.setOfBytes[dr.setOfBytes.Length - 2];
            val.Byte1 = dr.setOfBytes[dr.setOfBytes.Length - 1];

            this.liftControl1.SetData(dr.setOfBytes);
            rtbLog.Text += DataReader.log_input + DataReader.log_output;
            dr.outputBytes = "";
            rtbLog.SelectionStart = rtbLog.Text.Length;
            rtbLog.ScrollToCaret();

            //logWriter.AutoFlush = true;
            //logWriter.Write(rtbLog.Text);
        }
        private void timer1_Tick(object sender, EventArgs e)
        {

            timer1.Stop();
            labelTimer.Text = i++.ToString();

            SendQuery(query2);

            rtbLog.Text += $"\nВыбранная вкладка{mainTabControl.TabPages[mainTabControl.SelectedIndex].Text}";

            timer1.Start();

        }

        private struct DEV_BROADCAST_HDR
        {
            //отключаем предупреждения компилятора для ошибки 0649
        #pragma warning disable 0649
            internal UInt32 dbch_size;
            internal UInt32 dbch_devicetype;
            internal UInt32 dbch_reserved;
            //включаем предупреждения компилятора для ошибки 0649
        #pragma warning restore 0649
        };

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            if (m.Msg == 0x0219)
            {
                DEV_BROADCAST_HDR dbh;
                switch ((int)m.WParam)
                {
                    case 0x8000:
                        dbh = (DEV_BROADCAST_HDR)Marshal.PtrToStructure(m.LParam, typeof(DEV_BROADCAST_HDR));
                        if (dbh.dbch_devicetype == 0x00000003)
                        {
                            getCOMports();
                        }
                        break;
                    case 0x8004:
                        dbh = (DEV_BROADCAST_HDR)Marshal.PtrToStructure(m.LParam, typeof(DEV_BROADCAST_HDR));
                        if (dbh.dbch_devicetype == 0x00000003)
                        {

                            cbConnectedPorts.Text = "";
                            getCOMports();
                        }
                        break;
                }
            }
        }
        public void getCOMports()
        {
            try
            {
                string[] ports = SerialPort.GetPortNames();
                cbConnectedPorts.Items.Clear();
                cbConnectedPorts.Items.AddRange(ports);
                bDisconnectPort.Enabled = false;
                cbConnectedPorts.SelectedIndex = 0;
            }
            catch (Exception)
            {
                //MessageBox.Show("Нет доступных соединений");
            }
        }

        private void mainTabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            labelTimer.Text = mainTabControl.SelectedIndex.ToString();
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {

            dr?.Disconnect();
        }

        private void clearbutton_Click(object sender, EventArgs e)
        {
            rtbLog.Text = "";
        }

        private void statusStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {

        }
    }
}
