namespace SimpleTermEditor.Utility;

public class Editor
{
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

    public Editor()
    {
        // Switch the terminal to eagarly processing raw mode
        terminal.EnableRawMode();

        // Aww.. my sweet(ly registered) Multicast Delegates
        // Basically it disables the raw mode when the program exits
        AppDomain.CurrentDomain.ProcessExit += (s, e) => terminal.DisableRawMode();
    }
}