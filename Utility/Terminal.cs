using System.Runtime.InteropServices;
using SimpleTermEditor.Enum;

namespace SimpleTermEditor.Utility;

public class Terminal
{
    [StructLayout(LayoutKind.Sequential)]
    private struct Termios
    {
        // Raw mode process input diff than canonical mode (by lines, EOL, EOF)
        // Raw mode process input eagerly (configure via `VMIN` and `VTIME`)
        //   VMIN = Minimum number of characters to read
        //   VTIME = Timeout in 100 milliseconds (0.1s)

        // These flags basically help us configure the terminal settings
        // Like not echoing the input, switching to raw mode from canonical mode
        public uint c_iflag;
        public uint c_oflag;
        public uint c_cflag;
        public uint c_lflag;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
        public byte c_cc;
    }

    /// <summary>
    ///  Save the original terminal settings (canonical mode)
    /// </summary>
    private Termios originalTermios;

    /// <summary>
    ///  C Code to get current terminal settings
    /// </summary>
    /// <param name="fd"></param>
    /// <param name="termios"></param>
    /// <returns></returns>
    [DllImport("libc")]
    private static extern int tcgetattr(int fd, out Termios termios);

    /// <summary>
    ///  C Code to set new terminal settings (like raw mode)
    /// </summary>
    /// <param name="fd"></param>
    /// <param name="optional_actions"></param>
    /// <param name="termios"></param>
    /// <returns></returns>
    [DllImport("libc")]
    private static extern int tcsetattr(int fd, int optional_actions, ref Termios termios);

    /// <summary>
    ///  Exit with an error message
    /// </summary>
    /// <param name="s"></param>
    public void Die(string s)
    {
        Console.Clear();
        Console.WriteLine($"{s}: {Marshal.GetLastWin32Error()}");

        Environment.Exit(1);
    }

    /// <summary>
    ///  Switch back to the canonical mode
    /// </summary>
    public void DisableRawMode()
    {
        int result = tcsetattr(0, 0, ref originalTermios);
        if (result < 0)
        {
            Die("tcsetattr");
        }
    }

    public void EnableRawMode()
    {
        int result = tcgetattr(0, out originalTermios);
        if (result < 0)
        {
            Die("tcgetattr");
        }

        Termios newTermios = originalTermios;

        // IGNBRK    #TODO what does this do?
        // BRKINT    #TODO what does this do?
        // ICRNL     Disable the terminal translation from CR to NL (carriage/newline)
        // INPCK     #TODO what does this do?
        // ISTRIP    #TODO what does this do?
        newTermios.c_iflag &= ~((uint)(TerminalConstant.InputFlags.IGNBRK | TerminalConstant.InputFlags.BRKINT | TerminalConstant.InputFlags.ICRNL | TerminalConstant.InputFlags.INPCK | TerminalConstant.InputFlags.ISTRIP));

        // OPOST     Disable the output postprocessing (mostly '\n' to '\r\n')
        newTermios.c_oflag &= ~((uint)TerminalConstant.OutputFlags.OPOST);

        // CS8       #TODO what does this do?
        newTermios.c_cflag |= (uint)TerminalConstant.ControlFlags.CS8;

        // ECHO      Disable echoing(printing out) of input (to the terminal)
        // ICANON    Disable the canonical mode, onto the raw mode!
        // IEXTEN    Disable processing keyboard 'Ctrl-V'
        newTermios.c_lflag &= ~((uint)(TerminalConstant.LocalFlags.ECHO | TerminalConstant.LocalFlags.ICANON | TerminalConstant.LocalFlags.IEXTEN | TerminalConstant.LocalFlags.ISIG));

        result = tcsetattr(0, 0, ref newTermios);
        if (result < 0)
        {
            Die("tcsetattr");
        }
    }
}