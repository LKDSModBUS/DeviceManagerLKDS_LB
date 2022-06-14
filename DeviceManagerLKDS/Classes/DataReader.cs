﻿using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace DeviceManagerLKDS.Classes
{
    public class DataReader
    {
        public string inputBytes;
        public string outputBytes;

        public static string log_conStatus;
        public static string log_input;
        public static string log_output;

        public byte[] setOfBytes;
        public List<byte> rawData = new List<byte>();
        public List<byte[]> bytePackets = new List<byte[]>();
        
        const int checker = 0x01;

        object locker = new object();

        public SerialPort port = null;
        public DataReader(string portName)//, byte[] data)
        {
            try
            {
                port = new SerialPort(portName, 19200, Parity.None, 8, StopBits.One);
                port.Handshake = Handshake.None;


                port.DataReceived += new SerialDataReceivedEventHandler(PackageSeeker);
                port.Open();
                
                log_conStatus = $"\nОткрыто соединение с портом {portName}";
            }
            catch
            {
                log_conStatus = $"\nСоединение с портом {portName} уже открыто";
            }
        }
        public void Send(byte[] data)
        {
            if (!port.IsOpen)
                return;
            byte[] CRC = new byte[2];

            CRC = ModbusCRC16Calc(data, data.Length - 2);
            data[data.Length - 2] = CRC[CRC.Length - 2];
            data[data.Length - 1] = CRC[CRC.Length - 1];
            port.Write(data, 0, data.Length);

            inputBytes = ByteToString(data).ToUpper();
        }
       // public void Close() => port.Close();

        /*public static string DeviceSeeker()
        {
            string bits = "";
            string outputBits = "";

            foreach (byte[] set in bytePackets)
            {
                int[] BIT = new int[(set.Length - 2) * 8];

                for (int i = 0, k = 0; i < BIT.Length; k++)
                {
                    for (int j = 0; j < 8; j++, i++)
                    {
                        BIT[i] = (set[k] >> j) & 0x01;
                        bits += BIT[i];
                    }
                    char[] bitsChar = bits.ToCharArray();
                    Array.Reverse(bitsChar);
                    outputBits += new string(bitsChar) + " ";
                    bits = "";

                }
            }
            return outputBits.Trim();




        }*/
        
        public string ByteToString(byte[] bytes)
        {
            StringBuilder hex = new StringBuilder(bytes.Length * 2);
            foreach (byte b in bytes)
            {
                hex.AppendFormat("{0:x2}", b).Append(" ");
            }

            return hex.ToString();
        }

        public static byte[] ModbusCRC16Calc(byte[] data, int len)
        {
            byte[] CRC = new byte[2];
            ushort Register = 0xFFFF;
            ushort Polynom = 0xA001;

            for (int i = 0; i < len; i++)
            {
                Register = (ushort)(Register ^ data[i]);
                for (int j = 0; j < 8; j++)
                    if ((ushort)(Register & checker) == 1)
                    {
                        Register = (ushort)(Register >> 1);
                        Register = (ushort)(Register ^ Polynom);
                    }
                    else
                        Register = (ushort)(Register >> 1);
            }

            CRC[1] = (byte)(Register >> 8);
            CRC[0] = (byte)(Register & 0x00FF);

            return CRC;
        }
        
        public void PackageSeeker(object s, EventArgs e)
        {
            lock (locker)
            {
                int len = port.BytesToRead;
                byte[] bytes = new byte[len];

                if (port.IsOpen)
                {
                    port.Read(bytes, 0, len);
                    rawData.AddRange(bytes);
                    outputBytes += ByteToString(bytes).ToUpper();
                }
                try
                {
                    if ((rawData.Count > 2) && (rawData[2] + 2 <= rawData.Count - 3))
                    {
                        for (int i = 0; i < rawData.Count; i++)
                        {
                            if (rawData[i] == checker && rawData[i + 1] == 0x04)//0x01 и 0x04 костыли
                            {
                                setOfBytes = new byte[rawData[2] + 2];

                                rawData.CopyTo(3, setOfBytes, 0, rawData[2] + 2);
                                bytePackets.Add(setOfBytes);
                                rawData.Clear();
                            }
                        }

                        log_input = "\n\n[" + DateTime.Now + "-" + DateTime.Now.Millisecond + "] >: " + inputBytes;
                        log_output = "\n[" + DateTime.Now + "-" + DateTime.Now.Millisecond + "] <: " + outputBytes;
                    }
                }
                catch { }
            }
        }
        public void Disconnect()
        {
            port.Close();
            port.Dispose();

            //log_conStatus = $"\nЗакрыто соединение с портом {portName}";
        }

    }
}
