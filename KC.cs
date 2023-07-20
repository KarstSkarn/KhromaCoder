using System;
using System.IO;
using System.Text;
using Windows.Media;
using System.Drawing;
using System.Diagnostics;
using System.Windows.Forms;
using Windows.Devices.Input;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Windows.ApplicationModel.DataTransfer;
using static System.Net.Mime.MediaTypeNames;

class KC
{
    // UI DATA
    static byte menuPointer = 0;
    static byte menuHorizontalPointer = 0;
    static byte menuStatus = 0;
    static string consoleTitle = "Khroma Coder ";
    static string executableVersion = "1.0.18";

    // GENERAL OPTIONS DATA
    static byte verboseLevel = 0;
    static string codec = "h264";
    static string yuvValue = "AUTO";

    // PATHS
    const string FFMPEGPATH = "ffmpeg.exe";
    const string FFMPEGPROBEPATH = "ffprobe.exe";
    const string ENCODESOURCEFILE = "test.zip";
    const string KCVIDEOOUTDIRECTORY = "KCVideoOut/";
    const string KCFILEOUTDIRECTORY = "KCFileOut/";
    const string KCTEMPORARYDATADIRECTORY = "KCTemporaryFiles/";

    // VIDEO ENCODING DATA
    static int OUTW = 100; //640
    static int OUTH = 100; //360
    static uint CHKSUM = 0;
    static byte densityType = 4;

    // VIDEO DECODING DATA
    static int tolerance = 5;
    static int bwtolerance = 100; // 75

    static byte readStep = 0;
    static int framesPerSecond = 60;
    static int x0Pos = 0;
    static int y0Pos = 0;
    static int x1Pos = 0;
    static int y1Pos = 0;
    static int frameCounter = 1;
    static int codedFrameNumber = -1;
    static int videoSizeX = 0;
    static int videoSizeY = 0;
    static int genericEndXPointer = 0;
    static int genericEndYPointer = 0;
    static int videoPosX = 0;
    static int videoPosY = 0;
    static int tmpCounterX = 0;
    static int tmpCounterY = 0;
    static string fileName = "NULL";
    static string readBuffer = "";
    static string checksumValue = "";
    static string checksumReadValue = "NULL";

    // VIDEO DECODING CONTROL VALUES
    static bool headerFound = false;
    static byte encodingProcessStep = 0;
    static int checksumAlerts = 0;
    static int confirmedFrames = 0;
    static int fatalAlerts = 0;
    static int bytesCounter = 0;
    static string incomingDensityType = "";



    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    public static extern short GetAsyncKeyState(int vKey);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowThreadProcessId(IntPtr hWnd, out uint processId);


    [STAThread]
    static void Main()
    {
        menuStatus = 0;
        while (true)
        {
            Console.Title = consoleTitle + executableVersion;
            UIFormatConsole();
            if (menuStatus == 0)
            {
                Console.Clear();
                UIMainScreen();
            }
            if (menuStatus == 1)
            {
                KCResetFrameData(0);
                Console.Clear();
                UIDecodeScreen();
            }
            if (menuStatus == 2)
            {
                Console.Clear();
                UIEncodeScreen();
            }
            if (menuStatus == 3)
            {
                Console.Clear();
                UIOptionsScreen();
            }
            if (menuStatus == 9)
            {
                Environment.Exit(0);
            }
        }
    }
    // GRAPHICAL MENU SCREENS
    static string UIGetPercentage(int currentNumber, int totalNumber)
    {
        double percentage = (double)currentNumber / totalNumber;
        string percentageString = (percentage * 100).ToString("0.00") + "%";
        return percentageString;
    }
    static string UIGenerateLoadingBar(int currentNumber, int totalNumber, int barLength, char barCharacter)
    {
        double percentage = (double)currentNumber / totalNumber;
        int filledLength = (int)(percentage * barLength);
        int emptyLength = barLength - filledLength;
        string loadingBar = " ERROR GENERATING LOADING BAR ";

        try
        {
            loadingBar = new string(barCharacter, filledLength) + new string(' ', emptyLength);
        }
        catch
        {
            fatalAlerts++;
        }
        return loadingBar;
    }
    static bool UIUpdateDecodingStats(int frameNumber, int totalFrames)
    {
        UICheckWindowSize();
        bool cancelFlag = false;
        if (frameNumber > totalFrames)
        {
            cancelFlag = true;
        }
        WRSetCursor(0, 0);
        //          10        20        30        40        50        60        70         81
        //  123456789012345678901234567890123456789012345678901234567890123456789012345678901
        //                     XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
        WRSetCursor(38, 15);
        WR(UIGetPercentage(frameNumber, totalFrames), "White", "Black");
        WRSetCursor(19, 16);
        WR("║", "White", "Black");
        WR(UIGenerateLoadingBar(frameNumber, totalFrames, 42, Convert.ToChar("█")), "Blue", "DarkGray");
        WRSetCursor(62, 16);
        WR("║", "White", "Black");
        WRSetCursor(4, 18);
        if (!headerFound)
        {
            WR("■ ", "White", "Black");
            WR("Data header not found yet...", "Gray", "Black");
        }
        else
        {
            WR("■ ", "Green", "Black");
            WR("Data header found!            ", "Gray", "Black");
            WRSetCursor(46, 18);
            WR("■ ", "Green", "Black");
            WR("Density type: ", "Gray", "Black");
            if (densityType == 0)
            {
                WR("MONO (1bppx)", "White", "Black");
            }
            if (densityType == 1)
            {
                WR("DUAL (2bppx)", "White", "Black");
            }
            if (densityType == 2)
            {
                WR("HALF-BYTE (4bppx)", "White", "Black");
            }
            if (densityType == 3)
            {
                WR("2 BYTES (16bppx)", "White", "Black");
            }
            if (densityType == 4)
            {
                WR("3 BYTES (16/24bppx)", "White", "Black");
            }
        }
        WRSetCursor(4, 22);
        WR("■ ", "White", "Black");
        WR("Confirmed Frames: ", "Gray", "Black");
        if (confirmedFrames > 0)
        {
            WR(confirmedFrames.ToString(), "Green", "Black");
        }
        else
        {
            WR(confirmedFrames.ToString(), "White", "Black");
        }
        WRSetCursor(4, 23);
        WR("■ ", "White", "Black");
        WR("Checksum Alerts: ", "Gray", "Black");
        if (checksumAlerts == 0)
        {
            WR(checksumAlerts.ToString(), "White", "Black");
        }
        else
        {
            WR(checksumAlerts.ToString(), "Yellow", "Black");
        }
        WRSetCursor(46, 22);
        WR("■ ", "White", "Black");
        WR("Current Frame: ", "Gray", "Black");
        WR(frameNumber.ToString() + " / " + totalFrames.ToString(), "White", "Black");
        WRSetCursor(46, 23);
        WR("■ ", "White", "Black");
        WR("Fatal Alerts: ", "Gray", "Black");
        if (fatalAlerts == 0)
        {
            WR(fatalAlerts.ToString(), "White", "Black");
        }
        else
        {
            WR(fatalAlerts.ToString(), "Red", "Black");
        }
        int keyState = GetAsyncKeyState(27); // ESC
        if (UIKeyDown(keyState))
        {
            cancelFlag = true;
        }
        return cancelFlag;
    }
    static bool UIUpdateEncodingStats(double currentSize, double totalSize, int currentFrame)
    {
        UICheckWindowSize();
        bool cancelFlag = false;
        WRSetCursor(0, 0);
        //          10        20        30        40        50        60        70         81
        //  123456789012345678901234567890123456789012345678901234567890123456789012345678901
        //                     XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
        if (encodingProcessStep == 0)
        {
            WRSetCursor(38, 15);
            WR(UIGetPercentage(Convert.ToInt32(currentSize), Convert.ToInt32(totalSize)), "White", "Black");
            WRSetCursor(19, 16);
            WR("║", "White", "Black");
            WR(UIGenerateLoadingBar(Convert.ToInt32(currentSize), Convert.ToInt32(totalSize), 42, Convert.ToChar("█")), "Blue", "DarkGray");
            WRSetCursor(62, 16);
            WR("║", "White", "Black");
        }
        if (encodingProcessStep == 1)
        {
            WRSetCursor(20, 15);
            WR("Creating <.mp4> file...", "White", "Black");
            WRSetCursor(19, 16);
            WR("║", "White", "Black");
            WR(UIGenerateLoadingBar(100, 100, 42, Convert.ToChar("█")), "DarkGreen", "DarkGray");
            WRSetCursor(62, 16);
            WR("║", "White", "Black");
        }
        if (encodingProcessStep == 2)
        {
            WRSetCursor(20, 15);
            WR("Cleaning temporary files...", "White", "Black");
            WRSetCursor(19, 16);
            WR("║", "White", "Black");
            WR(UIGenerateLoadingBar(100, 100, 42, Convert.ToChar("█")), "Green", "DarkGray");
            WRSetCursor(62, 16);
            WR("║", "White", "Black");
        }
        if (encodingProcessStep == 0)
        {
            WRSetCursor(4, 18);
            WR("■ ", "White", "Black");
            WR("Density type: ", "Gray", "Black");
            if (densityType == 0)
            {
                WR("MONO (1bppx)", "White", "Black");
            }
            if (densityType == 1)
            {
                WR("DUAL (2bppx)", "White", "Black");
            }
            if (densityType == 2)
            {
                WR("HALF-BYTE (4bppx)", "White", "Black");
            }
            if (densityType == 3)
            {
                WR("2 BYTES (16bppx)", "White", "Black");
            }
            if (densityType == 4)
            {
                WR("3 BYTES (16/24bppx)", "White", "Black");
            }
            WRSetCursor(4, 19);
            WR("■ ", "White", "Black");
            WR("Bytes Processed: ", "Gray", "Black");
            WR(Convert.ToInt32(currentSize).ToString() + " Bytes / " + Convert.ToInt32(totalSize).ToString() + " Bytes", "Cyan", "Black");
            WRSetCursor(4, 20);
            WR("■ ", "White", "Black");
            WR("Generated Frames: ", "Gray", "Black");
            WR(currentFrame.ToString(), "Green", "Black");
            WRSetCursor(4, 21);
            WR("■ ", "White", "Black");
            WR("FPS Output: ", "Gray", "Black");
            WR(framesPerSecond.ToString(), "White", "Black");
            WRSetCursor(4, 22);
            WR("■ ", "White", "Black");
            WR("Video Size: ", "Gray", "Black");
            WR(OUTW.ToString() + "px * " + OUTH.ToString() + "px", "White", "Black");
        }
        int keyState = GetAsyncKeyState(27); // ESC
        if (UIKeyDown(keyState))
        {
            cancelFlag = true;
        }
        return cancelFlag;
    }
    static void UIMainScreen()
    {
        string[] colorsArray = { "Green", "DarkGreen", "Blue", "DarkBlue", "Cyan", "DarkCyan", "Red", "DarkRed", "DarkMagenta" };
        int keyState = 0;
        bool[] previousKeyStates = new bool[5];
        previousKeyStates[0] = false;
        previousKeyStates[1] = false;
        previousKeyStates[2] = false;
        Random RND = new Random();
        while (true)
        {
            // 1
            WRSetCursor(30, 3);
            WR(" ", "Green", "White"); // 1
            WR(" ", "Green", "Black"); // 2
            WR(" ", "Green", "White"); // 3
            WR(" ", "Green", "Black"); // 4
            WR(" ", "Green", "Black"); // 5
            WR(" ", "Green", "White"); // 6
            WR(" ", "Green", "Black"); // 7
            WR(" ", "Green", "White"); // 8
            WR(" ", "Green", "Blue");  // 9
            WR(" ", "Green", "Green"); // 0
            WR(" ", "Green", "Cyan");  // 1
            WR(" ", "Green", "Yellow"); // 2
            WR(" ", "Green", "DarkCyan"); // 3
            WR(" ", "Green", "DarkYellow"); // 4
            WR(" ", "Green", "White"); // 5
            WR(" ", "Green", "Black"); // 6
            WR(" ", "Green", "Black"); // 7
            WR(" ", "Green", "White"); // 8
            WR(" ", "Green", "DarkMagenta"); // 9
            WR(" ", "Green", "DarkRed"); // 0
            WR(" ", "Green", "Blue"); // 1
            // 2
            WRSetCursor(30, 4);
            WR(" ", "Green", "White"); // 1
            WR(" ", "Green", "Black"); // 2
            WR(" ", "Green", "White"); // 3
            WR(" ", "Green", "Black"); // 4
            WR(" ", "Green", "Blue"); // 5
            WR(" ", "Green", "Green"); // 6
            WR(" ", "Green", "DarkCyan"); // 7
            WR(" ", "Green", "Red"); // 8
            WR(" ", "Green", "White");  // 9
            WR(" ", "Green", "Black"); // 0
            WR(" ", "Green", "White");  // 1
            WR(" ", "Green", "White"); // 2
            WR(" ", "Green", "DarkRed"); // 3
            WR(" ", "Green", "Yellow"); // 4
            WR(" ", "Green", "DarkGreen"); // 5
            WR(" ", "Green", "White"); // 6
            WR(" ", "Green", "White"); // 7
            WR(" ", "Green", "Black"); // 8
            WR(" ", "Green", "White"); // 9
            WR(" ", "Green", "DarkBlue"); // 0
            WR(" ", "Green", "Green"); // 1
            // 3 - 9
            for (byte y = 5; y <= 10; y++)
            {
                WRSetCursor(30, y);
                for (byte r = 0; r <= 20; r++)
                {
                    WR(" ", "Green", colorsArray[RND.Next(0, 8)]);
                }
            }
            WRSetCursor(30, 11);
            WR(" ", "Green", "Blue"); // 1
            WR(" ", "Green", "Green"); // 2
            WR(" ", "Green", "DarkGreen"); // 3
            WR(" ", "Green", "Yellow"); // 4
            WR(" ", "Green", "Cyan"); // 5
            WR(" ", "Green", "Red"); // 6
            WR(" ", "Green", "DarkCyan"); // 7
            WR(" ", "Green", "DarkRed"); // 8
            WR(" ", "Green", "DarkBlue");  // 9
            WR(" ", "Green", "Cyan"); // 0
            WR(" ", "Green", "Cyan");  // 1
            WR(" ", "Green", "Yellow"); // 2
            WR(" ", "Green", "Red"); // 3
            WR(" ", "Green", "Green"); // 4
            WR(" ", "Green", "Blue"); // 5
            WR(" ", "Green", "White"); // 6
            WR(" ", "Green", "White"); // 7
            WR(" ", "Green", "White"); // 8
            WR(" ", "Green", "White"); // 9
            WR(" ", "Green", "DarkRed"); // 0
            WR(" ", "Green", "DarkMagenta"); // 1
            WRSetCursor(29, 12);
            WR("K H R O M A   C O D E R", "White", "Black");
            WRSetCursor(26, 13);
            WR("https://github.com/KarstSkarn", "DarkBlue", "Black");
            WRSetCursor(7, 27);
            WR("↑", "White", "Black");
            WRSetCursor(5, 28);
            WR("< ↓ >", "White", "Black");
            WRSetCursor(14, 28);
            WR("Use the ", "Gray", "Black");
            WR("ARROW KEYS, ESC", "White", "Black");
            WR(" and ", "Gray", "Black");
            WR("ENTER", "White", "Black");
            WR(" to navigate the menu", "Gray", "Black");
            WRSetCursor(31, 16);
            if (menuPointer != 0)
            {
                WR("-   DECODE FILE   -", "White", "Black");
            }
            else
            {
                WR("-   DECODE FILE   -", "Black", "White");
            }
            WRSetCursor(31, 18);
            if (menuPointer != 1)
            {
                WR("-   ENCODE FILE   -", "White", "Black");
            }
            else
            {
                WR("-   ENCODE FILE   -", "Black", "White");
            }
            WRSetCursor(31, 20);
            if (menuPointer != 2)
            {
                WR("-     OPTIONS     -", "White", "Black");
            }
            else
            {
                WR("-     OPTIONS     -", "Black", "White");
            }
            IntPtr currentWindowHandle = Process.GetCurrentProcess().MainWindowHandle;
            bool isFocused = KCIsWindowInFocus(currentWindowHandle);
            //if (isFocused) // TO DO: This isn't working at all so that's why I got it negated
            //{
                keyState = GetAsyncKeyState(38); // UP
                if (UIKeyDown(keyState))
                {
                    if ((menuPointer > 0) && !previousKeyStates[0])
                    {
                        menuPointer--;
                        keyState = 0;
                        previousKeyStates[0] = true;
                    }
                }
                else
                {
                    previousKeyStates[0] = false;
                }
                keyState = GetAsyncKeyState(40); // DOWN
                if (UIKeyDown(keyState))
                {
                    if ((menuPointer < 2) && !previousKeyStates[1])
                    {
                        menuPointer++;
                        keyState = 0;
                        previousKeyStates[1] = true;
                    }
                }
                else
                {
                    previousKeyStates[1] = false;
                }
                keyState = GetAsyncKeyState(37); // LEFT
                keyState = GetAsyncKeyState(39); // RIGHT
                keyState = GetAsyncKeyState(13); // ENTER
                if (UIKeyDown(keyState))
                {
                    if (menuPointer == 0)
                    {
                        menuStatus = 1;
                        break;
                    }
                    if (menuPointer == 1)
                    {
                        menuStatus = 2;
                        break;
                    }
                    if (menuPointer == 2)
                    {
                        menuStatus = 3;
                        break;
                    }
                }
                else
                {
                    previousKeyStates[3] = false;
                }
                keyState = GetAsyncKeyState(27); // ESC
                if (UIKeyDown(keyState))
                {
                    menuStatus = 9;
                    break;
                }
                UICheckWindowSize();
            //}
        }

    }
    static void UIDecodeScreen()
    {
        int keyState = 0;
        headerFound = false;
        incomingDensityType = "";
        checksumAlerts = 0;
        fatalAlerts = 0;
        confirmedFrames = 0;
        string decodeFilePath = "NULL";
        while (true)
        {
            WRSetCursor(0, 0);
            //          10        20        30        40        50        60        70         81
            //  123456789012345678901234567890123456789012345678901234567890123456789012345678901
            WR(" K", "Cyan", "Black");
            WR("HROMA CODER ", "White", "Black");
            WR("/", "Cyan", "Black");
            WR(" DECODE FILE", "Gray", "Black");
            WRSetCursor(0, 1);
            WR("─────────────────────────────────────────────────────────────────────────────────", "White", "Black");
            WRSetCursor(4, 3);
            if (decodeFilePath == "NULL")
            {
                WR("■ Select a file using the Windows Explorer ", "Gray", "Black");
                WRSetCursor(4, 4);
                OpenFileDialog openFileDialog = new OpenFileDialog();

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    decodeFilePath = openFileDialog.FileName;
                    WR("  Selected file: ", "Gray", "Black");
                    WR(KCGetFinalFileName(decodeFilePath), "Cyan", "Black");
                    if (decodeFilePath.Substring(decodeFilePath.Length - 3, 3) == "mp4")
                    {
                        WRSetCursor(4, 6);
                        WR("■ ", "Green", "Black");
                        WR("The file appears to be a correct <.mp4> file.", "Gray", "Black");
                    }
                    else
                    {
                        WRSetCursor(4, 6);
                        WR("■ ", "Red", "Black");
                        WR("Error: The file must be a valid <.mp4> file.", "Gray", "Black");
                        WRSetCursor(4, 7);
                        WR("■ ", "Red", "Black");
                        WR("Please, select a valid file.", "Gray", "Black");
                        Thread.Sleep(1000);
                        break;
                    }
                }
                if ((decodeFilePath == "NULL") || (decodeFilePath == ""))
                {
                    menuStatus = 0;
                    break;
                }
                WRSetCursor(4, 7);
                WR("■ ", "Cyan", "Black");
                WR("The process will start shortly...", "Gray", "Black");
                Thread.Sleep(1000);
                Console.Clear();
            }
            else
            {
                WRSetCursor(4, 3);
                WR("■ ", "Cyan", "Black");
                WR("Decoding: ", "Gray", "Black");
                WR(KCGetFinalFileName(decodeFilePath), "Cyan", "Black");
                KCReadVideoFrames(decodeFilePath);
                KCResetAllValues();
                Thread.Sleep(3000);
                menuStatus = 0;
                break;
            }
            WRSetCursor(14, 28);
            WR("Press and hold", "Gray", "Black");
            WR(" ESC ", "White", "Black");
            WR("to cancel the process at any moment", "Gray", "Black");
            keyState = GetAsyncKeyState(27); // ESC
            if (UIKeyDown(keyState))
            {
                menuStatus = 0;
                break;
            }
            UICheckWindowSize();
        }
    }
    static void UIEncodeScreen()
    {
        int keyState = 0;
        bool[] previousKeyStates = new bool[5];
        previousKeyStates[0] = false;
        previousKeyStates[1] = false;
        previousKeyStates[2] = false;
        string encodeFilePath = "NULL";
        while (true)
        {
            WRSetCursor(0, 0);
            //          10        20        30        40        50        60        70         81
            //  123456789012345678901234567890123456789012345678901234567890123456789012345678901
            WR(" K", "Cyan", "Black");
            WR("HROMA CODER ", "White", "Black");
            WR("/", "Cyan", "Black");
            WR(" ENCODE FILE", "Gray", "Black");
            WRSetCursor(0, 1);
            WR("─────────────────────────────────────────────────────────────────────────────────", "White", "Black");
            WRSetCursor(4, 3);
            if (encodeFilePath == "NULL")
            {
                WR("■ Select a file using the Windows Explorer ", "Gray", "Black");
                WRSetCursor(4, 4);
                OpenFileDialog openFileDialog = new OpenFileDialog();

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    encodeFilePath = openFileDialog.FileName;
                    WR("  Selected file: ", "Gray", "Black");
                    WR(KCGetFinalFileName(encodeFilePath), "Cyan", "Black");
                    WR(" - Parsed as: ", "Gray", "Black");
                    WR(KCRemoveSpecialCharacters(KCGetFinalFileName(encodeFilePath)), "Cyan", "Black");
                }
                WRSetCursor(4, 7);
                if ((encodeFilePath == "NULL") || (encodeFilePath == ""))
                {
                    menuStatus = 0;
                    break;
                }
                WR("■ ", "Cyan", "Black");
                WR("The process will start shortly...", "Gray", "Black");
                Thread.Sleep(1000);
                Console.Clear();
            }
            else
            {
                WRSetCursor(4, 3);
                WR("■ ", "Cyan", "Black");
                WR("Encoding: ", "Gray", "Black");
                WR(KCGetFinalFileName(encodeFilePath), "Cyan", "Black");
                WR(" - Parsed as: ", "Gray", "Black");
                WR(KCRemoveSpecialCharacters(KCGetFinalFileName(encodeFilePath)), "Cyan", "Black");
                encodingProcessStep = 0;
                KCGenerateVideoFrames(encodeFilePath, OUTW, OUTH, 2);
                encodingProcessStep = 1;
                UIUpdateEncodingStats(0, 0, 0);
                FFEncryptVideo(KCTEMPORARYDATADIRECTORY, framesPerSecond, KCRemoveSpecialCharacters(KCGetFinalFileName(encodeFilePath)));
                encodingProcessStep = 2;
                UIUpdateEncodingStats(0, 0, 0);
                KCClearTMPFiles();
                KCResetAllValues();
                Thread.Sleep(3000);
                menuStatus = 0;
                break;
            }
            WRSetCursor(14, 28);
            WR("Press and hold", "Gray", "Black");
            WR(" ESC ", "White", "Black");
            WR("to cancel the process at any moment", "Gray", "Black");
            keyState = GetAsyncKeyState(27); // ESC
            if (UIKeyDown(keyState))
            {
                menuStatus = 0;
                break;
            }
            UICheckWindowSize();
        }
    }
    static void UIOptionsScreen()
    {
        int keyState = 0;
        bool[] previousKeyStates = new bool[5];
        previousKeyStates[0] = false;
        previousKeyStates[1] = false;
        previousKeyStates[2] = false;
        while (true)
        {
            WRSetCursor(0, 0);
            //          10        20        30        40        50        60        70         81
            //  123456789012345678901234567890123456789012345678901234567890123456789012345678901
            WR(" K", "Cyan", "Black");
            WR("HROMA CODER ", "White", "Black");
            WR("/", "Cyan", "Black");
            WR(" OPTIONS", "Gray", "Black");
            WRSetCursor(0, 1);
            WR("─────────────────────────────────────────────────────────────────────────────────", "White", "Black");
            WRSetCursor(7, 27);
            WR("↑", "White", "Black");
            WRSetCursor(5, 28);
            WR("< ↓ >", "White", "Black");
            WRSetCursor(14, 28);
            WR("Use the ", "Gray", "Black");
            WR("ARROW KEYS, ESC", "White", "Black");
            WR(" and ", "Gray", "Black");
            WR("ENTER", "White", "Black");
            WR(" to navigate the menu", "Gray", "Black");
            WRSetCursor(4, 3);
            WR("■ DENSITY TYPE", "White", "Black");
            WRSetCursor(4, 5);
            if ((menuPointer == 0) && (menuHorizontalPointer == 0))
            {
                WR("-   MONO (1bppx)   -", "Black", "DarkRed");
            }
            else
            {
                if (densityType == 0)
                {
                    WR("-   MONO (1bppx)   -", "Black", "Gray");
                }
                else
                {
                    WR("-   MONO (1bppx)   -", "DarkGray", "Black");
                }
            }
            WRSetCursor(27, 5);
            if ((menuPointer == 0) && (menuHorizontalPointer == 1))
            {
                WR("-   DUAL (2bppx)   -", "Black", "DarkRed");
            }
            else
            {
                if (densityType == 1)
                {
                    WR("-   DUAL (2bppx)   -", "Black", "Gray");
                }
                else
                {
                    WR("-   DUAL (2bppx)   -", "DarkGray", "Black");
                }
            }
            WRSetCursor(4, 7);
            if ((menuPointer == 1) && (menuHorizontalPointer == 0))
            {
                WR("- HALFBYTE (4bppx) -", "Black", "Cyan");
            }
            else
            {
                if (densityType == 2)
                {
                    WR("- HALFBYTE (4bppx) -", "Black", "Gray");
                }
                else
                {
                    WR("- HALFBYTE (4bppx) -", "Gray", "Black");
                }
            }
            WRSetCursor(27, 7);
            if ((menuPointer == 1) && (menuHorizontalPointer == 1))
            {
                WR("- 2 BYTES (16bppx) -", "Black", "Cyan");
            }
            else
            {
                if (densityType == 3)
                {
                    WR("- 2 BYTES (16bppx) -", "Black", "Gray");
                }
                else
                {
                    WR("- 2 BYTES (16bppx) -", "Gray", "Black");
                }
            }
            WRSetCursor(4, 9);
            if ((menuPointer == 2) && (menuHorizontalPointer == 0))
            {
                WR("- 3 BYTES (24bppx) -", "Black", "Cyan");
            }
            else
            {
                if (densityType == 4)
                {
                    WR("- 3 BYTES (24bppx) -", "Black", "Gray");
                }
                else
                {
                    WR("- 3 BYTES (24bppx) -", "Gray", "Black");
                }
            }
            WRSetCursor(4, 11);
            WR("■ OUTPUT FRAMES PER SECOND", "White", "Black");
            WRSetCursor(4, 13);
            if ((menuPointer == 3) && (menuHorizontalPointer == 0))
            {
                WR("-      15 FPS      -", "Black", "Cyan");
            }
            else
            {
                if (framesPerSecond == 15)
                {
                    WR("-      15 FPS      -", "Black", "Gray");
                }
                else
                {
                    WR("-      15 FPS      -", "Gray", "Black");
                }
            }
            WRSetCursor(27, 13);
            if ((menuPointer == 3) && (menuHorizontalPointer == 1))
            {
                WR("-      30 FPS      -", "Black", "Cyan");
            }
            else
            {
                if (framesPerSecond == 30)
                {
                    WR("-      30 FPS      -", "Black", "Gray");
                }
                else
                {
                    WR("-      30 FPS      -", "Gray", "Black");
                }
            }
            WRSetCursor(4, 15);
            if ((menuPointer == 4) && (menuHorizontalPointer == 0))
            {
                WR("-      60 FPS      -", "Black", "Cyan");
            }
            else
            {
                if (framesPerSecond == 60)
                {
                    WR("-      60 FPS      -", "Black", "Gray");
                }
                else
                {
                    WR("-      60 FPS      -", "Gray", "Black");
                }
            }
            WRSetCursor(27, 15);
            if ((menuPointer == 4) && (menuHorizontalPointer == 1))
            {
                WR("-      120 FPS     -", "Black", "Cyan");
            }
            else
            {
                if (framesPerSecond == 120)
                {
                    WR("-      120 FPS     -", "Black", "Gray");
                }
                else
                {
                    WR("-      120 FPS     -", "Gray", "Black");
                }
            }
            WRSetCursor(4, 17);
            WR("■ CODEC", "White", "Black");
            WRSetCursor(4, 19);
            if ((menuPointer == 5) && (menuHorizontalPointer == 0))
            {
                WR("-       h264       -", "Black", "Cyan");
            }
            else
            {
                if (codec == "h264")
                {
                    WR("-       h264       -", "Black", "Gray");
                }
                else
                {
                    WR("-       h264       -", "Gray", "Black");
                }
            }
            WRSetCursor(27, 19);
            if ((menuPointer == 5) && (menuHorizontalPointer == 1))
            {
                WR("-       h265       -", "Black", "Cyan");
            }
            else
            {
                if (codec == "h265")
                {
                    WR("-       h265       -", "Black", "Gray");
                }
                else
                {
                    WR("-       h265       -", "Gray", "Black");
                }
            }
            WRSetCursor(4, 21);
            WR("■ YUV VALUES", "White", "Black");
            WRSetCursor(4, 23);
            if ((menuPointer == 6) && (menuHorizontalPointer == 0))
            {
                WR("-       AUTO       -", "Black", "Cyan");
            }
            else
            {
                if (yuvValue == "AUTO")
                {
                    WR("-       AUTO       -", "Black", "Gray");
                }
                else
                {
                    WR("-       AUTO       -", "Gray", "Black");
                }
            }
            WRSetCursor(27, 23);
            if ((menuPointer == 6) && (menuHorizontalPointer == 1))
            {
                WR("-      yuv444      -", "Black", "Cyan");
            }
            else
            {
                if (yuvValue == "yuv444")
                {
                    WR("-      yuv444      -", "Black", "Gray");
                }
                else
                {
                    WR("-      yuv444      -", "Gray", "Black");
                }
            }
            WRSetCursor(55, 3);
            WR("■ VIDEO SIZE", "White", "Black");
            WRSetCursor(55, 5);
            if ((menuPointer == 0) && (menuHorizontalPointer == 2))
            {
                WR("-    100 x 100     -", "Black", "Cyan");
            }
            else
            {
                if (OUTW == 100)
                {
                    WR("-    100 x 100     -", "Black", "Gray");
                }
                else
                {
                    WR("-    100 x 100     -", "Gray", "Black");
                }
            }
            WRSetCursor(55, 7);
            if ((menuPointer == 1) && (menuHorizontalPointer == 2))
            {
                WR("-    150 x 150     -", "Black", "Cyan");
            }
            else
            {
                if (OUTW == 150)
                {
                    WR("-    150 x 150     -", "Black", "Gray");
                }
                else
                {
                    WR("-    150 x 150     -", "Gray", "Black");
                }
            }
            WRSetCursor(55, 9);
            if ((menuPointer == 2) && (menuHorizontalPointer == 2))
            {
                WR("-    200 x 200     -", "Black", "Cyan");
            }
            else
            {
                if (OUTW == 200)
                {
                    WR("-    200 x 200     -", "Black", "Gray");
                }
                else
                {
                    WR("-    200 x 200     -", "Gray", "Black");
                }
            }
            IntPtr currentWindowHandle = Process.GetCurrentProcess().MainWindowHandle;
            bool isFocused = KCIsWindowInFocus(currentWindowHandle);
            //if (isFocused) // TO DO: This isn't working at all so that's why I got it negated
            //{
                keyState = GetAsyncKeyState(38); // UP
                if (UIKeyDown(keyState))
                {
                    if ((menuPointer > 0) && !previousKeyStates[0])
                    {
                        menuPointer--;
                        keyState = 0;
                        previousKeyStates[0] = true;
                        if ((menuPointer == 2) && (menuHorizontalPointer == 1))
                        {
                            menuPointer--;
                        }
                    }
                }
                else
                {
                    previousKeyStates[0] = false;
                }
                keyState = GetAsyncKeyState(40); // DOWN
                if (UIKeyDown(keyState))
                {
                    if ((menuPointer < 6) && !previousKeyStates[1])
                    {
                        menuPointer++;
                        keyState = 0;
                        previousKeyStates[1] = true;
                        if ((menuPointer == 2) && (menuHorizontalPointer == 1))
                        {
                            menuPointer++;
                        }
                    }
                }
                else
                {
                    previousKeyStates[1] = false;
                }
                keyState = GetAsyncKeyState(37); // LEFT
                if (UIKeyDown(keyState))
                {
                    if ((menuHorizontalPointer > 0) && !previousKeyStates[2])
                    {
                        menuHorizontalPointer--;
                        keyState = 0;
                        previousKeyStates[2] = true;
                        if ((menuPointer == 2) && (menuHorizontalPointer == 1))
                        {
                            menuHorizontalPointer--;
                        }
                    }
                }
                else
                {
                    previousKeyStates[2] = false;
                }
                keyState = GetAsyncKeyState(39); // RIGHT
                if (UIKeyDown(keyState))
                {
                    if ((menuHorizontalPointer < 2) && !previousKeyStates[3])
                    {
                        menuHorizontalPointer++;
                        keyState = 0;
                        previousKeyStates[3] = true;
                        if ((menuPointer == 2) && (menuHorizontalPointer == 1))
                        {
                            menuHorizontalPointer++;
                        }
                    }
                }
                else
                {
                    previousKeyStates[3] = false;
                }
                keyState = GetAsyncKeyState(13); // ENTER
                if (UIKeyDown(keyState))
                {
                    if ((menuPointer == 0) && (menuHorizontalPointer == 0))
                    {
                        //densityType = 0;
                    }
                    if ((menuPointer == 0) && (menuHorizontalPointer == 1))
                    {
                        //densityType = 1;
                    }
                    if ((menuPointer == 1) && (menuHorizontalPointer == 0))
                    {
                        densityType = 2;
                    }
                    if ((menuPointer == 1) && (menuHorizontalPointer == 1))
                    {
                        densityType = 3;
                    }
                    if ((menuPointer == 2) && (menuHorizontalPointer == 0))
                    {
                        densityType = 4;
                    }
                    if ((menuPointer == 0) && (menuHorizontalPointer == 2))
                    {
                        OUTW = 100;
                        OUTH = 100;
                    }
                    if ((menuPointer == 1) && (menuHorizontalPointer == 2))
                    {
                        OUTW = 150;
                        OUTH = 150;
                    }
                    if ((menuPointer == 2) && (menuHorizontalPointer == 2))
                    {
                        OUTW = 200;
                        OUTH = 200;
                    }
                    if ((menuPointer == 3) && (menuHorizontalPointer == 0))
                    {
                        framesPerSecond = 15;
                    }
                    if ((menuPointer == 3) && (menuHorizontalPointer == 1))
                    {
                        framesPerSecond = 30;
                    }
                    if ((menuPointer == 4) && (menuHorizontalPointer == 0))
                    {
                        framesPerSecond = 60;
                    }
                    if ((menuPointer == 4) && (menuHorizontalPointer == 1))
                    {
                        framesPerSecond = 120;
                    }
                    if ((menuPointer == 5) && (menuHorizontalPointer == 0))
                    {
                        codec = "h264";
                    }
                    if ((menuPointer == 5) && (menuHorizontalPointer == 1))
                    {
                        codec = "h265";
                    }
                    if ((menuPointer == 6) && (menuHorizontalPointer == 0))
                    {
                        yuvValue = "AUTO";
                    }
                    if ((menuPointer == 6) && (menuHorizontalPointer == 1))
                    {
                        yuvValue = "yuv444";
                    }
                }
                else
                {

                }
                keyState = GetAsyncKeyState(27); // ESC
                if (UIKeyDown(keyState))
                {
                    menuStatus = 0;
                    menuPointer = 0;
                    menuHorizontalPointer = 0;
                    Thread.Sleep(1000);
                    break;
                }
                UICheckWindowSize();
            //}
        }
    }
    // CONSOLE WRITING SIMPLIFICATION
    public static void WRL(string text, string FColor, string BColor)
    {
        if (text == String.Empty || FColor == String.Empty || BColor == String.Empty) { return; }
        Console.ForegroundColor = (ConsoleColor)Enum.Parse(typeof(ConsoleColor), FColor);
        Console.BackgroundColor = (ConsoleColor)Enum.Parse(typeof(ConsoleColor), BColor);
        System.Console.Write(text + Environment.NewLine);
        UIResetConsoleColor();
    }
    public static void WR(string text, string FColor, string BColor)
    {
        if (text == String.Empty || FColor == String.Empty || BColor == String.Empty) { return; }
        Console.ForegroundColor = (ConsoleColor)Enum.Parse(typeof(ConsoleColor), FColor);
        Console.BackgroundColor = (ConsoleColor)Enum.Parse(typeof(ConsoleColor), BColor);
        System.Console.Write(text);
        UIResetConsoleColor();
    }
    static void WRSetCursor(int x, int y)
    {
        if ((x <= Console.WindowWidth) && (y <= Console.WindowHeight))
        {
            Console.SetCursorPosition(x, y);
        }
    }
    public static void UIResetConsoleColor()
    {
        System.Console.ResetColor();
    }
    // CONSOLE GRAPHICAL FUNCTIONS
    static void UIFormatConsole()
    {
        Console.CursorVisible = false;
        Console.SetWindowSize(81, 30);
        Console.SetBufferSize(Console.WindowWidth, Console.WindowHeight);
    }
    static void UICheckWindowSize()
    {
        if ((Console.WindowWidth != 81) || (Console.WindowHeight != 30))
        {
            Console.Clear();
            UIFormatConsole();
        }
    }
    static bool UIKeyDown(int keyState)
    {
        bool state = false;
        state = (keyState & 0x8000) != 0;
        return state;
    }
    static void UIWriteLog(string text, byte argVerboseLevel)
    {
        if (argVerboseLevel <= verboseLevel)
        {
            Console.WriteLine(text);
        }
    }
    // KC INTERNAL FUNCTIONS
    public static bool KCIsWindowInFocus(IntPtr windowHandle)
    {
        IntPtr foregroundWindow = GetForegroundWindow();

        if (foregroundWindow == IntPtr.Zero)
            return false;

        GetWindowThreadProcessId(foregroundWindow, out uint foregroundProcessId);
        int currentProcessId = Process.GetCurrentProcess().Id;

        return foregroundProcessId == currentProcessId;
    }
    static void KCResetAllValues()
    {
        readStep = 0;
        x0Pos = 0;
        y0Pos = 0;
        x1Pos = 0;
        y1Pos = 0;
        frameCounter = 1;
        codedFrameNumber = -1;
        videoSizeX = 0;
        videoSizeY = 0;
        genericEndXPointer = 0;
        genericEndYPointer = 0;
        videoPosX = 0;
        videoPosY = 0;
        tmpCounterX = 0;
        tmpCounterY = 0;
        fileName = "NULL";
        readBuffer = "";
        checksumValue = "";
        checksumReadValue = "NULL";

        // VIDEO DECODING CONTROL VALUES
        headerFound = false;
        encodingProcessStep = 0;
        checksumAlerts = 0;
        confirmedFrames = 0;
        fatalAlerts = 0;
        bytesCounter = 0;
        incomingDensityType = "";
    }
    static int KCBinaryToDecimal(string binaryString)
    {
        int decimalValue = 0;
        int power = 0;

        for (int i = binaryString.Length - 1; i >= 0; i--)
        {
            if (binaryString[i] == '1')
            {
                decimalValue += (int)Math.Pow(2, power);
            }

            power++;
        }

        return decimalValue;
    }
    static string KCGetFinalFileName(string filePath)
    {
        string[] pathSegments = filePath.Split('\\', '/');
        string fileNameWithExtension = pathSegments[pathSegments.Length - 1];

        int lastIndex = fileNameWithExtension.LastIndexOf('.');
        if (lastIndex >= 0)
        {
            string fileName = fileNameWithExtension.Substring(0, lastIndex);
            return fileName;
        }

        return fileNameWithExtension;
    }
    static string KCRemoveSpecialCharacters(string input)
    {
        string noSpacesOrUnderscores = input.Replace(" ", "").Replace("_", "");
        string onlyBasicAscii = Regex.Replace(noSpacesOrUnderscores, @"[^\x00-\x7F]", "");

        return onlyBasicAscii;
    }
    static Color KCCodeChart(string binaryString)
    {
        Color RColor = new Color();

        if (densityType == 0)
        {
            if (binaryString == "0") { RColor = Color.FromArgb(255, 0, 255, 0); }
            if (binaryString == "1") { RColor = Color.FromArgb(255, 0, 0, 255); }
        }
        if (densityType == 2)
        {
            if (binaryString == "0000") { RColor = Color.FromArgb(255, 0, 0, 0); } // 00
            if (binaryString == "0001") { RColor = Color.FromArgb(255, 0, 0, 120); } // 01
            if (binaryString == "0010") { RColor = Color.FromArgb(255, 0, 0, 255); } // 02
            if (binaryString == "0011") { RColor = Color.FromArgb(255, 0, 120, 0); } // 03
            if (binaryString == "0100") { RColor = Color.FromArgb(255, 0, 120, 120); } // 04
            if (binaryString == "0101") { RColor = Color.FromArgb(255, 0, 120, 255); } // 05
            if (binaryString == "0110") { RColor = Color.FromArgb(255, 0, 255, 0); } // 06
            if (binaryString == "0111") { RColor = Color.FromArgb(255, 0, 255, 120); } // 07
            if (binaryString == "1000") { RColor = Color.FromArgb(255, 0, 255, 255); } // 08
            if (binaryString == "1001") { RColor = Color.FromArgb(255, 120, 0, 0); } // 09
            if (binaryString == "1010") { RColor = Color.FromArgb(255, 120, 0, 120); } // 10
            if (binaryString == "1011") { RColor = Color.FromArgb(255, 120, 0, 255); } // 11
            if (binaryString == "1100") { RColor = Color.FromArgb(255, 120, 120, 0); } // 12
            if (binaryString == "1101") { RColor = Color.FromArgb(255, 120, 120, 120); } // 13
            if (binaryString == "1110") { RColor = Color.FromArgb(255, 120, 120, 255); } // 14
            if (binaryString == "1111") { RColor = Color.FromArgb(255, 120, 255, 0); } // 15
        }
        if (densityType == 3)
        {
            if (binaryString.Length > 8)
            {
                string firstHalf = binaryString.Substring(0, 8);
                string secondHalf = binaryString.Substring(8, 8);
                int firstHalfDecimalValue = KCBinaryToDecimal(firstHalf);
                int secondHalfDecimalValue = KCBinaryToDecimal(secondHalf);
                RColor = Color.FromArgb(255, 0, Convert.ToByte(firstHalfDecimalValue), Convert.ToByte(secondHalfDecimalValue));
            }
            else
            {
                int decimalValue = KCBinaryToDecimal(binaryString);
                RColor = Color.FromArgb(255, 120, Convert.ToByte(decimalValue), 0);
            }
        }
        if (densityType == 4)
        {
            if (binaryString.Length == 16)
            {
                string firstHalf = binaryString.Substring(0, 8);
                string secondHalf = binaryString.Substring(8, 8);
                int firstHalfDecimalValue = KCBinaryToDecimal(firstHalf);
                int secondHalfDecimalValue = KCBinaryToDecimal(secondHalf);
                RColor = Color.FromArgb(255, 0, Convert.ToByte(firstHalfDecimalValue), Convert.ToByte(secondHalfDecimalValue));
            }
            else if (binaryString.Length == 8)
            {
                int decimalValue = KCBinaryToDecimal(binaryString);
                RColor = Color.FromArgb(255, 255, Convert.ToByte(decimalValue), 0);
            }
            else if (binaryString.Length == 24)
            {
                string firstHalf = binaryString.Substring(0, 8);
                string secondHalf = binaryString.Substring(8, 8);
                string thirdHalf = binaryString.Substring(16, 8);
                int firstHalfDecimalValue = KCBinaryToDecimal(firstHalf);
                int secondHalfDecimalValue = KCBinaryToDecimal(secondHalf);
                int thirdHalfDecimalValue = KCBinaryToDecimal(thirdHalf);
                RColor = Color.FromArgb(255, Convert.ToByte(firstHalfDecimalValue), Convert.ToByte(secondHalfDecimalValue), Convert.ToByte(thirdHalfDecimalValue));
            }
        }

        return RColor;
    }
    static void KCReadVideoFrames(string sourceFile)
    {
        KCResetFrameData(0);
        FFDecryptVideo(sourceFile);
    }
    static void KCGenerateVideoFrames(string sourceFile, int OW, int OH, byte densityMode)
    {
        Bitmap FBUFFER = new Bitmap(OW, OH);
        Bitmap AUXBUFF = new Bitmap(10, 10);
        int FCOUNTER = 1;
        int H_POIX = 0;
        int H_POIY = 0;
        int CYCLECOUNTER = 0;
        string[] TMP_STRARRAY = new string[2048];
        string BYTELIST = "";
        for (int i = 0; i < 2048; i++) { TMP_STRARRAY[i] = "NULL"; }
        Color TMP_COL = new Color();
        double BLISTSIZE = KCGetRawFileLength(sourceFile);
        double BLISTPOINTER = 0;
        if (BLISTSIZE >= 1024)
        {
            var BYTELISTHOLDER = KCReadRawFileData(sourceFile, 0, 1024);
            BYTELIST = BYTELISTHOLDER.Item1;
        }
        else
        {
            var BYTELISTHOLDER = KCReadRawFileData(sourceFile, 0, BLISTSIZE);
            BYTELIST = BYTELISTHOLDER.Item1;
        }

        while (true) // FRAME GENERATION LOOP
        {
            TMP_COL = Color.FromArgb(255, 255, 0, 255);
            for (int x = 0; x < OW; x++)
            {
                for (int y = 0; y < OH; y++)
                {
                    FBUFFER.SetPixel(x, y, TMP_COL);
                }
            }
            H_POIX = 0;
            H_POIY = 0;
            if (FCOUNTER == 1) // FIRST FRAME OPENING HEADER
            {
                var returnData = KCRawBinary("0", AUXBUFF, 0, 0, 10, 10);

                UIWriteLog("Format density type is set to [" + densityType.ToString() + "]", 2);
                if (densityType == 0) // 1 BIT DENSITY
                {
                    returnData = KCRawBinary("10100001", FBUFFER, H_POIX, H_POIY, OUTW, OUTH);
                    FBUFFER = returnData.Item1;
                    H_POIX = returnData.Item2;
                    H_POIY = returnData.Item3;
                }
                else if (densityType == 1) // 2 BIT DENSITY
                {
                    returnData = KCRawBinary("10100011", FBUFFER, H_POIX, H_POIY, OUTW, OUTH);
                    FBUFFER = returnData.Item1;
                    H_POIX = returnData.Item2;
                    H_POIY = returnData.Item3;
                }
                else if (densityType == 2) // 4 BIT DENSITY
                {
                    returnData = KCRawBinary("10100101", FBUFFER, H_POIX, H_POIY, OUTW, OUTH);
                    FBUFFER = returnData.Item1;
                    H_POIX = returnData.Item2;
                    H_POIY = returnData.Item3;
                }
                else if (densityType == 3) // 16 BIT DENSITY + 1 BIT CONTROL
                {
                    returnData = KCRawBinary("10101101", FBUFFER, H_POIX, H_POIY, OUTW, OUTH);
                    FBUFFER = returnData.Item1;
                    H_POIX = returnData.Item2;
                    H_POIY = returnData.Item3;
                }
                else if (densityType == 4) // 24 BIT DENSITY
                {
                    returnData = KCRawBinary("10101111", FBUFFER, H_POIX, H_POIY, OUTW, OUTH);
                    FBUFFER = returnData.Item1;
                    H_POIX = returnData.Item2;
                    H_POIY = returnData.Item3;
                }

                // FILE NAME ENCODING
                returnData = KCRawColorCode(KCRemoveSpecialCharacters(Path.GetFileName(sourceFile)), FBUFFER, H_POIX, H_POIY, OUTW, OUTH, false);
                FBUFFER = returnData.Item1;
                H_POIX = returnData.Item2;
                H_POIY = returnData.Item3;
                UIWriteLog("Encoded file name is: " + KCRemoveSpecialCharacters(Path.GetFileName(sourceFile)), 2);

                // 4 BIT SEPARATOR (1001)
                returnData = KCRawBinary("1001", FBUFFER, H_POIX, H_POIY, OUTW, OUTH);
                FBUFFER = returnData.Item1;
                H_POIX = returnData.Item2;
                H_POIY = returnData.Item3;

                // FRAME NUMBER ENCODING
                returnData = KCRawColorCode(FCOUNTER.ToString(), FBUFFER, H_POIX, H_POIY, OUTW, OUTH, false);
                FBUFFER = returnData.Item1;
                H_POIX = returnData.Item2;
                H_POIY = returnData.Item3;
                UIWriteLog("Encoding frame number [" + FCOUNTER.ToString() + "]", 2);

                // 4 BIT SEPARATOR (1010)
                returnData = KCRawBinary("1010", FBUFFER, H_POIX, H_POIY, OUTW, OUTH);
                FBUFFER = returnData.Item1;
                H_POIX = returnData.Item2;
                H_POIY = returnData.Item3;

                // W SIZE ENCODING
                returnData = KCRawColorCode(OUTW.ToString(), FBUFFER, H_POIX, H_POIY, OUTW, OUTH, false);
                FBUFFER = returnData.Item1;
                H_POIX = returnData.Item2;
                H_POIY = returnData.Item3;
                UIWriteLog("W Size is set to: " + OUTW.ToString(), 2);

                // 4 BIT SEPARATOR (1011)
                returnData = KCRawBinary("1011", FBUFFER, H_POIX, H_POIY, OUTW, OUTH);
                FBUFFER = returnData.Item1;
                H_POIX = returnData.Item2;
                H_POIY = returnData.Item3;

                // H SIZE ENCODING
                returnData = KCRawColorCode(OUTH.ToString(), FBUFFER, H_POIX, H_POIY, OUTW, OUTH, false);
                FBUFFER = returnData.Item1;
                H_POIX = returnData.Item2;
                H_POIY = returnData.Item3;
                UIWriteLog("H Size is set to: " + OUTH.ToString(), 2);

                // 4 BIT SEPARATOR (1101)
                returnData = KCRawBinary("1101", FBUFFER, H_POIX, H_POIY, OUTW, OUTH);
                FBUFFER = returnData.Item1;
                H_POIX = returnData.Item2;
                H_POIY = returnData.Item3;
            }
            if (FCOUNTER != 1) // "N" FRAME OPENING HEADER
            {
                // 4 BIT SEPARATOR (1101)
                var returnData = KCRawBinary("1101", FBUFFER, H_POIX, H_POIY, OUTW, OUTH);
                FBUFFER = returnData.Item1;
                H_POIX = returnData.Item2;
                H_POIY = returnData.Item3;

                // FRAME NUMBER ENCODING
                returnData = KCRawColorCode(FCOUNTER.ToString(), FBUFFER, H_POIX, H_POIY, OUTW, OUTH, false);
                FBUFFER = returnData.Item1;
                H_POIX = returnData.Item2;
                H_POIY = returnData.Item3;
                UIWriteLog("Encoding frame number [" + FCOUNTER.ToString() + "]", 2);

                // 4 BIT SEPARATOR (1101)
                returnData = KCRawBinary("1101", FBUFFER, H_POIX, H_POIY, OUTW, OUTH);
                FBUFFER = returnData.Item1;
                H_POIX = returnData.Item2;
                H_POIY = returnData.Item3;
            }
            while (true) // FILE DATA ENCODING AREA
            {
                if ((H_POIX == (OW - 8)) && (H_POIY == (OH - 1))) // END OF FRAME CHECKSUM ENCODING
                {
                    // 4 BIT SEPARATOR (1111)
                    var returnData = KCRawBinary("1111", FBUFFER, H_POIX, H_POIY, OUTW, OUTH);
                    FBUFFER = returnData.Item1;
                    H_POIX = returnData.Item2;
                    H_POIY = returnData.Item3;

                    // CHECKSUM RESULT ENCODING
                    returnData = KCRawColorCode(CHKSUMGet().ToString("X2"), FBUFFER, H_POIX, H_POIY, OUTW, OUTH, false);
                    FBUFFER = returnData.Item1;
                    UIWriteLog("Encoded Checksum <" + CHKSUMGet().ToString("X2") + ">", 2);
                    break;
                }
                else
                {
                    if ((BLISTPOINTER <= BLISTSIZE) || (BYTELIST.Length > 0)) // FILE DATA ENCODING
                    {
                        if (densityType == 0)
                        {
                            if (BYTELIST.Length >= 1)
                            {
                                string DATACHUNK = BYTELIST.Substring(0, 1);
                                BYTELIST = BYTELIST.Substring(1);
                                UIWriteLog("Data sequence list <" + DATACHUNK + ">", 3);
                                UIWriteLog("Checksum Status <" + CHKSUMGet().ToString("X2") + ">", 4);
                                CHKSUMAddValue(Convert.ToByte(DATACHUNK, 2));
                                var returnData = KCRawColorCode(DATACHUNK, FBUFFER, H_POIX, H_POIY, OUTW, OUTH, true);
                                FBUFFER = returnData.Item1;
                                CYCLECOUNTER++;
                                if ((CYCLECOUNTER % 8) == 0)
                                {
                                    BLISTPOINTER += 1;
                                }
                            }
                            else
                            {
                                if ((BLISTPOINTER + 1024) <= BLISTSIZE)
                                {
                                    var BYTELISTHOLDER = KCReadRawFileData(sourceFile, BLISTPOINTER, BLISTPOINTER + 1024);
                                    BYTELIST = BYTELISTHOLDER.Item1;
                                }
                                else
                                {
                                    var BYTELISTHOLDER = KCReadRawFileData(sourceFile, BLISTPOINTER, BLISTSIZE);
                                    BYTELIST = BYTELISTHOLDER.Item1;
                                }
                                H_POIX--;
                            }
                        }
                        if (densityType == 2)
                        {
                            if (BYTELIST.Length >= 4)
                            {
                                string DATACHUNK = BYTELIST.Substring(0, 4);
                                BYTELIST = BYTELIST.Substring(4);
                                UIWriteLog("Data sequence list <" + DATACHUNK + ">", 3);
                                UIWriteLog("Checksum Status <" + CHKSUMGet().ToString("X2") + ">", 4);
                                CHKSUMAddValue(Convert.ToByte(DATACHUNK, 2));
                                var returnData = KCRawColorCode(DATACHUNK, FBUFFER, H_POIX, H_POIY, OUTW, OUTH, true);
                                FBUFFER = returnData.Item1;
                                CYCLECOUNTER++;
                                if ((CYCLECOUNTER % 2) == 0)
                                {
                                    BLISTPOINTER += 1;
                                }
                            }
                            else
                            {
                                if ((BLISTPOINTER + 1024) <= BLISTSIZE)
                                {
                                    var BYTELISTHOLDER = KCReadRawFileData(sourceFile, BLISTPOINTER, BLISTPOINTER + 1024);
                                    BYTELIST = BYTELISTHOLDER.Item1;
                                }
                                else
                                {
                                    var BYTELISTHOLDER = KCReadRawFileData(sourceFile, BLISTPOINTER, BLISTSIZE);
                                    BYTELIST = BYTELISTHOLDER.Item1;
                                }
                                H_POIX--;
                            }
                        }
                        if (densityType == 3)
                        {
                            if (BYTELIST.Length >= 8)
                            {
                                if (BYTELIST.Length >= 16)
                                {
                                    string DATACHUNK = BYTELIST.Substring(0, 16);
                                    BYTELIST = BYTELIST.Substring(16);
                                    UIWriteLog("Data sequence list <" + DATACHUNK + ">", 3);
                                    UIWriteLog("Checksum Status <" + CHKSUMGet().ToString("X2") + ">", 4);
                                    CHKSUMAddValue(Convert.ToByte(DATACHUNK.Substring(0, 8), 2));
                                    CHKSUMAddValue(Convert.ToByte(DATACHUNK.Substring(8, 8), 2));
                                    var returnData = KCRawColorCode(DATACHUNK, FBUFFER, H_POIX, H_POIY, OUTW, OUTH, true);
                                    FBUFFER = returnData.Item1;
                                    CYCLECOUNTER++;
                                    BLISTPOINTER += 2;
                                }
                                if (BYTELIST.Length == 8)
                                {
                                    string DATACHUNK = BYTELIST.Substring(0, 8);
                                    BYTELIST = BYTELIST.Substring(8);
                                    UIWriteLog("Data sequence list <" + DATACHUNK + ">", 3);
                                    UIWriteLog("Checksum Status <" + CHKSUMGet().ToString("X2") + ">", 4);
                                    CHKSUMAddValue(Convert.ToByte(DATACHUNK, 2));
                                    var returnData = KCRawColorCode(DATACHUNK, FBUFFER, H_POIX, H_POIY, OUTW, OUTH, true);
                                    FBUFFER = returnData.Item1;
                                    CYCLECOUNTER++;
                                    BLISTPOINTER += 1;
                                }
                            }
                            else
                            {
                                if ((BLISTPOINTER + 1024) <= BLISTSIZE)
                                {
                                    var BYTELISTHOLDER = KCReadRawFileData(sourceFile, BLISTPOINTER, BLISTPOINTER + 1024);
                                    BYTELIST = BYTELISTHOLDER.Item1;
                                }
                                else
                                {
                                    var BYTELISTHOLDER = KCReadRawFileData(sourceFile, BLISTPOINTER, BLISTSIZE);
                                    BYTELIST = BYTELISTHOLDER.Item1;
                                }
                                H_POIX--;
                            }
                        }
                        if (densityType == 4)
                        {
                            if (BYTELIST.Length >= 8)
                            {
                                if (BYTELIST.Length >= 24)
                                {
                                    string RCHECK = BYTELIST.Substring(0, 8);
                                    int firstHalfDecimalValue = KCBinaryToDecimal(RCHECK);
                                    if ((firstHalfDecimalValue) != 0 && (firstHalfDecimalValue != 255))
                                    {
                                        string DATACHUNK = BYTELIST.Substring(0, 24);
                                        BYTELIST = BYTELIST.Substring(24);
                                        UIWriteLog("Data sequence list <" + DATACHUNK + ">", 3);
                                        UIWriteLog("Checksum Status <" + CHKSUMGet().ToString("X2") + ">", 4);
                                        CHKSUMAddValue(Convert.ToByte(DATACHUNK.Substring(0, 8), 2));
                                        CHKSUMAddValue(Convert.ToByte(DATACHUNK.Substring(8, 8), 2));
                                        CHKSUMAddValue(Convert.ToByte(DATACHUNK.Substring(16, 8), 2));
                                        var returnData = KCRawColorCode(DATACHUNK, FBUFFER, H_POIX, H_POIY, OUTW, OUTH, true);
                                        FBUFFER = returnData.Item1;
                                        CYCLECOUNTER++;
                                        BLISTPOINTER += 3;
                                    }
                                    else
                                    {
                                        string DATACHUNK = BYTELIST.Substring(0, 16);
                                        BYTELIST = BYTELIST.Substring(16);
                                        UIWriteLog("Data sequence list <" + DATACHUNK + ">", 3);
                                        UIWriteLog("Checksum Status <" + CHKSUMGet().ToString("X2") + ">", 4);
                                        CHKSUMAddValue(Convert.ToByte(DATACHUNK.Substring(0, 8), 2));
                                        CHKSUMAddValue(Convert.ToByte(DATACHUNK.Substring(8, 8), 2));
                                        var returnData = KCRawColorCode(DATACHUNK, FBUFFER, H_POIX, H_POIY, OUTW, OUTH, true);
                                        FBUFFER = returnData.Item1;
                                        CYCLECOUNTER++;
                                        BLISTPOINTER += 2;
                                    }
                                }
                                else if (BYTELIST.Length == 16)
                                {
                                    string DATACHUNK = BYTELIST.Substring(0, 16);
                                    BYTELIST = BYTELIST.Substring(16);
                                    UIWriteLog("Data sequence list <" + DATACHUNK + ">", 3);
                                    UIWriteLog("Checksum Status <" + CHKSUMGet().ToString("X2") + ">", 4);
                                    CHKSUMAddValue(Convert.ToByte(DATACHUNK.Substring(0, 8), 2));
                                    CHKSUMAddValue(Convert.ToByte(DATACHUNK.Substring(8, 8), 2));
                                    var returnData = KCRawColorCode(DATACHUNK, FBUFFER, H_POIX, H_POIY, OUTW, OUTH, true);
                                    FBUFFER = returnData.Item1;
                                    CYCLECOUNTER++;
                                    BLISTPOINTER += 2;
                                }
                                else if (BYTELIST.Length == 8)
                                {
                                    string DATACHUNK = BYTELIST.Substring(0, 8);
                                    BYTELIST = BYTELIST.Substring(8);
                                    UIWriteLog("Data sequence list <" + DATACHUNK + ">", 3);
                                    UIWriteLog("Checksum Status <" + CHKSUMGet().ToString("X2") + ">", 4);
                                    CHKSUMAddValue(Convert.ToByte(DATACHUNK, 2));
                                    var returnData = KCRawColorCode(DATACHUNK, FBUFFER, H_POIX, H_POIY, OUTW, OUTH, true);
                                    FBUFFER = returnData.Item1;
                                    CYCLECOUNTER++;
                                    BLISTPOINTER += 1;
                                }
                            }
                            else
                            {
                                if ((BLISTPOINTER + 1024) <= BLISTSIZE)
                                {
                                    if (BLISTPOINTER < BLISTSIZE)
                                    {
                                        var BYTELISTHOLDER = KCReadRawFileData(sourceFile, BLISTPOINTER, BLISTPOINTER + 1024);
                                        BYTELIST = BYTELISTHOLDER.Item1;
                                    }
                                }
                                else
                                {
                                    if (BLISTPOINTER < BLISTSIZE)
                                    {
                                        var BYTELISTHOLDER = KCReadRawFileData(sourceFile, BLISTPOINTER, BLISTSIZE);
                                        BYTELIST = BYTELISTHOLDER.Item1;
                                    }
                                }
                                H_POIX--;
                            }
                        }
                    }
                    else // END OF FILE ZERO FILL
                    {
                        FBUFFER.SetPixel(H_POIX, H_POIY, Color.Black);
                    }
                    if ((BLISTPOINTER == BLISTSIZE) && (BYTELIST.Length == 0)) // END OF FILE DATA ENCODING
                    {
                        // 4 BIT SEPARATOR (1010)
                        while (FBUFFER.GetPixel(H_POIX, H_POIY) != Color.FromArgb(255, 255, 0, 255))
                        {
                            H_POIX++;
                            if (H_POIX == (OW - 0)) { H_POIY += 1; H_POIX = 0; }
                            if (H_POIY == (OH - 0)) { break; }
                        }
                        var returnData = KCRawBinary("1010", FBUFFER, H_POIX, H_POIY, OUTW, OUTH);
                        FBUFFER = returnData.Item1;
                        H_POIX = returnData.Item2 - 1;
                        H_POIY = returnData.Item3;
                        FBUFFER.SetPixel(H_POIX, H_POIY, Color.Black);
                        BLISTPOINTER += 1;
                    }
                }
                H_POIX++;
                if (H_POIX == (OW - 0)) { H_POIY += 1; H_POIX = 0; }
                if (H_POIY == (OH - 0)) { break; }
            }
            // END OF ENCODED FRAME
            FBUFFER.Save(KCTEMPORARYDATADIRECTORY + FCOUNTER.ToString() + ".png", System.Drawing.Imaging.ImageFormat.Png);
            FCOUNTER++;
            CHKSUMReset();
            UIWriteLog("Data list status " + BLISTPOINTER.ToString() + " / " + BLISTSIZE.ToString(), 2);
            if (BLISTPOINTER >= BLISTSIZE)
            {
                UIWriteLog("Frame encoding been finished!", 2);
                break;
            }
            UIUpdateEncodingStats(BLISTPOINTER, BLISTSIZE, FCOUNTER);
        }
    }
    static (Bitmap, int, int, string) KCRawColorCode(string stringSource, Bitmap BMAP, int H_POIX, int H_POIY, int OW, int OH, bool avoidConversion)
    {
        string[] TMP_STRARRAY = new string[2048];
        string DEBUGSTRING = "";
        for (int i = 0; i < 2048; i++) { TMP_STRARRAY[i] = "NULL"; }
        if (avoidConversion == false)
        {
            TMP_STRARRAY = KCASCIIToBin(stringSource);
        }
        else
        {
            TMP_STRARRAY[0] = stringSource;
        }
        int TMP_COUNTER = 0;
        Color TMP_COL = new Color();
        while (true)
        {
            if (TMP_STRARRAY[TMP_COUNTER] != "NULL")
            {
                if (densityType <= 2)
                {
                    DEBUGSTRING = DEBUGSTRING + TMP_STRARRAY[TMP_COUNTER];
                    TMP_COL = KCCodeChart(TMP_STRARRAY[TMP_COUNTER]);
                    BMAP.SetPixel(H_POIX, H_POIY, TMP_COL);
                    TMP_COUNTER++;
                    H_POIX++;
                    if (H_POIX == OW) { H_POIY++; H_POIX = 0; }
                    if (H_POIY == OH) { break; }
                }
                else // DENSITY TYPES 3/4
                {
                    if ((densityType == 3) || (densityType == 4))
                    {
                        if ((TMP_COUNTER + 1) < 2048)
                        {
                            if (TMP_STRARRAY[TMP_COUNTER + 1] != "NULL")
                            {
                                TMP_COL = KCCodeChart(TMP_STRARRAY[TMP_COUNTER] + TMP_STRARRAY[TMP_COUNTER + 1]);
                                BMAP.SetPixel(H_POIX, H_POIY, TMP_COL);
                                TMP_COUNTER += 2;
                                H_POIX++;
                                if (H_POIX == OW) { H_POIY++; H_POIX = 0; }
                                if (H_POIY == OH) { break; }
                            }
                            else
                            {
                                TMP_COL = KCCodeChart(TMP_STRARRAY[TMP_COUNTER]);
                                BMAP.SetPixel(H_POIX, H_POIY, TMP_COL);
                                TMP_COUNTER++;
                                H_POIX++;
                                if (H_POIX == OW) { H_POIY++; H_POIX = 0; }
                                if (H_POIY == OH) { break; }
                            }
                        }
                    }
                }
            }
            else
            {
                break;
            }
            if (densityType == 2)
            {
                if (stringSource.Length == 4) { break; }
            }
        }
        return (BMAP, H_POIX, H_POIY, DEBUGSTRING);
    }
    static (Bitmap, int, int, string) KCRawBinary(string binarySource, Bitmap BMAP, int H_POIX, int H_POIY, int OW, int OH)
    {
        string SUBSTR;
        string DEBUGSTRING = "";
        for (int i = 0; i < binarySource.Length; i++)
        {
            SUBSTR = binarySource.Substring(i, 1);
            if (SUBSTR == "1") { BMAP.SetPixel(H_POIX, H_POIY, Color.White); }
            if (SUBSTR == "0") { BMAP.SetPixel(H_POIX, H_POIY, Color.Black); }
            DEBUGSTRING = DEBUGSTRING + SUBSTR;
            H_POIX++;
            if (H_POIX == OW) { H_POIY++; H_POIX = 0; }
            if (H_POIY == OH) { break; }
        }
        return (BMAP, H_POIX, H_POIY, DEBUGSTRING);
    }
    static void KCClearTMPFiles()
    {
        Directory.Delete(KCTEMPORARYDATADIRECTORY, true);
        Directory.CreateDirectory(KCTEMPORARYDATADIRECTORY);
    }
    static void KCUpdateFidelityAverage(byte goal, byte actualValue)
    {
        int diference = goal - actualValue;
        // UNUSED
    }
    public static double KCEuclideanDistance(byte r1, byte g1, byte b1, byte r2, byte g2, byte b2)
    {
        double deltaR = r2 - r1;
        double deltaG = g2 - g1;
        double deltaB = b2 - b1;

        double deltaRSquared = deltaR * deltaR;
        double deltaGSquared = deltaG * deltaG;
        double deltaBSquared = deltaB * deltaB;

        double distance = Math.Sqrt(deltaRSquared + deltaGSquared + deltaBSquared);

        return distance;
    }
    static string KCProcessPixelData(byte r, byte g, byte b)
    {
        string decodedString = "NULL";
        if (densityType == 0)
        {
            double distanceToG = KCEuclideanDistance(r, g, b, 0, 255, 0);
            double distanceToB = KCEuclideanDistance(r, g, b, 0, 0, 255);
            double distanceToW = KCEuclideanDistance(r, g, b, 255, 255, 255);
            if ((distanceToW < distanceToG) && (distanceToW < distanceToB))
            {
                if (distanceToG < distanceToB)
                {
                    decodedString = "0";
                    UIWriteLog("Pixel Decoding Values: R {" + r.ToString() + "}  G {" + g.ToString() + "}  B {" + b.ToString() + "}", 4);
                    UIWriteLog("Pixel Decoding Binary Value {" + decodedString.ToString() + "}", 4);
                }
                else if (distanceToG > distanceToB)
                {
                    decodedString = "1";
                    UIWriteLog("Pixel Decoding Values: R {" + r.ToString() + "}  G {" + g.ToString() + "}  B {" + b.ToString() + "}", 4);
                    UIWriteLog("Pixel Decoding Binary Value {" + decodedString.ToString() + "}", 4);
                }
                else
                {
                    decodedString = "0";
                    fatalAlerts++;
                }
            }
            else
            {
                decodedString = "♦";
            }
        }
        if (densityType == 2)
        {
            double[] proneList = new double[17];
            int minIndex = -1;
            for (int i = 0; i <= 16; i++)
            {
                proneList[i] = -1;
            }
            byte fullValue = 255; //220
            byte halfValue = 120; //100
            byte zeroValue = 0;
            // 00 - 0000 - 0 0 0
            if ((r <= (zeroValue + tolerance)) && (g <= (zeroValue + tolerance)) && (b <= (zeroValue + tolerance)))
            {
                KCUpdateFidelityAverage(0, r);
                KCUpdateFidelityAverage(0, g);
                KCUpdateFidelityAverage(0, b);
                proneList[0] = KCEuclideanDistance(zeroValue, zeroValue, zeroValue, r, g, b);
            }

            // 01 - 0001 - 0 0 120
            if ((r <= (zeroValue + tolerance)) && (g <= (zeroValue + tolerance)) && ((b >= (halfValue - tolerance)) || (b <= halfValue + tolerance)))
            {
                KCUpdateFidelityAverage(0, r);
                KCUpdateFidelityAverage(0, g);
                KCUpdateFidelityAverage(120, b);
                proneList[1] = KCEuclideanDistance(zeroValue, zeroValue, halfValue, r, g, b);
            }

            // 02 - 0010 - 0 0 255
            if ((r <= (zeroValue + tolerance)) && (g <= (zeroValue + tolerance)) && ((b >= (fullValue - tolerance)) || (b <= fullValue + tolerance)))
            {
                KCUpdateFidelityAverage(0, r);
                KCUpdateFidelityAverage(0, g);
                KCUpdateFidelityAverage(255, b);
                proneList[2] = KCEuclideanDistance(zeroValue, zeroValue, fullValue, r, g, b);
            }

            // 03 - 0011 - 0 120 0
            if ((r <= (zeroValue + tolerance)) && ((g >= (halfValue - tolerance)) || (g <= halfValue + tolerance)) && (b <= (zeroValue + tolerance)))
            {
                KCUpdateFidelityAverage(0, r);
                KCUpdateFidelityAverage(120, g);
                KCUpdateFidelityAverage(0, b);
                proneList[3] = KCEuclideanDistance(zeroValue, halfValue, zeroValue, r, g, b);
            }

            // 04 - 0100 - 0 120 120
            if ((r <= (zeroValue + tolerance)) && ((g >= (halfValue - tolerance)) || (g <= halfValue + tolerance)) && ((b >= (halfValue - tolerance)) || (b <= halfValue + tolerance)))
            {
                KCUpdateFidelityAverage(0, r);
                KCUpdateFidelityAverage(120, g);
                KCUpdateFidelityAverage(120, b);
                proneList[4] = KCEuclideanDistance(zeroValue, halfValue, halfValue, r, g, b);
            }

            // 05 - 0101 - 0 120 255
            if ((r <= (zeroValue + tolerance)) && ((g >= (halfValue - tolerance)) || (g <= halfValue + tolerance)) && ((b >= (fullValue - tolerance)) || (b <= fullValue + tolerance)))
            {
                KCUpdateFidelityAverage(0, r);
                KCUpdateFidelityAverage(120, g);
                KCUpdateFidelityAverage(255, b);
                proneList[5] = KCEuclideanDistance(zeroValue, halfValue, fullValue, r, g, b);
            }

            // 06 - 0110 - 0 255 0
            if ((r <= (zeroValue + tolerance)) && ((g >= (fullValue - tolerance)) || (g <= fullValue + tolerance)) && (b <= (zeroValue + tolerance)))
            {
                KCUpdateFidelityAverage(0, r);
                KCUpdateFidelityAverage(255, g);
                KCUpdateFidelityAverage(0, b);
                proneList[6] = KCEuclideanDistance(zeroValue, fullValue, halfValue, r, g, b);
            }

            // 07 - 0111 - 0 255 120
            if ((r <= (zeroValue + tolerance)) && ((g >= (fullValue - tolerance)) || (g <= fullValue + tolerance)) && ((b >= (halfValue - tolerance)) || (b <= halfValue + tolerance)))
            {
                KCUpdateFidelityAverage(0, r);
                KCUpdateFidelityAverage(255, g);
                KCUpdateFidelityAverage(120, b);
                proneList[7] = KCEuclideanDistance(zeroValue, fullValue, halfValue, r, g, b);
            }

            // 08 - 1000 - 0 255 255
            if ((r <= (zeroValue + tolerance)) && ((g >= (fullValue - tolerance)) || (g <= fullValue + tolerance)) && ((b >= (fullValue - tolerance)) || (b <= fullValue + tolerance)))
            {
                KCUpdateFidelityAverage(0, r);
                KCUpdateFidelityAverage(255, g);
                KCUpdateFidelityAverage(255, b);
                proneList[8] = KCEuclideanDistance(zeroValue, fullValue, fullValue, r, g, b);
            }

            // 09 - 1001 - 120 0 0
            if (((r >= (halfValue - tolerance)) || (r <= halfValue + tolerance)) && (g <= (zeroValue + tolerance)) && (b <= (zeroValue + tolerance)))
            {
                KCUpdateFidelityAverage(120, r);
                KCUpdateFidelityAverage(0, g);
                KCUpdateFidelityAverage(0, b);
                proneList[9] = KCEuclideanDistance(halfValue, zeroValue, zeroValue, r, g, b);
            }

            // 10 - 1010 - 120 0 120
            if (((r >= (halfValue - tolerance)) || (r <= halfValue + tolerance)) && (g <= (zeroValue + tolerance)) && ((b >= (halfValue - tolerance)) || (b <= halfValue + tolerance)))
            {
                KCUpdateFidelityAverage(120, r);
                KCUpdateFidelityAverage(0, g);
                KCUpdateFidelityAverage(120, b);
                proneList[10] = KCEuclideanDistance(halfValue, zeroValue, halfValue, r, g, b);
            }

            // 11 - 1011 - 120 0 255
            if (((r >= (halfValue - tolerance)) || (r <= halfValue + tolerance)) && (g <= (zeroValue + tolerance)) && ((b >= (fullValue - tolerance)) || (b <= fullValue + tolerance)))
            {
                KCUpdateFidelityAverage(120, r);
                KCUpdateFidelityAverage(0, g);
                KCUpdateFidelityAverage(255, b);
                proneList[11] = KCEuclideanDistance(halfValue, zeroValue, fullValue, r, g, b);
            }

            // 12 - 1100 - 120 120 0
            if (((r >= (halfValue - tolerance)) || (r <= halfValue + tolerance)) && ((g >= (halfValue - tolerance)) || (g <= halfValue + tolerance)) && (b <= (zeroValue + tolerance)))
            {
                KCUpdateFidelityAverage(120, r);
                KCUpdateFidelityAverage(120, g);
                KCUpdateFidelityAverage(0, b);
                proneList[12] = KCEuclideanDistance(halfValue, halfValue, zeroValue, r, g, b);
            }

            // 13 - 1101 - 120 120 120
            if (((r >= (halfValue - tolerance)) || (r <= halfValue + tolerance)) && ((g >= (halfValue - tolerance)) || (g <= halfValue + tolerance)) && ((b >= (halfValue - tolerance)) || (b <= halfValue + tolerance)))
            {
                KCUpdateFidelityAverage(120, r);
                KCUpdateFidelityAverage(120, g);
                KCUpdateFidelityAverage(120, b);
                proneList[13] = KCEuclideanDistance(halfValue, halfValue, halfValue, r, g, b);
            }

            // 14 - 1110 - 120 120 255
            if (((r >= (halfValue - tolerance)) || (r <= halfValue + tolerance)) && ((g >= (halfValue - tolerance)) || (g <= halfValue + tolerance)) && ((b >= (fullValue - tolerance)) || (b <= fullValue + tolerance)))
            {
                KCUpdateFidelityAverage(120, r);
                KCUpdateFidelityAverage(120, g);
                KCUpdateFidelityAverage(255, b);
                proneList[14] = KCEuclideanDistance(halfValue, halfValue, fullValue, r, g, b);
            }

            // 15 - 1111 - 120 255 0
            if (((r >= (halfValue - tolerance)) || (r <= halfValue + tolerance)) && ((g >= (fullValue - tolerance)) || (g <= fullValue + tolerance)) && (b <= (zeroValue + tolerance)))
            {
                KCUpdateFidelityAverage(120, r);
                KCUpdateFidelityAverage(255, g);
                KCUpdateFidelityAverage(0, b);
                proneList[15] = KCEuclideanDistance(halfValue, fullValue, zeroValue, r, g, b);
            }

            // 16 - WHITE - 255 255 255
            if (r >= (fullValue - tolerance) && g >= (fullValue - tolerance) && b >= (fullValue - tolerance))
            {
                KCUpdateFidelityAverage(0, r);
                KCUpdateFidelityAverage(0, g);
                KCUpdateFidelityAverage(0, b);
                proneList[16] = KCEuclideanDistance(fullValue, fullValue, fullValue, r, g, b);
            }

            for (int i = 0; i <= 16; i++)
            {
                if (proneList[i] != -1)
                {
                    proneList[i] = Math.Abs(proneList[i]);
                    UIWriteLog("Pixel Decoding Prone List {" + i.ToString() + "} = " + proneList[i].ToString(), 4);
                }
            }

            for (int i = 0; i <= 16; i++)
            {
                if (proneList[i] != -1)
                {
                    if (minIndex == -1 || proneList[i] < proneList[minIndex])
                    {
                        minIndex = i;
                    }
                }
            }

            if (minIndex == 0) { decodedString = "0000"; }
            if (minIndex == 1) { decodedString = "0001"; }
            if (minIndex == 2) { decodedString = "0010"; }
            if (minIndex == 3) { decodedString = "0011"; }
            if (minIndex == 4) { decodedString = "0100"; }
            if (minIndex == 5) { decodedString = "0101"; }
            if (minIndex == 6) { decodedString = "0110"; }
            if (minIndex == 7) { decodedString = "0111"; }
            if (minIndex == 8) { decodedString = "1000"; }
            if (minIndex == 9) { decodedString = "1001"; }
            if (minIndex == 10) { decodedString = "1010"; }
            if (minIndex == 11) { decodedString = "1011"; }
            if (minIndex == 12) { decodedString = "1100"; }
            if (minIndex == 13) { decodedString = "1101"; }
            if (minIndex == 14) { decodedString = "1110"; }
            if (minIndex == 15) { decodedString = "1111"; }
            if (minIndex == 16) { decodedString = "♦"; }
            UIWriteLog("Pixel Decoding Values: R {" + r.ToString() + "}  G {" + g.ToString() + "}  B {" + b.ToString() + "}", 4);
            UIWriteLog("Pixel Decoding Minimum Index {" + minIndex.ToString() + "} (" + decodedString + ")", 4);
        }
        if (densityType == 3)
        {
            if (r == 0)
            {
                decodedString = KCDecimalToBinary(g) + KCDecimalToBinary(b);
                UIWriteLog("Pixel Decoding Values: R {" + r.ToString() + "}  G {" + g.ToString() + "}  B {" + b.ToString() + "}", 4);
                UIWriteLog("Pixel Decoding Binary Value {" + decodedString.ToString() + "}", 4);
            }
            if (r == 120)
            {
                decodedString = KCDecimalToBinary(g);
                UIWriteLog("Pixel Decoding Values: R {" + r.ToString() + "}  G {" + g.ToString() + "}  B {" + b.ToString() + "}", 4);
                UIWriteLog("Pixel Decoding Binary Value {" + decodedString.ToString() + "}", 4);
            }
            if ((r == 255) && (g == 0) && (b == 255))
            {
                decodedString = "";
            }
            if ((r == 255) && (g == 255) && (b == 255))
            {
                decodedString = "♦";
            }
        }
        if (densityType == 4)
        {
            if (r == 255)
            {
                decodedString = KCDecimalToBinary(g);
                UIWriteLog("Pixel Decoding Values: R {" + r.ToString() + "}  G {" + g.ToString() + "}  B {" + b.ToString() + "}", 4);
                UIWriteLog("Pixel Decoding Binary Value {" + decodedString.ToString() + "}", 4);
            }
            if (r == 0)
            {
                decodedString = KCDecimalToBinary(g) + KCDecimalToBinary(b);
                UIWriteLog("Pixel Decoding Values: R {" + r.ToString() + "}  G {" + g.ToString() + "}  B {" + b.ToString() + "}", 4);
                UIWriteLog("Pixel Decoding Binary Value {" + decodedString.ToString() + "}", 4);
            }
            if ((r != 0) && (r != 255))
            {
                decodedString = KCDecimalToBinary(r) + KCDecimalToBinary(g) + KCDecimalToBinary(b);
                UIWriteLog("Pixel Decoding Values: R {" + r.ToString() + "}  G {" + g.ToString() + "}  B {" + b.ToString() + "}", 4);
                UIWriteLog("Pixel Decoding Binary Value {" + decodedString.ToString() + "}", 4);
            }
            if ((r == 255) && (g == 0) && (b == 255))
            {
                decodedString = "";
            }
            if ((r == 255) && (g == 255) && (b == 255))
            {
                decodedString = "♦";
            }
        }

        return decodedString;
    }
    static string KCDecimalToBinary(byte decimalValue)
    {
        string binary = "";

        while (decimalValue > 0)
        {
            int remainder = decimalValue % 2;
            binary = remainder.ToString() + binary;
            decimalValue /= 2;
        }

        while (binary.Length < 8)
        {
            binary = "0" + binary;
        }

        return binary;
    }
    static void KCFFProcessFrame(byte[] frameData, int xCorner, int yCorner, int width, int height)
    {
        int bytesPerPixel = 3; // RGB24
        CHKSUMReset();
        for (int y = xCorner; y < height; y++)
        {
            for (int x = yCorner; x < width; x++)
            {
                bool skipFlag = false;
                int index = (y * width + x) * bytesPerPixel;

                byte r = frameData[index];
                byte g = frameData[index + 1];
                byte b = frameData[index + 2];

                UIWriteLog("Pixel X {" + x.ToString() + "}  Y {" + y.ToString() + "}", 4);

                if (frameCounter == 1) // FIRST FRAME SEQUENCE
                {
                    if (!skipFlag)
                    {
                        if (readStep == 0) // FIRST FRAME HEADER SEARCH
                        {
                            readBuffer = readBuffer + KCReadRawBWBinary(r, g, b);
                            if (x == (width - 1))
                            {
                                UIWriteLog("Header search buffer (" + readBuffer + ")", 4);
                                if (readBuffer.Contains("10100101") || readBuffer.Contains("10100001") || readBuffer.Contains("10100011") || readBuffer.Contains("10101101") || readBuffer.Contains("10101111"))
                                {
                                    string matchHeaderType = "10100101";
                                    if (readBuffer.Contains("10100001"))
                                    {
                                        densityType = 0;
                                        matchHeaderType = "10100001";
                                        readStep = 1;
                                        headerFound = true;
                                    }
                                    if (readBuffer.Contains("10100011"))
                                    {
                                        densityType = 1;
                                        matchHeaderType = "10100011";
                                        readStep = 1;
                                        headerFound = true;
                                    }
                                    if (readBuffer.Contains("10100101"))
                                    {
                                        densityType = 2;
                                        matchHeaderType = "10100101";
                                        readStep = 1;
                                        headerFound = true;
                                    }
                                    if (readBuffer.Contains("10101101"))
                                    {
                                        densityType = 3;
                                        matchHeaderType = "10101101";
                                        readStep = 1;
                                        headerFound = true;
                                    }
                                    if (readBuffer.Contains("10101111"))
                                    {
                                        densityType = 4;
                                        matchHeaderType = "10101111";
                                        readStep = 1;
                                        headerFound = true;
                                    }
                                    var matchPosition = readBuffer.IndexOf(matchHeaderType);
                                    skipFlag = true;
                                    videoPosX = matchPosition;
                                    videoPosY = y;
                                    x = matchPosition + 7;
                                    readBuffer = "";
                                    UIWriteLog("Header found at {" + matchPosition.ToString() + " X}  {" + y.ToString() + " Y}", 2);
                                    UIWriteLog("Header states density type [" + densityType.ToString() + "]", 2);
                                }
                                else
                                {
                                    readBuffer = "";
                                }
                            }
                        }
                    }

                    if (!skipFlag)
                    {
                        if (readStep == 1) // FIRST FRAME FILE NAME SEARCH
                        {
                            string singleBuffer;
                            singleBuffer = KCProcessPixelData(r, g, b);
                            if (singleBuffer != "NULL")
                            {
                                if (singleBuffer == "♦")
                                {
                                    skipFlag = true;
                                    readStep = 2;
                                    try
                                    {
                                        fileName = KCBinToASCII(readBuffer);
                                    }
                                    catch
                                    {
                                        fatalAlerts++;
                                    }
                                    x--;
                                    genericEndXPointer = x;
                                    UIWriteLog("File name recovered binary is {" + readBuffer + "}", 4);
                                    UIWriteLog("File name will be: " + fileName, 3);
                                }
                                else
                                {
                                    readBuffer = readBuffer + singleBuffer;
                                    tmpCounterX = x;
                                    tmpCounterY = y;
                                }
                            }
                        }
                    }

                    if (!skipFlag)
                    {
                        if (readStep == 2) // FIRST FRAME FRAME NUMBER SEPARATOR SEARCH
                        {
                            readBuffer = readBuffer + KCReadRawBWBinary(r, g, b);
                            if (x == genericEndXPointer + 5)
                            {
                                if (readBuffer.Contains("1001"))
                                {
                                    skipFlag = true;
                                    readStep = 3;
                                    var matchPosition = x - 4;
                                    x = matchPosition + 3;
                                    readBuffer = "";
                                    UIWriteLog("Frame number found at {" + matchPosition.ToString() + " X}  {" + y.ToString() + " Y}", 3);
                                }
                                else
                                {
                                    readBuffer = "";
                                }
                            }
                        }
                    }

                    if (!skipFlag)
                    {
                        if (readStep == 3) // FIRST FRAME NUMBER VALUE SEARCH
                        {
                            string singleBuffer;
                            singleBuffer = KCProcessPixelData(r, g, b);
                            if (singleBuffer != "NULL")
                            {
                                if (singleBuffer == "♦")
                                {
                                    skipFlag = true;
                                    try
                                    {
                                        codedFrameNumber = Convert.ToInt32(KCBinToASCII(readBuffer));
                                    }
                                    catch
                                    {
                                        fatalAlerts++;
                                    }
                                    readStep = 4;
                                    x--;
                                    genericEndXPointer = x;
                                    UIWriteLog("Frame number binary value is {" + readBuffer + "}", 4);
                                    UIWriteLog("Frame number value is [" + codedFrameNumber.ToString() + "]", 3);
                                }
                                else
                                {
                                    readBuffer = readBuffer + singleBuffer;
                                    tmpCounterX = x;
                                    tmpCounterY = y;
                                }
                            }
                        }
                    }

                    if (!skipFlag)
                    {
                        if (readStep == 4) // FIRST FRAME VIDEO X SIZE SEPARATOR SEARCH
                        {
                            readBuffer = readBuffer + KCReadRawBWBinary(r, g, b);
                            if (x == genericEndXPointer + 5)
                            {
                                if (readBuffer.Contains("1010"))
                                {
                                    readStep = 5;
                                    skipFlag = true;
                                    var matchPosition = x - 4;
                                    x = matchPosition + 3;
                                    readBuffer = "";
                                    UIWriteLog("Video X Size separator found at {" + matchPosition.ToString() + " X}  {" + y.ToString() + " Y}", 3);
                                }
                                else
                                {
                                    readBuffer = "";
                                }
                            }
                        }
                    }

                    if (!skipFlag)
                    {
                        if (readStep == 5) // FIRST FRAME VIDEO X SIZE VALUE SEARCH
                        {
                            string singleBuffer = "";
                            singleBuffer = KCProcessPixelData(r, g, b);
                            if (singleBuffer != "NULL")
                            {
                                if (singleBuffer == "♦")
                                {
                                    skipFlag = true;
                                    readStep = 6;
                                    try
                                    {
                                        videoSizeX = Convert.ToInt32(KCBinToASCII(readBuffer));
                                    }
                                    catch
                                    {
                                        fatalAlerts++;
                                    }
                                    x--;
                                    genericEndXPointer = x;
                                    UIWriteLog("Video X Size binary value is {" + readBuffer + "]", 4);
                                    UIWriteLog("Video X Size value is [" + videoSizeX.ToString() + "]", 3);
                                }
                                else
                                {
                                    readBuffer = readBuffer + singleBuffer;
                                    tmpCounterX = x;
                                    tmpCounterY = y;
                                }
                            }
                        }
                    }

                    if (!skipFlag)
                    {
                        if (readStep == 6) // FIRST FRAME VIDEO Y SIZE SEPARATOR SEARCH
                        {
                            readBuffer = readBuffer + KCReadRawBWBinary(r, g, b);
                            if (x == genericEndXPointer + 5)
                            {
                                if (readBuffer.Contains("1011"))
                                {
                                    skipFlag = true;
                                    readStep = 7;
                                    var matchPosition = x - 4;
                                    x = matchPosition + 3;
                                    readBuffer = "";
                                    UIWriteLog("Video Y Size separator found at {" + matchPosition.ToString() + " X}  {" + y.ToString() + " Y}", 3);
                                }
                                else
                                {
                                    readBuffer = "";
                                }
                            }
                        }
                    }

                    if (!skipFlag)
                    {
                        if (readStep == 7) // FIRST FRAME VIDEO Y SIZE VALUE SEARCH
                        {
                            string singleBuffer = "";
                            singleBuffer = KCProcessPixelData(r, g, b);
                            if (singleBuffer != "NULL")
                            {
                                if (singleBuffer == "♦")
                                {
                                    skipFlag = true;
                                    readStep = 8;
                                    try
                                    {
                                        videoSizeY = Convert.ToInt32(KCBinToASCII(readBuffer));
                                    }
                                    catch
                                    {
                                        fatalAlerts++;
                                    }
                                    x--;
                                    genericEndXPointer = x;
                                    UIWriteLog("Video Y Size binary value is {" + readBuffer + "]", 4);
                                    UIWriteLog("Video Y Size value is [" + videoSizeY.ToString() + "]", 3);
                                }
                                else
                                {
                                    readBuffer = readBuffer + singleBuffer;
                                    tmpCounterX = x;
                                    tmpCounterY = y;
                                }
                            }
                        }
                    }

                    if (!skipFlag)
                    {
                        if (readStep == 8) // FIRST FRAME FILE CODE START SEPARATOR SEARCH
                        {
                            readBuffer = readBuffer + KCReadRawBWBinary(r, g, b);
                            if (x == genericEndXPointer + 5)
                            {
                                if (readBuffer.Contains("1101"))
                                {
                                    skipFlag = true;
                                    readStep = 10;
                                    x1Pos = videoPosX + videoSizeX;
                                    y1Pos = videoPosY + videoSizeY;
                                    var matchPosition = x - 4;
                                    videoPosX = matchPosition;
                                    videoPosY = y;
                                    x = matchPosition + 3;
                                    y = y;
                                    readBuffer = "";
                                    UIWriteLog("File Code separator found at {" + matchPosition.ToString() + " X}  {" + y.ToString() + " Y}", 3);
                                }
                                else
                                {
                                    readBuffer = "";
                                }
                            }
                        }
                    }

                    if (!skipFlag) // FILE CODE DECODING STEP
                    {
                        if (readStep == 10)
                        {
                            string singleBuffer = "";
                            singleBuffer = KCProcessPixelData(r, g, b);
                            if (singleBuffer != "NULL")
                            {
                                if (singleBuffer == "♦")
                                {
                                    skipFlag = true;
                                    try
                                    {
                                        KCWriteBinaryDataToFile(readBuffer, KCFILEOUTDIRECTORY + fileName);
                                    }
                                    catch
                                    {
                                        fatalAlerts++;
                                    }
                                    readStep = 11;
                                    x--;
                                    genericEndXPointer = x;
                                    UIWriteLog("Checksum separator found at {" + x.ToString() + "} X  {" + y.ToString() + " Y}", 3);
                                    UIWriteLog("File Data in this frame: " + (readBuffer.Length / 8).ToString() + " Bytes", 3);
                                }
                                else
                                {
                                    if (densityType <= 2)
                                    {
                                        CHKSUMAddValue(Convert.ToByte(singleBuffer, 2));
                                    }
                                    else
                                    {
                                        if (singleBuffer.Length == 24)
                                        {
                                            CHKSUMAddValue(Convert.ToByte(singleBuffer.Substring(0, 8), 2));
                                            CHKSUMAddValue(Convert.ToByte(singleBuffer.Substring(8, 8), 2));
                                            CHKSUMAddValue(Convert.ToByte(singleBuffer.Substring(16, 8), 2));
                                        }
                                        else if (singleBuffer.Length == 16)
                                        {
                                            CHKSUMAddValue(Convert.ToByte(singleBuffer.Substring(0, 8), 2));
                                            CHKSUMAddValue(Convert.ToByte(singleBuffer.Substring(8, 8), 2));
                                        }
                                        else
                                        {
                                            CHKSUMAddValue(Convert.ToByte(singleBuffer, 2));
                                        }
                                    }
                                    readBuffer = readBuffer + singleBuffer;
                                    tmpCounterX = x;
                                    tmpCounterY = y;
                                }
                            }
                        }
                    }

                    if (!skipFlag)
                    {
                        if (readStep == 11) // FIRST FRAME CHECKSUM SEPARATOR SEARCH
                        {
                            readBuffer = readBuffer + KCReadRawBWBinary(r, g, b);
                            if (x == genericEndXPointer + 5)
                            {
                                if (readBuffer.Contains("1111"))
                                {
                                    skipFlag = true;
                                    readStep = 12;
                                    var matchPosition = x - 4;
                                    x = matchPosition + 3;
                                    readBuffer = "";
                                    UIWriteLog("Checksum separator found at {" + matchPosition.ToString() + " X}  {" + y.ToString() + " Y}", 3);
                                }
                                else if (readBuffer.Contains("1010"))
                                {
                                    skipFlag = true;
                                    readStep = 200;
                                    var matchPosition = x - 4;
                                    x = matchPosition + 3;
                                    readBuffer = "";
                                    UIWriteLog("File end separator found at {" + x.ToString() + " X}  {" + y.ToString() + " Y}", 3);
                                }
                                else
                                {
                                    readBuffer = "";
                                }
                            }
                        }
                    }

                    if (!skipFlag)
                    {
                        if (readStep == 12) // FIRST FRAME CHECKSUM VALUE SEARCH
                        {
                            string singleBuffer = "";
                            singleBuffer = KCProcessPixelData(r, g, b);
                            if (singleBuffer != "NULL")
                            {
                                if (x == (width - 1))
                                {
                                    skipFlag = true;
                                    readBuffer = readBuffer + singleBuffer;
                                    checksumReadValue = readBuffer;
                                    try
                                    {
                                        checksumReadValue = KCBinToASCII(checksumReadValue);
                                    }
                                    catch
                                    {
                                        fatalAlerts++;
                                    }
                                    UIWriteLog("Checksum binary value is {" + readBuffer + "]", 4);
                                    UIWriteLog("Checksum value is [" + checksumReadValue + "]", 3);
                                    UIWriteLog("Checksum local internal value is [" + CHKSUMGet().ToString("X2") + "]", 3);
                                    if (CHKSUMGet().ToString("X2") == checksumReadValue.ToString())
                                    {
                                        confirmedFrames++;
                                        UIWriteLog("Checksum match!", 3);
                                        KCResetFrameData(100);
                                    }
                                    else
                                    {
                                        checksumAlerts++;
                                        KCResetFrameData(100);
                                    }
                                    genericEndXPointer = 0;
                                }
                                else
                                {
                                    readBuffer = readBuffer + singleBuffer;
                                    tmpCounterX = x;
                                    tmpCounterY = y;
                                }
                            }
                        }
                    }
                    if (!skipFlag)
                    {
                        if (readStep == 200) // FILE END CHECKSUM VALUE READ
                        {
                            string singleBuffer = "";
                            singleBuffer = KCProcessPixelData(r, g, b);
                            if (singleBuffer != "NULL")
                            {
                                if (singleBuffer == "♦")
                                {
                                    skipFlag = true;
                                    x--;
                                    genericEndXPointer = x;
                                    UIWriteLog("Checksum binary value is {" + readBuffer + "]", 4);
                                    UIWriteLog("Checksum value is [" + checksumReadValue + "]", 3);
                                    UIWriteLog("Checksum local internal value is [" + CHKSUMGet().ToString("X2") + "]", 3);
                                    if (CHKSUMGet().ToString("X2") == checksumReadValue)
                                    {
                                        UIWriteLog("Checksum match!", 3);
                                        KCResetFrameData(100);
                                    }
                                    else
                                    {
                                        KCResetFrameData(100);
                                    }
                                }
                                else
                                {
                                    readBuffer = readBuffer + singleBuffer;
                                    tmpCounterX = x;
                                    tmpCounterY = y;
                                }
                            }
                        }
                    }
                }
                if (frameCounter > 1) // THE REST OF FRAMES UNTIL THE END
                {
                    if (!skipFlag)
                    {
                        if (readStep == 100) // FRAME START SEPARATOR SEARCH
                        {
                            readBuffer = readBuffer + KCReadRawBWBinary(r, g, b);
                            if (x == genericEndXPointer + 5)
                            {
                                if (readBuffer.Contains("1101"))
                                {
                                    skipFlag = true;
                                    readStep = 101;
                                    var matchPosition = x - 5; // SMALL EXCEPTION
                                    x = matchPosition + 3;
                                    readBuffer = "";
                                    UIWriteLog("Frame start separator found at {" + matchPosition.ToString() + " X}  {" + y.ToString() + " Y}", 3);
                                }
                                else
                                {
                                    readBuffer = "";
                                }
                            }
                        }
                    }
                    if (!skipFlag)
                    {
                        if (readStep == 101) // FRAME NUMBER VALUE SEARCH
                        {
                            string singleBuffer = "";
                            singleBuffer = KCProcessPixelData(r, g, b);
                            if (singleBuffer != "NULL")
                            {
                                if (singleBuffer == "♦")
                                {
                                    skipFlag = true;
                                    readStep = 102;
                                    x--;
                                    genericEndXPointer = x;
                                    try
                                    {
                                        codedFrameNumber = Convert.ToInt32(KCBinToASCII(readBuffer));
                                    }
                                    catch
                                    {
                                        fatalAlerts++;
                                    }
                                    UIWriteLog("Frame number binary value is {" + readBuffer + "}", 4);
                                    UIWriteLog("Frame number value is [" + codedFrameNumber.ToString() + "]", 3);
                                }
                                else
                                {
                                    readBuffer = readBuffer + singleBuffer;
                                    tmpCounterX = x;
                                    tmpCounterY = y;
                                }
                            }
                        }
                    }

                    if (!skipFlag)
                    {
                        if (readStep == 102) // FILE CODE SEPARATOR SEARCH
                        {
                            readBuffer = readBuffer + KCReadRawBWBinary(r, g, b);
                            if (x == genericEndXPointer + 5)
                            {
                                if (readBuffer.Contains("1101"))
                                {
                                    skipFlag = true;
                                    readStep = 103;
                                    var matchPosition = x - 4;
                                    x = matchPosition + 3;
                                    readBuffer = "";
                                    UIWriteLog("File code separator found at {" + matchPosition.ToString() + " X}  {" + y.ToString() + " Y}", 3);
                                }
                                else
                                {
                                    readBuffer = "";
                                }
                            }
                        }
                    }

                    if (!skipFlag)
                    {
                        if (readStep == 103) // FRAME OR DATA END SEARCH
                        {
                            string singleBuffer = "";
                            singleBuffer = KCProcessPixelData(r, g, b);
                            if (singleBuffer != "NULL")
                            {
                                if (singleBuffer == "♦")
                                {
                                    skipFlag = true;
                                    readStep = 104;
                                    x--;
                                    genericEndXPointer = x;
                                    try
                                    {
                                        KCWriteBinaryDataToFile(readBuffer, KCFILEOUTDIRECTORY + fileName);
                                    }
                                    catch
                                    {
                                        fatalAlerts++;
                                    }
                                    UIWriteLog("Frame end separator found at {" + x.ToString() + " X}  {" + y.ToString() + " Y}", 3);
                                }
                                else
                                {
                                    if (densityType <= 2)
                                    {
                                        CHKSUMAddValue(Convert.ToByte(singleBuffer, 2));
                                    }
                                    else
                                    {
                                        if (singleBuffer.Length == 24)
                                        {
                                            CHKSUMAddValue(Convert.ToByte(singleBuffer.Substring(0, 8), 2));
                                            CHKSUMAddValue(Convert.ToByte(singleBuffer.Substring(8, 8), 2));
                                            CHKSUMAddValue(Convert.ToByte(singleBuffer.Substring(16, 8), 2));
                                        }
                                        else if (singleBuffer.Length == 16)
                                        {
                                            CHKSUMAddValue(Convert.ToByte(singleBuffer.Substring(0, 8), 2));
                                            CHKSUMAddValue(Convert.ToByte(singleBuffer.Substring(8, 8), 2));
                                        }
                                        else
                                        {
                                            CHKSUMAddValue(Convert.ToByte(singleBuffer, 2));
                                        }
                                    }
                                    readBuffer = readBuffer + singleBuffer;
                                    tmpCounterX = x;
                                    tmpCounterY = y;
                                }
                            }
                        }
                    }

                    if (!skipFlag)
                    {
                        if (readStep == 104)
                        {
                            readBuffer = readBuffer + KCReadRawBWBinary(r, g, b);
                            if (x == genericEndXPointer + 5)
                            {
                                if (readBuffer.Contains("1111"))
                                {
                                    skipFlag = true;
                                    readStep = 110;
                                    var matchPosition = x - 4;
                                    x = matchPosition + 3;
                                    readBuffer = "";
                                    UIWriteLog("Checksum separator found at {" + x.ToString() + " X}  {" + y.ToString() + " Y}", 3);
                                }
                                else if (readBuffer.Contains("1010"))
                                {
                                    skipFlag = true;
                                    readStep = 200;
                                    var matchPosition = x - 4;
                                    x = matchPosition + 3;
                                    readBuffer = "";
                                    UIWriteLog("File end separator found at {" + x.ToString() + " X}  {" + y.ToString() + " Y}", 3);
                                }
                                else
                                {
                                    readBuffer = "";
                                }
                            }
                        }
                    }

                    if (!skipFlag)
                    {
                        if (readStep == 110)
                        {
                            string singleBuffer;
                            singleBuffer = KCProcessPixelData(r, g, b);
                            if (singleBuffer != "NULL")
                            {
                                if (x == (width - 1))
                                {
                                    skipFlag = true;
                                    readBuffer = readBuffer + singleBuffer;
                                    checksumReadValue = readBuffer;
                                    checksumReadValue = KCBinToASCII(checksumReadValue);
                                    UIWriteLog("Checksum binary value is {" + readBuffer + "]", 4);
                                    UIWriteLog("Checksum value is [" + checksumReadValue + "]", 3);
                                    UIWriteLog("Checksum local internal value is [" + CHKSUMGet().ToString("X2") + "]", 3);
                                    if (CHKSUMGet().ToString("X2") == checksumReadValue.ToString())
                                    {
                                        UIWriteLog("Checksum match!", 3);
                                        KCResetFrameData(100);
                                        confirmedFrames++;
                                    }
                                    else
                                    {
                                        KCResetFrameData(100);
                                        checksumAlerts++;
                                    }
                                    genericEndXPointer = 0;
                                }
                                else
                                {
                                    readBuffer = readBuffer + singleBuffer;
                                    tmpCounterX = x;
                                    tmpCounterY = y;
                                }
                            }
                        }
                    }

                    if (!skipFlag)
                    {
                        if (readStep == 200) // FILE END CHECKSUM VALUE READ
                        {
                            string singleBuffer = "";
                            singleBuffer = KCProcessPixelData(r, g, b);
                            if (singleBuffer != "NULL")
                            {
                                if (singleBuffer == "♦")
                                {
                                    skipFlag = true;
                                    x--;
                                    genericEndXPointer = x;
                                    UIWriteLog("Checksum binary value is {" + readBuffer + "]", 4);
                                    UIWriteLog("Checksum value is [" + checksumReadValue + "]", 3);
                                    UIWriteLog("Checksum local internal value is [" + CHKSUMGet().ToString("X2") + "]", 3);
                                    if (CHKSUMGet().ToString("X2") == checksumReadValue)
                                    {
                                        UIWriteLog("Checksum match!", 3);
                                        KCResetFrameData(100);
                                    }
                                    else
                                    {
                                        KCResetFrameData(100);
                                    }
                                }
                                else
                                {
                                    readBuffer = readBuffer + singleBuffer;
                                    tmpCounterX = x;
                                    tmpCounterY = y;
                                }
                            }
                        }
                    }
                }
                // END OF FRAME PROCESS
            }
            // Y SECTION
        }
    }
    static double KCGetRawFileLength(string filePath)
    {
        FileInfo fileInfo = new FileInfo(filePath);
        return fileInfo.Length;
    }
    static (string, int, int) KCReadRawFileData(string filePath, double cIndex, double mIndex)
    {
        string BYTELIST = "";
        int currentIndex = 0;
        int fileLength = 0;

        if (densityType <= 5)
        {
            try
            {
                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    fileLength = Convert.ToInt32(KCGetRawFileLength(filePath));

                    byte[] buffer = new byte[Convert.ToInt32(mIndex) - Convert.ToInt32(cIndex)];
                    fs.Seek((long)cIndex, SeekOrigin.Begin);
                    int bytesRead = fs.Read(buffer, 0, buffer.Length);

                    for (int i = 0; i < bytesRead; i++)
                    {
                        byte b = buffer[i];
                        string binaryString = Convert.ToString(b, 2).PadLeft(8, '0');
                        BYTELIST += binaryString;
                        currentIndex++;
                    }
                }
            }
            catch (IOException e)
            {
                UIWriteLog("Error ocurred while reading the file <" + filePath + "> [" + e.Message + "]", 2);
                fatalAlerts++;
            }
        }
        return (BYTELIST, currentIndex, fileLength);
    }
    public static void KCWriteBinaryDataToFile(string binaryString, string fileName)
    {
        using (BinaryWriter writer = new BinaryWriter(File.Open(fileName, FileMode.Append)))
        {
            byte currentByte = 0;
            int bitsWritten = 0;

            foreach (char bitChar in binaryString)
            {
                if (bitChar != '0' && bitChar != '1')
                {
                    continue;
                }

                byte bit = (byte)(bitChar - '0');
                currentByte = (byte)((currentByte << 1) | bit);
                bitsWritten++;

                if (bitsWritten == 8)
                {
                    writer.Write(currentByte);
                    currentByte = 0;
                    bitsWritten = 0;
                }
            }

            if (bitsWritten > 0) // WRITE LAST INCOMPLETE BYTE IF ANY
            {
                currentByte <<= (8 - bitsWritten);
                writer.Write(currentByte);
            }
        }
    }
    public static string KCReadRawBWBinary(byte r, byte g, byte b)
    {
        string returnValue = "";
        if ((r <= (0 + bwtolerance)) && (g <= (0 + bwtolerance)) && (b <= (0 + bwtolerance)))
        {
            returnValue = "0";
            KCUpdateFidelityAverage(0, r);
            KCUpdateFidelityAverage(0, g);
            KCUpdateFidelityAverage(0, b);
        }
        if ((r >= (255 - bwtolerance)) && (g >= (255 - bwtolerance)) && (b >= (255 - bwtolerance)))
        {
            returnValue = "1";
            KCUpdateFidelityAverage(255, r);
            KCUpdateFidelityAverage(255, g);
            KCUpdateFidelityAverage(255, b);
        }
        return returnValue;
    }
    static string[] KCASCIIToBin(string inputString)
    {
        string[] wordOut = new string[200];
        for (int i = 0; i < 200; i++)
        {
            wordOut[i] = "NULL";
        }
        byte[] ASCIIBYTES = Encoding.ASCII.GetBytes(inputString);

        if (densityType == 0)
        {
            StringBuilder BINARYSTRING = new StringBuilder();
            foreach (byte b in ASCIIBYTES)
            {
                string BINARYBYTE = Convert.ToString(b, 2).PadLeft(8, '0');
                BINARYSTRING.Append(BINARYBYTE);
            }

            string BINARYRESULT = BINARYSTRING.ToString();

            for (int i = 0; i < wordOut.Length; i++)
            {
                if (i < BINARYRESULT.Length)
                {
                    wordOut[i] = BINARYRESULT.Substring(i / 8, 1);
                }
            }
        }
        if (densityType == 2)
        {
            StringBuilder BINARYSTRING = new StringBuilder();
            foreach (byte b in ASCIIBYTES)
            {
                string BINARYBYTE = Convert.ToString(b, 2).PadLeft(8, '0');
                BINARYSTRING.Append(BINARYBYTE);
            }

            string BINARYRESULT = BINARYSTRING.ToString();

            for (int i = 0; (i * 4 + 3) < BINARYRESULT.Length; i++)
            {
                if (i < 200)
                {
                    wordOut[i] = BINARYRESULT.Substring(i * 4, 4);
                }
            }
        }
        if ((densityType == 3) || (densityType == 4))
        {
            int index = 0;
            foreach (byte b in ASCIIBYTES)
            {
                string BINARYBYTE = Convert.ToString(b, 2).PadLeft(8, '0');
                wordOut[index] = BINARYBYTE;
                index++;
            }
        }
        return wordOut;
    }
    public static string KCBinToASCII(string binaryString)
    {
        string result = "";

        if (binaryString.Length >= 8)
        {
            for (int i = 0; i < binaryString.Length; i += 8)
            {
                if (binaryString.Length >= (i + 8))
                {
                    string binary = binaryString.Substring(i, 8);
                    if (KCIsBinaryString(binary))
                    {
                        int decimalValue = 0;
                        for (int j = 0; j < binary.Length; j++)
                        {
                            decimalValue = decimalValue * 2 + (binary[j] - '0');
                        }
                        char asciiChar = Convert.ToChar(decimalValue);
                        result += asciiChar;
                    }
                    else
                    {
                        return "";
                    }
                }
            }
        }
        else
        {
            return "";
        }
        return result;
    }
    static string[] KCSplitByteIntoHalfBytes(byte b)
    {
        string binary = Convert.ToString(b, 2).PadLeft(8, '0');

        string[] halfBytes = new string[2];
        halfBytes[0] = binary.Substring(0, 4);
        halfBytes[1] = binary.Substring(4, 4);

        return halfBytes;
    }
    static bool KCIsBinaryString(string str)
    {
        foreach (char c in str)
        {
            if (c != '0' && c != '1')
                return false;
        }
        return true;
    }
    static void KCResetReadData()
    {
        headerFound = false;
        encodingProcessStep = 0;
        checksumAlerts = 0;
        confirmedFrames = 0;
        fatalAlerts = 0;
        bytesCounter = 0;
        incomingDensityType = "";
    }
    static void KCResetFrameData(byte stepValue)
    {
        readStep = stepValue;
        genericEndXPointer = 0;
        genericEndYPointer = 0;
        codedFrameNumber = -1;
        CHKSUMReset();
    }
    static string KCGetFileNameWithoutExtension(string filePath)
    {
        string fileName = Path.GetFileName(filePath);
        int lastDotIndex = fileName.LastIndexOf('.');

        if (lastDotIndex >= 0)
        {
            fileName = fileName.Substring(0, lastDotIndex);
        }

        return fileName;
    }
    // CHKSUM FUNCTIONS
    static void CHKSUMAddValue(byte value)
    {
        CHKSUM += value;
        CHKSUM &= 0x0F0F0F0F;
    }
    static void CHKSUMReset()
    {
        CHKSUM = 0;
    }
    static uint CHKSUMGet()
    {
        return CHKSUM;
    }

    // FFMPEG FUNCTIONS
    static void FFEncryptVideo(string sourceFile, int fps, string originalFile)
    {
        string inputFolder = KCTEMPORARYDATADIRECTORY;
        string outputVideo = KCVIDEOOUTDIRECTORY + KCGetFileNameWithoutExtension(originalFile) + ".mp4";
        string arguments = "NULL";
        string codecUsed = "264";
        if (codec == "h264")
        {
            codecUsed = "264";
        }
        else
        {
            codecUsed = "265";
        }
        if (yuvValue == "AUTO")
        {
            if (densityType < 2)
            {
                arguments = $"-y -framerate " + fps + " -i " + inputFolder + "\\%d.png -vf scale='iw:ih:flags=none',unsharp " + "-c:v libx" + codecUsed + " -pix_fmt yuv420p " + outputVideo;
                //arguments = $"-y -framerate " + fps + " -i " + inputFolder + "\\%d.png -vf scale='iw:ih' -c:v libx" + codecUsed + " -pix_fmt yuv420p " + outputVideo;
            }
            if (densityType >= 2)
            {
                arguments = $"-y -framerate " + fps + " -i " +inputFolder + "\\%d.png -vf scale='iw:ih' -c:v libx" + codecUsed + "rgb -preset veryslow -crf 0 -pix_fmt rgb24 -colorspace rgb -color_primaries bt709 -color_trc bt709 -movflags +faststart " + outputVideo;
            }
        }
        else
        {
            arguments = $"-y -framerate " + fps + " -i " + inputFolder + "\\%d.png -vf scale='iw:ih' -c:v libx" + codecUsed + "rgb -preset veryslow -crf 0 -pix_fmt rgb24 -colorspace rgb -color_primaries bt709 -color_trc bt709 -movflags +faststart " + outputVideo;
        }

        FFRunMpegProcess(arguments);
        UIWriteLog("Video creation complete <" + outputVideo + ">", 2);
    }
    static void FFDecryptVideo(string videoPath)
    {
        Process MP4FrameCount = new Process();
        ProcessStartInfo startInfo = new ProcessStartInfo();

        startInfo.FileName = FFMPEGPROBEPATH;
        startInfo.Arguments = $"-v error -select_streams v:0 -count_packets -show_entries stream=nb_read_packets -of default=nokey=1:noprint_wrappers=1 \"{videoPath}\"";
        startInfo.RedirectStandardOutput = true;
        startInfo.UseShellExecute = false;
        startInfo.CreateNoWindow = true;

        MP4FrameCount.StartInfo = startInfo;
        MP4FrameCount.Start();

        string output = MP4FrameCount.StandardOutput.ReadToEnd();
        MP4FrameCount.WaitForExit();

        if (int.TryParse(output, out int totalFrames))
        {

        }
        else
        {
            totalFrames = 0;
        }

        Process MP4DecryptProcess = new Process();
        MP4DecryptProcess.StartInfo.FileName = FFMPEGPATH;
        MP4DecryptProcess.StartInfo.Arguments = $"-i \"{videoPath}\" -vf format=rgb24 -f rawvideo -";
        MP4DecryptProcess.StartInfo.RedirectStandardOutput = true;
        MP4DecryptProcess.StartInfo.UseShellExecute = false;
        MP4DecryptProcess.StartInfo.CreateNoWindow = true;

        MP4DecryptProcess.Start();

        using (Stream outputStream = MP4DecryptProcess.StandardOutput.BaseStream)
        {
            int width = FFGetVideoWidth(videoPath);
            int height = FFGetVideoHeight(videoPath);
            x0Pos = 0;
            y0Pos = 0;
            x1Pos = width;
            y1Pos = height;

            // CALCULATE THE FRAME SIZE IN BYTES (RGB24 FORMAT - 3 BYTES PER PIXEL)
            int frameSize = width * height * 3;

            byte[] buffer = new byte[frameSize];
            int bytesRead;

            int frameIndex = 1;

            // READ THE FRAMES ONE BY ONE
            while ((bytesRead = outputStream.Read(buffer, 0, frameSize)) > 0)
            {
                UIWriteLog("Frame number [" + frameCounter + "]", 2);
                KCFFProcessFrame(buffer, x0Pos, y0Pos, x1Pos, y1Pos);
                if (readStep == 0) { frameCounter = 1; } else { frameCounter++; }
                if (UIUpdateDecodingStats(frameIndex, totalFrames))
                {
                    break;
                }
                frameIndex++;
            }
        }

        MP4DecryptProcess.WaitForExit();
    }
    static void FFRunMpegProcess(string arguments)
    {
        Process MP4EncryptProcess = new Process();
        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = FFMPEGPATH,
            Arguments = arguments,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        MP4EncryptProcess.StartInfo = startInfo;
        MP4EncryptProcess.OutputDataReceived += FFProcessOutputDataReceived;
        MP4EncryptProcess.ErrorDataReceived += FFProcessErrorDataReceived;

        MP4EncryptProcess.Start();
        MP4EncryptProcess.BeginOutputReadLine();
        MP4EncryptProcess.BeginErrorReadLine();

        MP4EncryptProcess.WaitForExit();
    }
    static void FFProcessOutputDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (!string.IsNullOrEmpty(e.Data))
        {
            //Console.WriteLine($"FFmpeg output: {e.Data}");
            //Console.ReadLine();
        }
    }
    static void FFProcessErrorDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (!string.IsNullOrEmpty(e.Data))
        {
            //Console.WriteLine($"FFmpeg error: {e.Data}");
            //Console.ReadLine();
        }
    }
    static int FFGetVideoWidth(string videoPath)
    {
        Process GetWidthProcess = new Process();
        GetWidthProcess.StartInfo.FileName = FFMPEGPROBEPATH;
        GetWidthProcess.StartInfo.Arguments = $"-v error -select_streams v:0 -show_entries stream=width -of default=noprint_wrappers=1:nokey=1 \"{videoPath}\"";
        GetWidthProcess.StartInfo.RedirectStandardOutput = true;
        GetWidthProcess.StartInfo.UseShellExecute = false;
        GetWidthProcess.StartInfo.CreateNoWindow = true;

        GetWidthProcess.Start();

        string output = GetWidthProcess.StandardOutput.ReadToEnd();

        GetWidthProcess.WaitForExit();

        int width;
        if (int.TryParse(output, out width))
        {
            return width;
        }

        return 0;
    }
    static int FFGetVideoHeight(string videoPath)
    {
        Process GetHeightProcess = new Process();
        GetHeightProcess.StartInfo.FileName = FFMPEGPROBEPATH;
        GetHeightProcess.StartInfo.Arguments = $"-v error -select_streams v:0 -show_entries stream=height -of default=noprint_wrappers=1:nokey=1 \"{videoPath}\"";
        GetHeightProcess.StartInfo.RedirectStandardOutput = true;
        GetHeightProcess.StartInfo.UseShellExecute = false;
        GetHeightProcess.StartInfo.CreateNoWindow = true;

        GetHeightProcess.Start();

        string output = GetHeightProcess.StandardOutput.ReadToEnd();

        GetHeightProcess.WaitForExit();

        int height;
        if (int.TryParse(output, out height))
        {
            return height;
        }

        return 0;
    }
}