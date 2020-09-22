using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using System.Diagnostics;
using System.Security.Principal;
using System.Threading;
using Accord.Video.FFMPEG;

class Program
{
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    static extern bool SetProcessDPIAware();
    static bool record = false;
    static int timeToRecord = 10;
    public static VideoFileWriter vf = new VideoFileWriter();
    static System.Windows.Forms.Timer myTimer = new System.Windows.Forms.Timer();
    static int alarmCounter = 1;
    static bool exitFlag = false;

    private static Bitmap Shooter(string filePath)
    {
        // Screenshot block to screenshot Multiple screens
        SetProcessDPIAware();
        Bitmap screenshot = new Bitmap(SystemInformation.VirtualScreen.Width, SystemInformation.VirtualScreen.Height,
                                PixelFormat.Format32bppArgb);
        Graphics screenGraph = Graphics.FromImage(screenshot);
        screenGraph.CopyFromScreen(SystemInformation.VirtualScreen.X, SystemInformation.VirtualScreen.Y, 0, 0,
                                    SystemInformation.VirtualScreen.Size,
                                    CopyPixelOperation.SourceCopy);

        if (!String.IsNullOrEmpty(filePath))
        {
            screenshot.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);
            Console.WriteLine("[+] Screenshot successful, exiting now");
            return null;
        }
        //Allow for reuse when capturing video
        else
        {
            return screenshot;
        }

    }

    // This is the method to run when the timer is raised.
    // https://docs.microsoft.com/en-us/dotnet/api/system.windows.forms.timer?view=netcore-3.1
    private static void TimerEventProcessor(Object myObject, EventArgs myEventArgs)
    {
        //myTimer.Stop();

        // Displays a message box asking whether to continue running the timer.
        if (alarmCounter < timeToRecord * 10)
        {
            Bitmap screenGrab = Shooter(String.Empty);
            vf.WriteVideoFrame(screenGrab);

            // Restarts the timer and increments the counter.
            alarmCounter += 1;
            //myTimer.Enabled = true;
        }
        else
        {
            // Stops the timer.
            exitFlag = true;
        }
    }

    private static void Watcher2(string filePath)
    {
        Console.WriteLine("Entered watcher2");
        int videoBitRate = 1200 * 1000;
        vf.Open(filePath, SystemInformation.VirtualScreen.Width, SystemInformation.VirtualScreen.Height, 24, VideoCodec.MPEG4, videoBitRate);
        for (int i = 0; i < timeToRecord + 2; i++)
        {
            Bitmap screenGrab = Shooter(String.Empty);
            vf.WriteVideoFrame(screenGrab, TimeSpan.FromSeconds(i));
            Thread.Sleep(1000);
        }
        Thread.Sleep(1000);
        vf.Close();

    }

    private static void Watcher(string filePath)
    {
        myTimer.Tick += new EventHandler(TimerEventProcessor);

        vf.Open(filePath, SystemInformation.VirtualScreen.Width, SystemInformation.VirtualScreen.Height, 500, VideoCodec.MPEG4, 50000000);

        // Sets the timer interval to 1 seconds.
        myTimer.Interval = 1000;
        myTimer.Start();

        // Runs the timer, and raises the event.
        while (exitFlag == false)
        {
            // Processes all the events in the queue.
            Application.DoEvents();
        }
        vf.Close();
    }

    [System.Runtime.InteropServices.DllImport("kernel32.dll")]
    static extern bool ProcessIdToSessionId(uint dwProcessId, out uint pSessionId);

    static uint GetTerminalServicesSessionId()
    {
        var proc = Process.GetCurrentProcess();
        var pid = proc.Id;

        var sessionId = 0U;
        if (ProcessIdToSessionId((uint)pid, out sessionId))
        {
            return sessionId;
        }
        return 1U; // fallback, the console session is session 1
    }

    private static string GetWriteLocation(string filePath)
    {
        string appPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string extension;

        if (record)
            extension = ".avi";
        else
            extension = ".png";
        
        if (String.IsNullOrEmpty(filePath))
            return appPath + "\\" + DateTime.Now.ToString("M-dd-yyyy_HH-mm-ss") + extension;

        if (File.Exists(filePath))
        {
            Console.WriteLine("\n[-] ERROR: File already exists!\n[-] Error: Exiting...");
            return appPath + "\\" + DateTime.Now.ToString("M-dd-yyyy_HH-mm-ss") + extension;
        }
        else if (Directory.Exists(filePath))
        {
            if (filePath.EndsWith("\\"))
            {
                filePath = filePath + DateTime.Now.ToString("M-dd-yyyy_HH-mm-ss") + extension;
            }
            else
            {
                filePath = filePath + "\\" + DateTime.Now.ToString("M-dd-yyyy_HH-mm-ss") + extension;
            }
        }
        else if (filePath.EndsWith(".png") || filePath.EndsWith(".avi"))
        {
            //do nothing, it's a good file name probs
        }
        else
        {
            if (filePath.EndsWith("\\"))
            {
                filePath = filePath + DateTime.Now.ToString("M-dd-yyyy_HH-mm-ss") + extension;
            }
            else
            {
                filePath = filePath + "\\" + DateTime.Now.ToString("M-dd-yyyy_HH-mm-ss") + extension;
            }
        }

        return filePath;
    }

    private static string ParseArgs(string[] commands)
    {
        if (commands.Count() > 3)
        {
            Console.WriteLine("\n[-]Error: Too many inputs! Exiting...");
            return null;
        }

        if (commands.Count() == 3)
        {
            if (commands[0] == "record")
            {
                Console.WriteLine("[+] Recording screen...");
                record = true;
            }
            
            if (int.TryParse(commands[2], out int result))
                timeToRecord = result;
            else
                Console.WriteLine("[-] Error: third argument is not an integer, using the default of 10s");

            return GetWriteLocation(commands[1]);
        }
        
        if (commands.Count() == 2)
        {
            if (commands[0] == "record")
            {
                Console.WriteLine("[+] Recording screen...");
                record = true;
            }
            if (int.TryParse(commands[1], out int result))
            {
                Console.WriteLine("[+] Using record time of {0} seconds", result);
                timeToRecord = result;
                return GetWriteLocation(String.Empty);
            }
            else
            {
                return GetWriteLocation(commands[1]);
            }
        }
        
        if (commands.Count() == 1 && commands[0] == "record")
        {
            record = true;
            return GetWriteLocation(String.Empty);
        }
        else if (commands.Count() == 1)
            return GetWriteLocation(commands[0]);

        if (String.IsNullOrEmpty(string.Join("",commands)))
            return GetWriteLocation(String.Empty);

        //should never reach this
        return null;
    }

    public static void Main(string[] args)
    {
        // Parse args -> creates a string from array -> checks for no args -> checks if the file already exists -> screenshot
        string filePath;
        bool isElevated;

        filePath = ParseArgs(args);

        if (filePath == null)
        {
            Console.WriteLine("Error in parsing arguments. Try again");
            Application.Exit();
        }

        Console.WriteLine("[+] Screenshot/video save location: " + Path.GetFullPath(filePath));

        Console.CancelKeyPress += delegate {
            Console.WriteLine("Exiting application gracefully...");
            try
            {
                vf.Close();
            }
            catch
            {
                Application.Exit();
            }
        };

        if (!record)
        {
            try
            {
                Shooter(filePath);
            }
            catch (System.ComponentModel.Win32Exception)
            {
                Console.WriteLine("[+] User may be running inside RDP session, attempting to attach with tscon...\n");

                using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
                {
                    WindowsPrincipal principal = new WindowsPrincipal(identity);
                    isElevated = principal.IsInRole(WindowsBuiltInRole.Administrator);
                }

                if (isElevated)
                {
                    Console.WriteLine("[+] User is admin, continuing...");
                    uint sessionID = GetTerminalServicesSessionId();
                    if (sessionID == 0)
                    {
                        Console.WriteLine("\n[-] Session ID is 0 which means you're probably running as SYSTEM");
                        Console.WriteLine("[-] Try running as user in a high integrity context instead");
                    }
                    else
                    {
                        //Grab the session ID for the current process
                        var winDir = System.Environment.GetEnvironmentVariable("WINDIR");
                        Process.Start(Path.Combine(winDir, "system32", "tscon.exe"),
                            String.Format("{0} /dest:console", GetTerminalServicesSessionId()))
                        .WaitForExit();
                        Thread.Sleep(1000);

                        Shooter(filePath);
                    }
                }
                else
                {
                    Console.WriteLine("\n[-] Not running with Administrator privileges, please rerun as admin");
                    return;
                }
            }
            catch (System.Runtime.InteropServices.ExternalException)
            {
                Console.WriteLine("[-] Error with path given (unable to save bitmap there)\n");
                filePath = GetWriteLocation(String.Empty);
                Console.WriteLine("[+] Using default screenshot location: " + Path.GetFullPath(filePath));
                Shooter(filePath);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
        else
        {
            // We'll make this more complex later
            try
            {
                Watcher2(filePath);
            }
            catch (System.IO.IOException)
            {
                Console.WriteLine("Something happened with args, using defaults");
                timeToRecord = 10;
                filePath = GetWriteLocation(String.Empty);
                Console.WriteLine("[+] Using default video save location: " + Path.GetFullPath(filePath));
                Watcher2(filePath);
            }
        }
    }
}

