using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

class Program
{
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    public static extern bool SetProcessDPIAware();

    private static void Shooter(string filePath)
    {
        //Function to take the screenshot
        // Screenshot block to screenshot Multiple screens
        SetProcessDPIAware();
        Bitmap screenshot = new Bitmap(SystemInformation.VirtualScreen.Width, SystemInformation.VirtualScreen.Height,
                                PixelFormat.Format32bppArgb);
        Graphics screenGraph = Graphics.FromImage(screenshot);
        screenGraph.CopyFromScreen(SystemInformation.VirtualScreen.X, SystemInformation.VirtualScreen.Y, 0, 0,
                                   SystemInformation.VirtualScreen.Size,
                                   CopyPixelOperation.SourceCopy);

        screenshot.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);
    }
    public static void Main(string[] args)
    {
        // Checks for too many args -> creates a string from array -> checks for no args -> checks if the file already exists -> screenshot
        string filePath;
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
            filePath = DateTime.Now.ToString("M-dd-yyyy--HH-mm-ss") + ".png";
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
        catch (System.Runtime.InteropServices.ExternalException)
        {
            Console.WriteLine("[-] Error with path given (unable to save bitmap there)\n");
            filePath = "C:\\Users\\Public\\Documents\\" + DateTime.Now.ToString("M-dd-yyyy_HH-mm-ss") + ".png";
            Console.WriteLine("[+] Using default screenshot location: " + Path.GetFullPath(filePath));
            Shooter(filePath);
        }

    }
}

