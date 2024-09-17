namespace SimpleTermEditor.Utility;

/// <summary>
///  A line of text in the editor with operations
/// </summary>
public class EditorRow
{
    /// <summary>
    ///  The original text
    /// </summary>
    public string Chars { get; set; }

    /// <summary>
    ///  The transformed text (like \t => N amount of spaces)
    /// </summary>
    public string Render { get; set; }

    /// <summary>
    ///  The length of the rendered/transformed text
    /// </summary>
    public int Size { get; set; }

    public EditorRow(string text)
    {
        Chars = text;
        Render = Chars.Replace("\t", new string(' ', Editor.EDITOR_TAB_STOP));
        Size = Render.Length;
    }

    public void InsertChar(int at, char c)
    {
        if (at < 0 || at > Chars.Length)
        {
            at = Chars.Length;
        }

        Chars = Chars.Insert(at, c.ToString());
        Render = Chars.Replace("\t", new string(' ', Editor.EDITOR_TAB_STOP));
        Size = Render.Length;
    }

    public void DeleteChar(int at)
    {
        if (at < 0 || at >= Chars.Length)
        {
            return;
        }

        Chars = Chars.Remove(at, 1);
        Render = Chars.Replace("\t", new string(' ', Editor.EDITOR_TAB_STOP));
        Size = Render.Length;
    }
}