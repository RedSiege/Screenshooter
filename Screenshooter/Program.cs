using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using System.Diagnostics;
using System.Security.Principal;
using System.Threading;
using ScreenRecorderLib;
using System.Threading.Tasks;
using System.Timers;
using System.Reflection;
using System.Security.Cryptography;
using System.Windows.Threading;
using System.Runtime.InteropServices;
using System.ComponentModel;

// Borrowed from Nick - https://stackoverflow.com/a/72296
public static class ResourceExtractor
{
    public static void ExtractResourceToFile(string resourceName, string filename)
    {
        if (!System.IO.File.Exists(filename))
            using (System.IO.Stream s = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            using (System.IO.FileStream fs = new System.IO.FileStream(filename, System.IO.FileMode.Create))
            {
                byte[] b = new byte[s.Length];
                s.Read(b, 0, b.Length);
                fs.Write(b, 0, b.Length);
            }
    }
}


class Program
{


    [DllImport("user32.dll")]
    static extern bool SetProcessDPIAware();
    static bool record = false;
    static int timeToRecord = 10;
    static bool exitFlag = false;
    private static bool _isRecording;
    private static bool isDone = false;
    private static Stopwatch _stopWatch;
    public static Recorder rec;

    private static void OnTimedEvent(object source, ElapsedEventArgs e)
    {
        exitFlag = true;
    }


    [DllImport("ScreenRecorderLib.dll")] public extern static Recorder GetRecorder();


    private static void Watcher(string filePath)
    {
        // Borrowed from the great coder(s) at https://github.com/sskodje/ScreenRecorderLib/blob/master/TestConsoleApp/Program.cs
        RecorderOptions options = new RecorderOptions
        {
            RecorderMode = RecorderMode.Video,
            //If throttling is disabled, out of memory exceptions may eventually crash the program,
            //depending on encoder settings and system specifications.
            IsThrottlingDisabled = false,
            //Hardware encoding is enabled by default.
            IsHardwareEncodingEnabled = true,
            //Low latency mode provides faster encoding, but can reduce quality.
            IsLowLatencyEnabled = true,
            //Fast start writes the mp4 header at the beginning of the file, to facilitate streaming.
            IsMp4FastStartEnabled = false,
            VideoOptions = new VideoOptions
            {
                BitrateMode = BitrateControlMode.UnconstrainedVBR,
                Bitrate = 8000 * 1000,
                Framerate = 24,
                IsFixedFramerate = false,
                EncoderProfile = H264Profile.Main
            },
        };

        rec = Recorder.CreateRecorder(options);
        rec.OnRecordingFailed += Rec_OnRecordingFailed;
        rec.OnRecordingComplete += Rec_OnRecordingComplete;
        rec.OnStatusChanged += Rec_OnStatusChanged;

        System.Timers.Timer aTimer = new System.Timers.Timer();
        aTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
        aTimer.Interval = timeToRecord * 1000;
        aTimer.Start();

        rec.Record(filePath);
        CancellationTokenSource cts = new CancellationTokenSource();
        var token = cts.Token;
        Task.Run(async () =>
        {
            while (true)
            {
                if (token.IsCancellationRequested)
                    return;
                if (_isRecording)
                {
                    Dispatcher.CurrentDispatcher.Invoke(() =>
                    {
                        //Console.Write(String.Format("\rElapsed: {0}s:{1}ms", _stopWatch.Elapsed.Seconds, _stopWatch.Elapsed.Milliseconds));
                    });
                }
                await Task.Delay(10);
            }
        }, token);
        while (true)
        {
            if (exitFlag == true)
            {
                break;
            }
        }

        cts.Cancel();
        rec.Stop();
        while (!isDone)
        {
            Thread.Sleep(1000);
        }
        //Console.ReadKey();
    }

    private static void Rec_OnStatusChanged(object sender, RecordingStatusEventArgs e)
    {
        switch (e.Status)
        {
            case RecorderStatus.Idle:
                //Console.WriteLine("Recorder is idle");
                break;
            case RecorderStatus.Recording:
                _stopWatch = new Stopwatch();
                _stopWatch.Start();
                _isRecording = true;
                Console.WriteLine("[+] Recording started");
                break;
            case RecorderStatus.Paused:
                Console.WriteLine("Recording paused");
                break;
            case RecorderStatus.Finishing:
                Console.WriteLine("[+] Finishing encoding");
                break;
            default:
                break;
        }
    }

    private static void Rec_OnRecordingComplete(object sender, RecordingCompleteEventArgs e)
    {
        Console.WriteLine("[+] Recording completed");
        _isRecording = false;
        _stopWatch?.Stop();
        Console.WriteLine(String.Format("File: {0}", e.FilePath));
        isDone = true;
    }

    private static void Rec_OnRecordingFailed(object sender, RecordingFailedEventArgs e)
    {
        Console.WriteLine("Recording failed with: " + e.Error);
        _isRecording = false;
        _stopWatch?.Stop();
    }

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
            extension = ".mp4";
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

        if (String.IsNullOrEmpty(string.Join("", commands)))
            return GetWriteLocation(String.Empty);

        //should never reach this
        return null;
    }






    public static void Main(string[] args)
    {

        try
        {
            //ResourceExtractor.ExtractResourceToFile("Screenshooter.ScreenRecorderLib.dll", "unmanagedservice.dll");
            //Load();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }


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

        Console.CancelKeyPress += delegate
        {
            Console.WriteLine("\n[:(] Rage Quit huh? Attempting to exit the application gracefully...\n");
            try
            {
                rec.Stop();
                while (!isDone)
                {
                    Thread.Sleep(1000);
                }
                Application.Exit();
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
                Watcher(filePath);
            }
            catch (System.IO.IOException e)
            {
                Console.WriteLine(e);
                Console.WriteLine("Something happened with args, using defaults");
                timeToRecord = 10;
                filePath = GetWriteLocation(String.Empty);
                Console.WriteLine("[+] Using default video save location: " + Path.GetFullPath(filePath));
                Watcher(filePath);
            }
            catch (Exception a)
            {
                Console.WriteLine(a);
            }
        }
    }

    public static Assembly Load()
    {
        // Get the byte[] of the DLL
        byte[] ba = null;
        string resource = "Screenshooter.ScreenRecorderLib.dll";
        Assembly curAsm = Assembly.GetExecutingAssembly();
        using (Stream stm = curAsm.GetManifestResourceStream(resource))
        {
            ba = new byte[(int)stm.Length];
            stm.Read(ba, 0, (int)stm.Length);
        }

        bool fileOk = false;
        string tempFile = "";

        using (SHA1CryptoServiceProvider sha1 = new SHA1CryptoServiceProvider())
        {
            // Get the hash value of the Embedded DLL
            string fileHash = BitConverter.ToString(sha1.ComputeHash(ba)).Replace("-", string.Empty);

            // The full path of the DLL that will be saved
            tempFile = Path.GetTempPath() + "ScreenRecorderLib.dll";

            // Check if the DLL is already existed or not?
            if (File.Exists(tempFile))
            {
                // Get the file hash value of the existed DLL
                byte[] bb = File.ReadAllBytes(tempFile);
                string fileHash2 = BitConverter.ToString(sha1.ComputeHash(bb)).Replace("-", string.Empty);

                // Compare the existed DLL with the Embedded DLL
                if (fileHash == fileHash2)
                {
                    // Same file
                    fileOk = true;
                }
                else
                {
                    // Not same
                    fileOk = false;
                }
            }
            else
            {
                // The DLL is not existed yet
                fileOk = false;
            }
        }

        // Create the file on disk
        if (!fileOk)
        {
            System.IO.File.WriteAllBytes(tempFile, ba);
        }

        // Load it into memory    
        return Assembly.LoadFile(tempFile);
    }
}