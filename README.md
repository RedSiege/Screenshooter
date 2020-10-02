# Screenshooter

## This tool was created to take full screenshots or a recording of the user's desktop(s) when beacon doesn't want to work. 

Update: Can now record video of the desktop as well for X amount of time, *default is 10s*. This MUST be done by dropping the exe to the system first and will not work through execute-assembly.

To use, run the file and it will create a screenshot and save it in the current user's AppData\Roaming directory with a timestamped name. You can also pass it a flag for the location/filename where you want it saved. For some examples:

## Usage

The application takes in 0-3 flags as input. These flags *need* to be in the correct order to work (caveat is record functionality)! See the examples below. If you want a screen recording, you must specify "record" as the first positional argument. You can then either specify the path, the path and how long to record, or just how long to record. The length of the recording is in seconds.

```
Screenshooter.exe
Screenshooter.exe C:\Users\Public\Documents\
Screenshooter.exe C:\Users\Public\Documents\screenshot.png

- To record the screen(s) (must drop on system first and then use the execute command in beacon):
Screenshooter.exe record
Screenshooter.exe record 30
Screenshooter.exe record c:\users\test\desktop\vid.mp4
Screenshooter.exe record c:\users\test\desktop\vid.mp4 30
```
