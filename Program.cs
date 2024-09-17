using SimpleTermEditor.Utility;

namespace SimpleTermEditor;

public class Program
{
    /// <summary>
    ///  A simple editor that supports basic text editing and navigation.
    /// </summary>
    /// <param name="args"></param>
    static void Main(string[] args)
    {
        // Either start a new file or open an existing file
        Editor editor = new Editor();
        if (args.Length > 0)
        {
            editor.Open(args[0]);
        }

        // The guidance message at the bottom
        editor.SetStatusMessage("HELP: "
            + "Ctrl-S: Save"
            + "Ctrl-Q: Quit"
            + "Ctrl-F: Find"
        );

        // Refresh the screen and process non-writing operations
        while (true)
        {
            editor.RefreshScreen();
            editor.ProcessKeypress();
        }
    }
}
