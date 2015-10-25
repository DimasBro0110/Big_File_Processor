using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BigFileProcess
{
    class Program
    {
        class Task_To_Do
        {

        }

        class Generator
        {
            private bool isDone;
            private string fileFrom;
            private static Queue<string> tasks;
            private static Table annagrams;
            private static Mutex task_mutex;
            private static Mutex angr_mutex;
            private static Mutex proc_mutex;
            private TcpListener tcpListener;

            public Generator(string path)
            {
                isDone = false;
                IPAddress adress;
                IPAddress.TryParse("127.0.0.5", out adress);
                tcpListener = new TcpListener(adress, 30);
                Console.WriteLine("Server launched, adress {0}, port {1}", adress.ToString(), 30);
                fileFrom = path;
                task_mutex = new Mutex();
                angr_mutex = new Mutex();
                proc_mutex = new Mutex();
                tasks = new Queue<string>();
                annagrams = new Table();
                Thread t = new Thread(Tasks_Getter);
                t.Start();
                Listener();
            }

            private void Listener()
            {
                tcpListener.Start();
                while (true)
                {
                    ThreadPool.QueueUserWorkItem(new WaitCallback(ClientAcception), tcpListener.AcceptTcpClient());
                }
            }

            private void ClientAcception(Object stateInfo)
            {
                TcpClient client = null;
                NetworkStream stream = null;
                try
                {
                    client = (TcpClient)stateInfo;
                    stream = client.GetStream();
                    Task_To_Process(stream);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Client Disconnected \n{0}", ex);
                    if (stream != null)
                    {
                        stream.Dispose();
                        stream.Close();
                    }
                    if (client != null)
                    {
                        stream.Dispose();
                        stream.Close();
                    }
                }
            }

            private string Resize(byte[] array)
            {
                byte[] cur;
                int i = 0;
                foreach (byte b in array)
                {
                    if (b != '\0' && b != '\n')
                    {
                        i++;
                    }
                }
                cur = new byte[i];
                i = 0;
                foreach (byte b in array)
                {
                    if (b != '\0' && b != '\n')
                    {
                        cur[i] = b;
                        i++;
                    }
                }
                return Encoding.ASCII.GetString(cur, 0, cur.Length);
            }

            private void Task_To_Process(NetworkStream stream)
            {
                int countReadBytes = 0;
                byte[] ReadBytes = new byte[2048];
                byte[] SendBytes = new byte[2048];
                string recieved = "";

                if (tasks.Count != 0)
                {
                    task_mutex.WaitOne();
                    SendBytes = Encoding.ASCII.GetBytes(tasks.Dequeue());
                    task_mutex.ReleaseMutex();

                    proc_mutex.WaitOne();
                    stream.Write(SendBytes, 0, SendBytes.Length);
                    proc_mutex.ReleaseMutex();
                }
                else
                {
                    SendBytes = Encoding.ASCII.GetBytes("closed -1");

                    proc_mutex.WaitOne();
                    stream.Write(SendBytes, 0, SendBytes.Length);
                    proc_mutex.ReleaseMutex();
                }

                Array.Clear(SendBytes, 0, SendBytes.Length);

                while ((countReadBytes = stream.Read(ReadBytes, 0, ReadBytes.Length)) > 0)
                {
                    recieved = Resize(ReadBytes);
                    if (!recieved.Equals("closed -1"))
                    {
                        string[] result = recieved.Split(':');
                        angr_mutex.WaitOne();
                        annagrams.setValues(Convert.ToInt32(result[0]), result[1]);                       
                        angr_mutex.ReleaseMutex();

                        Array.Clear(result, 0, result.Length);

                        if (tasks.Count != 0)
                        {
                            task_mutex.WaitOne();
                            SendBytes = Encoding.ASCII.GetBytes(tasks.Dequeue());
                            task_mutex.ReleaseMutex();
                        }
                        else
                        {
                            SendBytes = Encoding.ASCII.GetBytes("closed -1");
                        }

                        proc_mutex.WaitOne();
                        stream.Write(SendBytes, 0, SendBytes.Length);
                        proc_mutex.ReleaseMutex();

                        Array.Clear(ReadBytes, 0, ReadBytes.Length);
                        Array.Clear(SendBytes, 0, SendBytes.Length);
                    }
                    else
                    {
                        SendBytes = Encoding.ASCII.GetBytes("closed -1");

                        stream.Write(SendBytes, 0, SendBytes.Length);
                    }
                }
            }

            private void Tasks_Getter()
            {
                StreamReader sr = null;
                while (!isDone)
                {
                    try
                    {
                        string task_str = "";
                        Encoding enc = Encoding.GetEncoding(1251);
                        sr = new StreamReader(fileFrom, enc);
                        while (sr.Peek() >= 0)
                        {
                            task_str = sr.ReadLine();
                            tasks.Enqueue(task_str);
                        }
                        isDone = true;
                        Console.WriteLine("Done {0}", tasks.Count);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                    finally
                    {
                        if (sr != null)
                        {
                            sr.Dispose();
                            sr.Close();
                            isDone = true;
                        }
                    }
                }
            }

            ~Generator()
            {
                tasks.Clear();
            }
        }

        static void Main(string[] args)
        {
            int max = Environment.ProcessorCount * 2;
            ThreadPool.SetMaxThreads(max, max);
            ThreadPool.SetMinThreads(2, 2);
            new Generator(@"C:\Architect\file.txt");
            Console.ReadKey();
        }
    }

    class Table
    {
        private Dictionary<int, LinkedList<string>> table;

        public Table()
        {
            table = new Dictionary<int, LinkedList<string>>();
        }

        public void setValues(int hash, string word)
        {
            if (this.table.ContainsKey(hash))
            {
                this.table[hash].AddLast(word);
            }
            else
            {
                LinkedList<string> lst = new LinkedList<string>();
                lst.AddLast(word);
                this.table.Add(hash, lst);
            }
        }
    }
}
