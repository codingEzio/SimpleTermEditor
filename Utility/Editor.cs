using System.Text;

namespace SimpleTermEditor.Utility;

public class Editor
{
    #region VariableAndConstants

    /// <summary>
    ///  Meant to convert the tab size to the number of spaces
    /// </summary>
    public const int EDITOR_TAB_STOP = 8;

    /// <summary>
    ///  Amount of times to confirm quitting if editor is NOT blank slate
    /// </summary>
    public const int EDITOR_QUIT_CONFIRM_TIMES = 3;

    private int cursorX = 0, cursorY = 0;
    private int rowOffset = 0, colOffset = 0;

    // Mainly used for filling up the screen with '~'s and status messages
    private int screenRows = Console.WindowHeight - 2;
    private int screenCols = Console.WindowWidth;

    private int numRows = 0;
    private int dirty = 0;

    string filename = string.Empty;
    string statusMessage = string.Empty;

    DateTime statusMessageTime = DateTime.Now;
    private int quitConfirmTimes = EDITOR_QUIT_CONFIRM_TIMES;

    List<EditorRow> rows = new List<EditorRow>();
    Terminal terminal = new Terminal();

    #endregion

    public Editor()
    {
        // Switch the terminal to eagarly processing raw mode
        terminal.EnableRawMode();

        // Aww.. my sweet(ly registered) Multicast Delegates
        // Basically it disables the raw mode when the program exits
        AppDomain.CurrentDomain.ProcessExit += (s, e) => terminal.DisableRawMode();
    }

    /// <summary>
    ///  Update all the essential content on the screen
    ///  - the rows
    ///  - the cursor
    ///  - the status bar
    ///  - the message bar
    /// </summary>
    public void RefreshScreen()
    {
        Scroll();

        StringBuilder sb = new StringBuilder();

        // Hide the cursor
        sb.Append("\x1b[?25l");

        // Move the cursor to start of the line (== press HOME)
        sb.Append("\x1b[H");

        DrawRows(sb);
        DrawStatusBar(sb);
        DrawMessageBar(sb);

        // Move the cursor to the specific position
        sb.Append($"\x1b[{cursorY - rowOffset + 1};{cursorX - colOffset + 1}H");

        // Make the cursor visible
        sb.Append("\x1b[?25h");

        Console.Write(sb.ToString());
    }

    /// <summary>
    ///  Handle all the keypresses happened in the terminal
    ///  Either it's a character to be in, or an action to be taken
    /// </summary>
    public void ProcessKeypress()
    {
        var keyInfo = Console.ReadKey(true);

        // Enter, Backspace+Delete
        // UpArrow, DownArrow, LeftArrow, RightArrow
        // Home, End
        // PageUp, PageDown
        // Ctrl+Q, Ctrl+S, Ctrl+F
        // Normally inserting characters

        switch (keyInfo.Key)
        {
            case ControlKey.S:
                if ((keyInfo.Modifiers & ConsoleModifiers.Control) != 0)
                {
                    Save();
                }

                break;

            case ConsoleKey.F:
                if ((keyInfo.Modifiers & ConsoleModifiers.Control) != 0)
                {
                    Find();
                }

                break;

            case ConsoleKey.Q:
                if ((keyInfo.Modifiers & ConsoleModifiers.Control) != 0)
                {
                    if (dirty > 0 && quitConfirmTimes > 0)
                    {
                        Console.WriteLine($"WARNING: You have unsaved changes. Press Ctrl-Q {quitConfirmTimes} times again to quit.");
                        quitConfirmTimes--;

                        return;
                    }

                    Console.Clear();
                    Environment.Exit(0);
                }

                break;


            default:
                InsertChar(keyInfo.KeyChar);

                break;
        }

        quitConfirmTimes = EDITOR_QUIT_CONFIRM_TIMES;
    }
}