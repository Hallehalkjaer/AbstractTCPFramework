using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace AbstractTCPFramework
{
    public abstract class AbstractTCPServer
    {
        private int PORT = 7007;
        private bool STOP = false;
        private bool stopShutdown = false;


        TraceSource ts = new TraceSource("TCPServer");
        /// <summary>
        /// Creates a new server and a new "StopServer"
        /// can handle multiple clients at once
        /// </summary>
        public void Start()
        {
            
            
            ts.Switch = new SourceSwitch("TCPServerLog", "All");

            TraceListener consListener = new ConsoleTraceListener();
            ts.Listeners.Add(consListener);
            TraceListener xmListener = new XmlWriterTraceListener(new StreamWriter("TCPServerLog.xml"));
            xmListener.Filter = new EventTypeFilter(SourceLevels.Warning);
            ts.Listeners.Add(xmListener);
            TraceListener txtListener = new TextWriterTraceListener(new StreamWriter("TCPServerLog.txt"));
            ts.Listeners.Add(txtListener);

            XmlDocument xml = new XmlDocument();
            xml.Load("ServerConfig.xml");
            XmlNode? nameNode = xml.DocumentElement.SelectSingleNode("ServerName");
            if (nameNode is not null)
            {
                string nameStr = nameNode.InnerText.Trim();
                ts.TraceEvent(TraceEventType.Information, 496, $"ServerName: {nameStr}");
                Console.WriteLine("");
            }

            XmlNode? portNode = xml.DocumentElement.SelectSingleNode("ServerPort");
            if (portNode is not null)
            {
                string portStr = portNode.InnerText.Trim();
                int portNumber = Convert.ToInt32(portStr);
            }


            TcpListener serverListener = new TcpListener(IPAddress.Loopback, PORT);
            serverListener.Start();

            ts.TraceEvent(TraceEventType.Information, 496, $"server started on port: {PORT}");

            //Console.WriteLine($"server started on port ",PORT);
            Task.Run(StopServer);

            while (!STOP)
            {
                if (serverListener.Pending())
                {
                    TcpClient client = serverListener.AcceptTcpClient();
                    ts.TraceEvent(TraceEventType.Information,496,"Client accepted ready for client work");
                    Task.Run(() =>
                    {
                        DoOneClient(client);
                    });
                }
                else
                {
                    Thread.Sleep(1000);
                    ts.TraceEvent(TraceEventType.Information,496,"no pending clients, waiting for new client");
                }
                
                
            }
            ts.Close();
        }
        /// <summary>
        /// StopServer is used to shut down the original server
        /// when a client is connecting to stop sever it will call SetStop();
        /// </summary>
        public void StopServer()
        {
            /*ts.Switch = new SourceSwitch("TCPStopServerLog", "All");
            TraceListener consListener = new ConsoleTraceListener();
            ts.Listeners.Add(consListener);
            TraceListener xmListener = new XmlWriterTraceListener(new StreamWriter("TCPServerLog.xml"));
            xmListener.Filter = new EventTypeFilter(SourceLevels.Error);
            ts.Listeners.Add(xmListener);
            TraceListener txtListener = new TextWriterTraceListener(new StreamWriter("TCPServerLog.txt"));
            ts.Listeners.Add(txtListener);*/
            int stopPORT = PORT + 1;


            TcpListener serverListener = new TcpListener(IPAddress.Any, stopPORT);
            serverListener.Start();
            ts.TraceEvent(TraceEventType.Information,496,$"Shutdown Server Started on port: {stopPORT}");
            
            if (!stopShutdown)
            {
                TcpClient client = serverListener.AcceptTcpClient();
                ts.TraceEvent(TraceEventType.Information,496,"Client for Shutdown Accepted");
                //Console.WriteLine("client incomming fo shutting down");
                DoStopClient(client);

            }
            ts.TraceEvent(TraceEventType.Warning,496,"Shutting down StopServer And TCPServer");
            STOP = true;
        }

        protected virtual void DoStopClient(TcpClient client)
        {
            using (StreamReader sr = new StreamReader(client.GetStream()))
            {
                string str = sr.ReadLine();
                if (str.Trim().ToLower() == "stop")
                {
                    stopShutdown = true;
                }
            }
        }

        /// <summary>
        /// initiates StreamReader and StreamWriter
        /// call TCPServerWork with StreamReader and StreamWriter as parameters
        /// sets autoflush to true to flush out cache so every line read will be send back immediately and not cached instead
        /// </summary>
        /// <param name="client"></param>
        private void DoOneClient(TcpClient client)
        {
            using (StreamWriter sw = new StreamWriter(client.GetStream()))
            using (StreamReader sr = new StreamReader(client.GetStream()))
            {
                sw.AutoFlush = true;

                TCPServerWork(sr,sw);
                ts.TraceEvent(TraceEventType.Information,496,"Doing Client Work");
            }

        }

        /// <summary>
        /// uses sr and sw to read and write from and to client
        /// </summary>
        /// <param name="sr"></param>
        /// <param name="sw"></param>
        public abstract void TCPServerWork(StreamReader sr, StreamWriter sw);


    }
}
