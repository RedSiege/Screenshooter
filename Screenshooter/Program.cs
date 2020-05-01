using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using System.Diagnostics;
using System.Security.Principal;
using System.Threading;

class Program
{
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    public static extern bool SetProcessDPIAware();

    private static void Shooter(string filePath)
    {
        // Screenshot block to screenshot Multiple screens
        SetProcessDPIAware();
        Bitmap screenshot = new Bitmap(SystemInformation.VirtualScreen.Width, SystemInformation.VirtualScreen.Height,
                                PixelFormat.Format32bppArgb);
        Graphics screenGraph = Graphics.FromImage(screenshot);
        screenGraph.CopyFromScreen(SystemInformation.VirtualScreen.X, SystemInformation.VirtualScreen.Y, 0, 0,
                                    SystemInformation.VirtualScreen.Size,
                                    CopyPixelOperation.SourceCopy);

        screenshot.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);
        Console.WriteLine("[+] Screenshot successful, exiting now");
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

    public static void Main(string[] args)
    {
        // Checks for too many args -> creates a string from array -> checks for no args -> checks if the file already exists -> screenshot
        string filePath;
        bool isElevated;
        string appPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        if (args.Count() > 1)
        {
            Console.WriteLine("\n[-] Too many inputs!\n[-] Error: Exiting...");
            return;
        }
        else
        {
            filePath = string.Join("", args);
        }

        if (filePath == null || filePath.Count() == 0)
        {
            filePath = appPath + "\\" + DateTime.Now.ToString("M-dd-yyyy--HH-mm-ss") + ".png";
        }
        else if (File.Exists(filePath))
        {
            Console.WriteLine("\n[-] ERROR: File already exists!\n[-] Error: Exiting...");
            return;
        }
        else if (Directory.Exists(filePath))
        {
            if (filePath.EndsWith("\\"))
            {
                filePath = filePath + DateTime.Now.ToString("M-dd-yyyy_HH-mm-ss") + ".png";
            }
            else
            {
                filePath = filePath + "\\" + DateTime.Now.ToString("M-dd-yyyy_HH-mm-ss") + ".png";
            }
        }
        else if (filePath.EndsWith(".png"))
        {
            //do nothing, it's a good file name probs
        }
        else
        {
            filePath = filePath + ".png";
        }

        Console.WriteLine("[+] Screenshot location: " + Path.GetFullPath(filePath));

        try
        {
            Shooter(filePath);
        }
        catch (System.ComponentModel.Win32Exception a)
        {
            Console.WriteLine("[+] User may be running inside RDP session, attempting to attach with tscon...\n");

            using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
            {
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                isElevated = principal.IsInRole(WindowsBuiltInRole.Administrator);
            }

            if(isElevated)
            {
                Console.WriteLine("[+] User is admin, continuing...");
                uint sessionID = GetTerminalServicesSessionId();
                if (sessionID == 0)
                {
                    Console.WriteLine("\n[-] Session ID is 0 which means you're probably running as SYSTEM");
                    Console.WriteLine("[-] Try running as user in a high integrity context");
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
            filePath = appPath + "\\" + DateTime.Now.ToString("M-dd-yyyy--HH-mm-ss") + ".png";
            Console.WriteLine("[+] Using default screenshot location: " + Path.GetFullPath(filePath));
            Shooter(filePath);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
}

