﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AnonymoMediaClientCLI
{
    internal class Program
    {
        static string last_message = "";
        static void Main(string[] args)
        {
            TcpClient client = new TcpClient();
            client.Connect("127.0.0.1", 8085);
            NetworkStream stream = client.GetStream();
            Thread listener = new Thread(() =>
            {
                bool is_packet_data = false;
                int packet_length = 0;
                List<byte> byte_list = new List<byte>();
                while (true)
                {
                    if (packet_length <= 0)
                    {
                        is_packet_data = false;
                        if (byte_list.Count() > 0)
                        {
                            last_message = Encoding.ASCII.GetString(byte_list.ToArray());
                            //Console.WriteLine(last_message);
                            //output.write(PacketResponse(ToByteArray(byte_list)));
                        }
                    }
                    if (is_packet_data)
                    {
                        byte_list.Add((byte)stream.ReadByte());
                        packet_length--;
                    }
                    else
                    {
                        is_packet_data = true;
                        byte_list.Clear();
                        int a = stream.ReadByte();
                        packet_length = stream.ReadByte() + 256 * a;
                    }
                }
            });
            listener.Start();
            while (true)
            {
                string message = Console.ReadLine();
                string response = SendString(stream, message);
                Console.WriteLine(response);
            }
            Console.ReadLine();
        }

        static string SendString(NetworkStream stream, string msg)
        {
            if (msg.Length > 255 * 256 + 255 * 1)
            {
                Console.WriteLine("Message is too big!");
                return null;
            }
            byte[] packet = new byte[msg.Length + 2];
            int a = msg.Length / 255;
            int b = msg.Length % 255;
            packet[0] = (byte)a;
            packet[1] = (byte)b;
            for (int i = 0; i < msg.Length; i++)
            {
                packet[i + 2] = (byte)msg[i];
            }
            stream.Write(packet, 0, packet.Length);

            string method = msg.Split(' ')[0];
            string response = "";
            while (true)
            {
                //Console.WriteLine(last_message.Split(' ')[0] + "    " + method);
                if(last_message.Split(' ')[0] == method)
                {
                    response = last_message;
                    break;
                }
            }
            return response;
        }
    }
}
