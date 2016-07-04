using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;
using System.IO;
using System.IO.Pipes;
using System.Security.Principal;
using System.Security.AccessControl;

namespace ScriptableWMPlayer
{
    static class Program
    {
        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        const int SW_HIDE = 0;
        const int SW_SHOW = 5;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static int Main(string[] args)
        {
            int returnCode = 0;
            bool isFirst = false;
            Mutex mutex = null;
            try
            {
                mutex = Mutex.OpenExisting("ScriptableWMPlayerMutex");
            }
            catch
            {
                isFirst = true;
                mutex = new Mutex(false, "ScriptableWMPlayerMutex");
            }

            if (!isFirst)
            {
                if (args.Length > 0 && args[0].Equals("-alive", StringComparison.InvariantCultureIgnoreCase))
                {
                    Console.WriteLine("Alive");
                }
                else
                {
                    using (NamedPipeServerStream serverStream = new NamedPipeServerStream("ScriptableWMPlayerPipeClient", PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
                    using (NamedPipeClientStream clientStream = new NamedPipeClientStream(".", "ScriptableWMPlayerPipeServer", PipeDirection.Out))
                    {
                        using (StreamWriter writer = new StreamWriter(clientStream))
                        {
                            clientStream.Connect(10000);
                            if (!clientStream.IsConnected)
                            {
                                Console.WriteLine("ERROR: Unable to connect to pipe");
                                returnCode = -1;
                            }
                            else
                            {
                                string value = String.Join(((char)1).ToString(), args);
                                writer.Write(value);
                                clientStream.WaitForPipeDrain();
                            }
                        }
                        if (returnCode == 0)
                        {
                            IAsyncResult result = serverStream.BeginWaitForConnection(null, null);
                            if (result.AsyncWaitHandle.WaitOne(1000))
                            {
                                serverStream.EndWaitForConnection(result);
                                using (StreamReader reader = new StreamReader(serverStream))
                                {
                                    Console.Write(reader.ReadToEnd());
                                }
                            }
                            else
                            {
                                Console.WriteLine("ERROR: Timed out waiting for server response.");
                                return returnCode = -1;
                            }
                        }
                    }
                } 
            }
            else
            {
                if (args.Any(arg => arg.Equals("-alive", StringComparison.InvariantCultureIgnoreCase)))
                {
                    Console.WriteLine("Dead");
                }
                else
                {
                    bool hidConsole = false;
                    if (args.Any((arg) => arg.Equals("-hideconsole", StringComparison.InvariantCultureIgnoreCase)))
                    {
                        IntPtr hWnd = GetConsoleWindow();
                        ShowWindow(hWnd, SW_HIDE);
                        hidConsole = true;
                    }

                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);

                    Player player = new Player(args);
                    Thread serverThread = new Thread(new ParameterizedThreadStart(ServerThreadProc));
                    serverThread.Start((object)player);

                    Application.Run(player);

                    listenForConnections = false;
                    serverThread.Join(1000);
                    if (serverThread.IsAlive)
                    {
                        serverThread.Abort();
                    }

                    if (hidConsole)
                    {
                        IntPtr hWnd = GetConsoleWindow();
                        ShowWindow(hWnd, SW_SHOW);
                    }
                }
            }

            if (mutex != null && mutex.WaitOne(0))
                mutex.ReleaseMutex();

            return returnCode;
        }

        static bool listenForConnections = true;
        static void ServerThreadProc(object context)
        {
            Player player = context as Player;

            while (listenForConnections)
            {
                PipeSecurity security = new PipeSecurity();
                SecurityIdentifier sid = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
                PipeAccessRule rule = new PipeAccessRule(sid, PipeAccessRights.ReadWrite, AccessControlType.Allow);
                security.AddAccessRule(rule);
                string result = "ERROR";
                using (NamedPipeServerStream serverStream = new NamedPipeServerStream("ScriptableWMPlayerPipeServer", PipeDirection.In, 2, PipeTransmissionMode.Byte, PipeOptions.Asynchronous))//, 2, PipeTransmissionMode.Byte, PipeOptions.Asynchronous, 255, 255, security))
                {
                    bool waitingForData = true;
                    var asyncResult = serverStream.BeginWaitForConnection(null, null);
                    while (listenForConnections && waitingForData)
                    {
                        if (asyncResult.AsyncWaitHandle.WaitOne(100))
                        {
                            serverStream.EndWaitForConnection(asyncResult);
                            using (StreamReader reader = new StreamReader(serverStream))
                            {
                                string args = reader.ReadToEnd();
                                result = player.CommandReceived(args);
                            }
                            waitingForData = false;
                        }
                    }
                }
                if (listenForConnections)
                {
                    using (NamedPipeClientStream clientStream = new NamedPipeClientStream(".", "ScriptableWMPlayerPipeClient", PipeDirection.Out))
                    {
                        try
                        {
                            clientStream.Connect(1000);
                            using (StreamWriter writer = new StreamWriter(clientStream))
                                writer.WriteLine(result);
                        }
                        catch { }
                    }
                }
            }
        }
    }
}
