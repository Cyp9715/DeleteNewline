namespace Delete_Newline.Contracts.Structures;

public class KeybindPageConfiguration
{
    public required Keybind Keybind { get; set; }
    public required RegexChain RegexChain { get; set; }
}