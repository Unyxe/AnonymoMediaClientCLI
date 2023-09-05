using System;
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
                            Doer(last_message);
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
        static void Doer(string message)
        {
            string[] args = message.Split(' ');
            string method = args[0];
            switch (method)
            {
                case "auth":
                    {
                        string auth_str = args[1];
                        Console.WriteLine($"Auth key recieved: [{auth_str}].");
                    }
                    break;
                case "new_msg":
                    {
                        string chat_id = args[1];
                        string msg_string = args[2];
                        string sender = args[3];
                        Console.WriteLine($"New message in chat [{chat_id}] from [{sender}]: '{msg_string}'.");
                    }
                    break;
                case "new_chat":
                    {
                        args = FromBase64(args[1]).Split(' ');
                        string chat_id = args[0];
                        string chat_display_name = args[1];
                        Console.WriteLine($"New chat created: {chat_display_name} [{chat_id}].");
                    }
                    break;
                case "remove_chat":
                    {
                        string chat_id = args[1];
                        Console.WriteLine($"Chat removed: [{chat_id}].");
                    }
                    break;
                case "member_delete":
                    {
                        string chat_id = args[1];
                        Console.WriteLine($"You were deleted from the chat [{chat_id}].");
                    }
                    break;
                case "chats_response":
                    {
                        
                        args = FromBase64(args[1]).Split('\n');
                        if (args[0] == "" && args.Length == 1)
                        {
                            args = new string[0];
                        }
                        Console.WriteLine("_____________YOUR_CHATS_____________");
                        string[][] chats = new string[args.Length][];
                        for(int i = 0; i < chats.Length; i++)
                        {
                            chats[i] = args[i].Split(' ');
                        }

                        for(int i = 0; i < chats.Length; i++)
                        {
                            string chat_id = chats[i][0];
                            string chat_display_name = chats[i][1];
                            Console.WriteLine($"{i+1}: {chat_display_name} [{chat_id}].");
                        }
                        Console.WriteLine("____________________________________");
                    }
                    break;
                case "messages_response":
                    {
                        string chat_id = args[2];
                        args = FromBase64(args[1]).Split('\n');
                        if (args[0] == "" && args.Length == 1)
                        {
                            args = new string[0];
                        }
                        Console.WriteLine("_____________MESSAGE_HISTORY_____________");
                        Console.WriteLine("For chat: " + chat_id);
                        string[][] messages = new string[args.Length][];
                        for (int i = 0; i < messages.Length; i++)
                        {
                            messages[i] = args[i].Split(' ');
                        }

                        for (int i = 0; i < messages.Length; i++)
                        {
                            string sender = messages[i][0];
                            string msg = FromBase64(messages[i][1]);
                            Console.WriteLine($"{i + 1}: {sender}: '{msg}'.");
                        }
                        Console.WriteLine("_________________________________________");
                    }
                    break;
                case "members_response":
                    {
                        string chat_id = args[2];
                        args = FromBase64(args[1]).Split(' ');
                        if (args[0] == "" && args.Length == 1)
                        {
                            args = new string[0];
                        }
                        Console.WriteLine("_____________MEMBER_LIST_____________");
                        Console.WriteLine("For chat: " + chat_id);
                        for (int i = 0; i < args.Length; i++)
                        {
                            string member_name = args[i];
                            Console.WriteLine($"{i + 1}: {member_name}");
                        }
                        Console.WriteLine("_____________________________________");
                    }
                    break;
            }
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


        public static string ToBase64(string input)
        {
            return Convert.ToBase64String(Encoding.ASCII.GetBytes(input));
        }

        public static string ToBase64FromByte(byte[] input)
        {
            return Convert.ToBase64String(input);
        }
        public static byte[] FromBase64ToByte(string input)
        {
            return Convert.FromBase64String(input);
        }

        public static string FromBase64(string input)
        {
            return Encoding.Default.GetString(Convert.FromBase64String(input));
        }
    }
}
