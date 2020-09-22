# Screenshooter

## This tool was created to take full screenshots of the user's desktop(s) when beacon doesn't want to work. Update: Can now do video but the file size of the application is huge (35mb). Working on a fix (which is why this is in dev).

To use, run the file and it will create a screenshot and save it in the current user's AppData\Roaming directory with a timestamped name. You can also pass it a flag for the location/filename where you want it saved. For some examples:

## Usage

The application takes in 0-3 flags as input. These flags *need* to be in the correct order to work! See the examples below. If you want a screen recording, you must specify "record" as the first positional argument. You can then either specify the path, the path and how long to record, or the just how long to record. The length of the recording is in seconds.

```
Screenshooter.exe
Screenshooter.exe C:\Users\Public\Documents\
Screenshooter.exe C:\Users\Public\Documents\screenshot.png

- To record the screen(s):
Screenshooter.exe record
Screenshooter.exe record c:\users\test\desktop\vid.avi
Screenshooter.exe record c:\users\test\desktop\vid.avi 30
```