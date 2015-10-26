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
                    Console.WriteLine("Client Disconnected");
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
                    if (b != '\0')
                    {
                        i++;
                    }
                }
                cur = new byte[i];
                i = 0;
                foreach (byte b in array)
                {
                    if (b != '\0')
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

                        proc_mutex.WaitOne();
                        stream.Write(SendBytes, 0, SendBytes.Length);
                        proc_mutex.ReleaseMutex();

                        annagrams.DownloadToFile(@"C:\Architect\analyse.txt"); //передаем путь к файлу, куда выводим найденный аннаграмы
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
                        sr = new StreamReader(fileFrom, Encoding.Default); //для Windows справедливо как Default т.е 1251 , так и utf-8.
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

            private void ToFileDownloader()
            {      
                annagrams.DownloadToFile(@"C:\Architect\analyse.txt");  
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
            if (args.Length > 0)
            {
                if (File.Exists(args[0]))
                {
                    new Generator(args[0]);
                }
                else
                {
                    string input_file = "";
                    do
                    {
                        Console.WriteLine("Введите путь к обрабатываемому файлу : ");
                        input_file = Console.ReadLine();
                    }
                    while (!File.Exists(input_file));
                    new Generator(input_file);
                } 
            }
            else
            {
                string input_file = "";
                do
                {
                    Console.WriteLine("Введите путь к обрабатываемому файлу : ");
                    input_file = Console.ReadLine();
                }
                while (!File.Exists(input_file));
                new Generator(input_file);
            }
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

        public int size()
        {
            return table.Count;
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

        public void DownloadToFile(string output)
        {
            if (table.Count != 0)
            {
                StreamWriter sw = null;
                try
                {
                    sw = new StreamWriter(output, false, Encoding.Default);
                    foreach (KeyValuePair<int, LinkedList<string>> cur in table)
                    {
                        if (cur.Value.Count() >= 1)
                        {
                            foreach (string word in cur.Value)
                            {
                                sw.Write(word + " ");
                            }
                            sw.WriteLine();
                        }
                    }
                }
                catch (Exception ex)
                {
                }
                finally
                {
                    if (sw != null)
                    {
                        sw.Dispose();
                        sw.Close();
                    }
                }
            }
        }
    }
}
