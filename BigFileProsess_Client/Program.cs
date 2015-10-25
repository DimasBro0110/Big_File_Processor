using System;
using System.IO;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BigFileProsess_Client
{
    class Processor
    {
        private TcpClient client;

        public Processor()
        {
            try
            {
                client = new TcpClient("127.0.0.5", 30);
                if (client.Connected)
                {
                    ServerConnection(client.GetStream());
                }
                else
                {
                    client.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private string TaskToDo(string task)
        {
            int hashsum = 1;
            if (task.Equals("closed -1"))
            {
                return "closed -1";
            }
            else
            {
                foreach (char ch in task.ToLower())
                {
                    hashsum *= ch;
                }
            }
            string res = Convert.ToString(hashsum) + ":" + task;

            return res; 
        }

        private string Resize(byte[] array)
        {
            byte[] cur;
            int i = 0;
            foreach (byte b in array)
            {
                if (b != '\0' && b!= '\n')
                {
                    i++;
                }
            }
            cur = new byte[i];
            i = 0;
            foreach (byte b in array)
            {
                if (b != '\0' && b!= '\n')
                {
                    cur[i] = b;
                    i++;
                }
            }
            return Encoding.ASCII.GetString(cur, 0, cur.Length);
        }

        private void ServerConnection(NetworkStream stream)
        {
            int RecievedSymbols = 0;
            byte[] recieved = new byte[2048];
            byte[] to_send = new byte[2048];
            string task = "";
            while ( (RecievedSymbols = stream.Read(recieved, 0, recieved.Length)) > 0 )
            {
                task = Resize(recieved);
                task = TaskToDo(task);
                Console.WriteLine(task);
                if (task.Equals("closed -1"))
                {
                    to_send = Encoding.ASCII.GetBytes(task);                   
                    stream.Write(to_send, 0, to_send.Length);
                    client.Close();
                    return;
                }
                else
                {
                    to_send = Encoding.ASCII.GetBytes(task);
                    stream.Write(to_send, 0, to_send.Length);
                }
               Array.Clear(to_send, 0, to_send.Length);
               Array.Clear(recieved, 0, recieved.Length);
            }
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            new Processor();
            Console.ReadKey();
        }
    }
}
