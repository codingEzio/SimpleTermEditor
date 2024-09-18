using System.Text;
using SimpleTermEditor.Entity.Enum;

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
    int quitConfirmTimes = EDITOR_QUIT_CONFIRM_TIMES;

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

        // Normal characters
        // Enter, Backspace+Delete
        // UpArrow, DownArrow, LeftArrow, RightArrow
        // Home, End
        // PageUp, PageDown
        // Ctrl+Q, Ctrl+S, Ctrl+F

        if (keyInfo.KeyChar >= 32 && keyInfo.KeyChar < 127)
        {
            InsertChar(keyInfo.KeyChar);
            return;
        }

        switch (keyInfo.Key)
        {
            case ConsoleKey.Enter:
                InsertNewline();
                break;

            case ConsoleKey.Backspace:
            case ConsoleKey.Delete:
                DeleteChar();
                break;

            case ConsoleKey.UpArrow:
                MoveCursor(Direction.Up);
                break;

            case ConsoleKey.DownArrow:
                MoveCursor(Direction.Down);
                break;

            case ConsoleKey.LeftArrow:
                MoveCursor(Direction.Left);
                break;

            case ConsoleKey.RightArrow:
                MoveCursor(Direction.Right);
                break;

            case ConsoleKey.Home:
                cursorX = 0;
                break;

            case ConsoleKey.End:
                cursorX = (cursorY < rows.Count) ? rows[cursorY].Size : 0;
                break;

            case ConsoleKey.PageUp:
            case ConsoleKey.PageDown:
                if (keyInfo.Key == ConsoleKey.PageUp)
                {
                    cursorY = rowOffset;
                } else if (keyInfo.Key == ConsoleKey.PageDown)
                {
                    cursorY = rowOffset  + screenRows - 1;
                }

                int times = screenRows;
                while (times-- > 0)
                {
                    MoveCursor(keyInfo.Key == ConsoleKey.PageUp ? Direction.Up : Direction.Down);
                }

                break;

            case ConsoleKey.S:
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
        }

        quitConfirmTimes = EDITOR_QUIT_CONFIRM_TIMES;
    }

    private void DrawRows(StringBuilder sb)
    {
        for (int y = 0; y < screenRows; y++)
        {
            int fileRow = y + rowOffset;
            if (fileRow >= rows.Count)
            {
                // Show welcome message if the file is empty
                if (rows.Count == 0 && y == screenRows / 3)
                {
                    string welcome = "Welcome to Simple Term Editor!";

                    // Truncate the welcome message if it's too long
                    if (welcome.Length > screenCols)
                    {
                        welcome = welcome.Substring(0, screenCols);
                    }

                    // Add padding to make sure that it's centered
                    int padding = (screenCols - welcome.Length) / 2;
                    if (padding > 0)
                    {
                        sb.Append('~');
                        padding--;
                    }

                    sb.Append(' ', padding);
                    sb.Append(welcome);
                }
                // Otherwise fill up the empty lines with '~'s
                else
                {
                    sb.Append('~');
                }
            }
            else
            {
                // Determine how much content could be shown on the screen
                int len = rows[fileRow].Render.Length - colOffset;
                if (len < 0)
                {
                    len = 0;
                }

                if (len > screenCols)
                {
                    len = screenCols;
                }

                sb.Append(rows[fileRow].Render.Substring(colOffset, len));
            }

            // Clear the line
            sb.Append("\x1b[K");

            sb.Append("\r\n");
        }
    }

    private void Scroll()
    {
        // The letters were the visible screen
        // The numbers and symbols were the boundaries
        //   @ 00000000000000 #
        //   @ AAAAAAAAAAAAAA #
        //   @ BBBBBBBBBBBBBB #
        //   @ CCCCCCCCCCCCCC #
        //   @ 11111111111111 #

        // Cursor moved above the visible screen, scroll up
        // Like you moved above the A,B,C to the '0' row
        if (cursorY < rowOffset)
        {
            rowOffset = cursorY;
        }

        // Cursor moved below the visible screen, scroll down
        // Like you moved below the A,B,C to the '1' row
        if (cursorY >= rowOffset + screenRows)
        {
            rowOffset = cursorY - screenRows + 1;
        }

        // Cursor moved left of the visible screen, scroll left
        // Like you moved left of the A,B,C to the '@' column
        if (cursorX < colOffset)
        {
            colOffset = cursorX;
        }

        // Cursor moved right of the visible screen, scroll right
        // Like you moved right of the A,B,C to the '#' column
        if (cursorX >= colOffset + screenCols)
        {
            colOffset = cursorX - screenCols + 1;
        }
    }

    private void MoveCursor(Direction direction)
    {
        switch (direction)
        {
            case Direction.Left:
                // Move cursor to the left if not reaching the start
                if (cursorX != 0)
                {
                    cursorX--;
                }
                //  Move the cursor to the end of the previous line if reaching the start
                else if (cursorY > 0)
                {
                    cursorY--;
                    cursorX = rows[cursorY].Size;
                }
                break;

            case Direction.Right:
                // Move cursor to the right if not reaching the end
                if (cursorY < rows.Count)
                {
                    cursorX++;
                }
                // Move the cursor to the start of the next line if exceeding
                else if (cursorX == rows[cursorY].Size)
                {
                    cursorY ++;
                    cursorX = 0;
                }
                break;

            case Direction.Up:
                // If not the first row of the file, move up one
                if (cursorY != 0)
                {
                    cursorY--;
                }
                break;

            case Direction.Down:
                // If still not the end of the file, move down one
                if (cursorY < rows.Count - 1)
                {
                    cursorY++;
                }

                break;
        }

        int rowLength = (cursorY < rows.Count) ? rows[cursorY].Size : 0;
        if (cursorX >= rowLength)
        {
            cursorX = rowLength;
        }
    }

    private void InsertChar(char c)
    {
        // If the cursor is at the end of the row, insert a new one
        if (cursorY == rows.Count)
        {
            InsertRow(rows.Count, string.Empty);
        }

        // Insert the character right after where the cursor is
        rows[cursorY].InsertChar(cursorX, c);

        // Move the cursor to the right one character
        cursorX++;

        // Changes made
        dirty++;
    }

    private void DeleteChar()
    {
        // The end of the file
        if (cursorY == rows.Count)
        {
            return;
        }
        // The very start of the file
        if (cursorX == 0 && cursorY == 0)
        {
            return;
        }

        // Cursor is not at the leftmost position
        //   Delete the character right before the cursor
        //   Move the cursor to the left one character
        if (cursorX > 0)
        {
            rows[cursorY].DeleteChar(cursorX - 1);
            cursorX--;
        }
        // Cursor is at the leftmost position
        //   Get the length of the previous row (for appending)
        //   Append the line where u at to the previous row
        //   Update the content of the previous row
        //   Delete the line where u at
        //   Move the cursor up one row
        else
        {
            cursorX = rows[cursorY - 1].Size;
            rows[cursorY - 1].Chars += rows[cursorY].Chars;

            UpdateRow(rows[cursorY - 1]);
            DeleteRow(cursorY);
            cursorY--;
        }

        dirty++;
    }

    /// <summary>
    ///  Prompt the user to input stuff to save/find (bottom of the screen)
    /// </summary>
    /// <param name="prompt"></param>
    /// <returns></returns>
    private string Prompt(string prompt)
    {
        Console.CursorVisible = true;
        SetStatusMessage(prompt);
        RefreshScreen();

        StringBuilder input = new StringBuilder();
        while (true)
        {
            RefreshScreen();
            var keyInfo = Console.ReadKey(true);

            // All these handles the input happened at the bottom (like 'Save As')

            // A scenario where you input the filename (for saving) and ENTER
            if (keyInfo.Key == ConsoleKey.Enter)
            {
                if (input.Length > 0)
                {
                    Console.CursorVisible = false;

                    SetStatusMessage("");
                    return input.ToString();
                }
            }
            // A scenario where you input the filename and edit it
            else if (keyInfo.Key == ConsoleKey.Backspace || keyInfo.Key == ConsoleKey.Delete)
            {
                if (input.Length > 0)
                {
                    input.Length--;
                }
            }
            // Nullify the input if you press ESC (not savin', gotta writin')
            else if (keyInfo.Key == ConsoleKey.Escape)
            {
                Console.CursorVisible = false;
                SetStatusMessage("");

                return null;
            }
            // Keep appending the input as long as it ain't control characters
            else if (!char.IsControl(keyInfo.KeyChar))
            {
                input.Append(keyInfo.KeyChar);
            }

            SetStatusMessage($"{prompt}{input}");
        }

    }

    public void Open(string fileName)
    {
        filename = fileName;

        try
        {
            string[] fileRows = File.ReadAllLines(filename);
            foreach (string line in fileRows)
            {
                InsertRow(numRows, line);
            }

            dirty = 0;
        }
        catch (Exception)
        {
            SetStatusMessage($"Cannot open file");
        }
    }

    private void Save()
    {
        if (string.IsNullOrEmpty(filename))
        {
            filename = Prompt("Save as: ");
            if (string.IsNullOrEmpty(filename))
            {
                SetStatusMessage("Save aborted");
                return;
            }
        }

        try
        {
            File.WriteAllLines(filename, GetRowsAsString());

            dirty = 0;
            SetStatusMessage($"{rows.Count} lines written to disk");
        }
        catch (Exception e)
        {
            SetStatusMessage($"Cannot save! I/O error: {e.Message}");
        }
    }

    private void Find(bool ignoreCase = true)
    {
        string query = Prompt("Search: ") ?? string.Empty;
        if (string.IsNullOrEmpty(query))
        {
            return;
        }

        for (int i = 0; i < rows.Count; i++)
        {
            int index = rows[i].Render.IndexOf(
                query,
                ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal
            );

            if (index >= 0)
            {
                cursorY = i;
                cursorX = index;
                rowOffset = rows.Count;

                break;
            }
        }
    }

    private List<string> GetRowsAsString()
    {
        List<string> fileRows = new List<string>();
        foreach (EditorRow row in rows)
        {
            fileRows.Add(row.Chars);
        }

        return fileRows;
    }

    public void SetStatusMessage(string message)
    {
        statusMessage = message;
        statusMessageTime = DateTime.Now;
    }

    private void DrawStatusBar(StringBuilder sb)
    {
        // An ANSI escape code called 'reverse video mode'
        // Swapping the background and foreground colors of the text
        sb.Append("\x1b[7m");

        string leftSectionStatus = $"{(string.IsNullOrEmpty(filename) ? "[No Name]" : filename)}" +
                        " - " +
                        $"{rows.Count} lines {(dirty > 0 ? "(modified)" : "")}";
        string rightSectionStatus = $"{cursorY + 1}/{rows.Count}";

        // Truncate the status message if it's too long then do the appending
        if (leftSectionStatus.Length > screenCols)
        {
            leftSectionStatus = leftSectionStatus.Substring(0, screenCols);
        }
        sb.Append(leftSectionStatus);

        // Basically make sure the right section is properly right-aligned
        // Until it reaches that, it would keep adding spaces.
        while (leftSectionStatus.Length < screenCols)
        {
            if ((screenCols - rightSectionStatus.Length) == rightSectionStatus.Length)
            {
                sb.Append(rightSectionStatus);

                break;
            }
            else
            {
                sb.Append(' ');
                leftSectionStatus += ' ';
            }
        }

        // Reset any text formatting it has before and move to the next line
        sb.Append("\x1b[m\r\n");
    }

    private void DrawMessageBar(StringBuilder sb)
    {
        // An ANSI escape code called 'reverse video mode'
        // Swapping the background and foreground colors of the text
        sb.Append("\x1b[K");

        if ((DateTime.Now - statusMessageTime).TotalSeconds < 5)
        {
            int msgLen = statusMessage.Length;
            if (msgLen > screenCols)
            {
                msgLen = screenCols;
            }

            sb.Append(statusMessage.Substring(0, msgLen));
        }
    }

    private void InsertNewline()
    {
        // If the cursor is at the very start of the line (or the file)
        if (cursorX == 0)
        {
            InsertRow(cursorX, string.Empty);
        }
        // If the cursor is NOT at the start the of the line
        else
        {
            // Grab content from the cursor to the end of the line
            string line = rows[cursorY].Chars.Substring(cursorX);

            // Grab content from the beginning of the line to the cursor
            rows[cursorY].Chars = rows[cursorY].Chars.Substring(0, cursorX);

            // Update the original line with the left half
            UpdateRow(rows[cursorY]);

            // Insert and update the new line with the right half
            InsertRow(cursorY + 1, line);
        }

        // Move the cursor to the beginning of the next line
        cursorY++;
        cursorX = 0;

        dirty++;
    }

    public void InsertRow(int at, string s)
    {
        if (at < 0 || at > rows.Count)
        {
            return;
        }

        // Insert the new row with attributes and update the number of rows
        rows.Insert(at, new EditorRow(s));
        numRows++;

        dirty++;
    }

    public void DeleteRow(int at)
    {
        if (at < 0 || at >= rows.Count)
        {
            return;
        }

        rows.RemoveAt(at);
        numRows--;

        dirty++;
    }

    public void UpdateRow(EditorRow row)
    {
        // Update the remaining attributes of an editor row
        // For the transformed text and the length of the text
        row.Render = row.Chars.Replace("\t", new string(' ', EDITOR_TAB_STOP));
        row.Size = row.Render.Length;
    }
}