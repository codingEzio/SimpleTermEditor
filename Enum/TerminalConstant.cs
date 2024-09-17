namespace SimpleTermEditor.Enum;

public class TerminalConstant
{
    [Flags]
    public enum InputFlags : uint
    {
        IGNBRK = 0x00000001,
        BRKINT = 0x00000002,
        ICRNL = 0x00000100,
        INPCK = 0x00000400,
        ISTRIP = 0x00001000
    }

    [Flags]
    public enum OutputFlags : uint
    {
        OPOST = 0x00000001
    }

    [Flags]
    public enum ControlFlags : uint
    {
        CS8 = 0x00000300
    }

    [Flags]
    public enum LocalFlags : uint
    {
        ECHO = 0x00000008,
        ICANON = 0x00000100,
        IEXTEN = 0x00000080,
        ISIG = 0x00000004
    }
}