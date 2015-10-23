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
            private static Dictionary<int, string> annagrams;
            private static Mutex task_mutex;
            private static Mutex angr_mutex;
            private static Mutex proc_mutex;
            private TcpListener tcpListener;

            public Generator(string path)
            {
                isDone = false;
                fileFrom = path;
                task_mutex = new Mutex();
                angr_mutex = new Mutex();
                proc_mutex = new Mutex();
                tasks = new Queue<string>();
                annagrams = new Dictionary<int, string>();
                Thread t = new Thread(Tasks_Getter);
                t.Start();
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
                    Console.WriteLine(ex);
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

            private void Task_To_Process(NetworkStream stream)
            {
                int countReadBytes = 0;
                byte[] ReadBytes = new byte[2048];
                string recieved = "";

                task_mutex.WaitOne();
                byte[] SendBytes = Encoding.ASCII.GetBytes(tasks.Dequeue());
                task_mutex.ReleaseMutex();

                proc_mutex.WaitOne();
                stream.Write(SendBytes, 0, SendBytes.Length);
                proc_mutex.ReleaseMutex();

                Array.Clear(ReadBytes, 0, ReadBytes.Length);

                while ((countReadBytes = stream.Read(ReadBytes, 0, ReadBytes.Length)) > 0)
                {
                    recieved = Encoding.ASCII.GetString(ReadBytes, 0, ReadBytes.Length);
                    string[] result = recieved.Split(':');

                    angr_mutex.WaitOne();
                    annagrams.Add(Convert.ToInt32(result[0]), result[1]);
                    angr_mutex.ReleaseMutex();

                    Array.Clear(result, 0, result.Length);

                    task_mutex.WaitOne();
                    ReadBytes = Encoding.ASCII.GetBytes(tasks.Dequeue());
                    task_mutex.ReleaseMutex();

                    proc_mutex.WaitOne();
                    stream.Write(ReadBytes, 0, ReadBytes.Length);
                    proc_mutex.ReleaseMutex();

                    Array.Clear(ReadBytes, 0, ReadBytes.Length);
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
                        sr = new StreamReader(fileFrom);
                        while (sr.Peek() >= 0)
                        {
                            task_str = sr.ReadLine();
                            task_mutex.WaitOne();
                            tasks.Enqueue(task_str);
                            task_mutex.ReleaseMutex();
                        }
                        isDone = true;
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
            }
        }

        static void Main(string[] args)
        {
            Console.ReadKey();
        }
    }
}
