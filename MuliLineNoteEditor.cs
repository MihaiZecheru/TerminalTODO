using Spectre.Console;

namespace TerminalTODO;

internal class MuliLineNoteEditor
{
    private List<List<char>> lines = new List<List<char>>();

    public MuliLineNoteEditor(string? footer_text = null)
    {
        lines.Add(new List<char>());
        Console.Clear();
        AnsiConsole.Write(new Panel("[plum2]Write Note[/]").Expand().BorderColor(Color.Blue).AsciiBorder());
        Console.CursorTop++;
        
        if (footer_text != null)
        {
            lines[0] = footer_text.ToList();
            Console.Write(footer_text);
        }

        Console.CursorVisible = true;
    }

    public Note? MainLoop()
    {
        while (true)
        {
            if (!Console.KeyAvailable) continue;
            ConsoleKeyInfo keyInfo = Console.ReadKey(true);

            // Quit
            if (keyInfo.Key == ConsoleKey.Q && keyInfo.Modifiers.HasFlag(ConsoleModifiers.Control))
            {
                return null;
            }

            // Help
            if (keyInfo.Key == ConsoleKey.H && keyInfo.Modifiers.HasFlag(ConsoleModifiers.Control))
            {
                Program.ShowHelpScreen();
                AnsiConsole.Write(new Panel("[plum2]Write Note[/]").Expand().BorderColor(Color.Blue).AsciiBorder());
                Console.CursorTop++;
                foreach (var line in lines)
                {
                    Console.WriteLine(string.Concat(line));
                }
                continue;
            }

            // Save with Alt+S or Ctrl+Enter
            if (
                (keyInfo.Key == ConsoleKey.S && keyInfo.Modifiers.HasFlag(ConsoleModifiers.Alt)) ||
                (keyInfo.Key == ConsoleKey.Enter && keyInfo.Modifiers.HasFlag(ConsoleModifiers.Control))
            ) {
                Console.CursorVisible = false;
                // Trim empty lines at end
                while (lines.Count > 1 && lines[lines.Count - 1].Count == 0)
                {
                    lines.RemoveAt(lines.Count - 1);
                }
                // Trim lines at start
                while (lines.Count > 1 && lines[0].Count == 0)
                {
                    lines.RemoveAt(0);
                }
                // Trim all lines
                for (int i = 0; i < lines.Count; i++)
                {
                    while (lines[i].Count > 0 && lines[i][lines[i].Count - 1] == ' ')
                    {
                        lines[i].RemoveAt(lines[i].Count - 1);
                    }
                }
                // Return the note if it has content
                if (lines.Count == 0 && lines[0].Count == 0) return null;
                return new Note(string.Join('\n', lines.Select(line => string.Concat(line))));
            }

            // Backspace
            if (keyInfo.Key == ConsoleKey.Backspace)
            {
                if (lines[lines.Count - 1].Count > 0)
                {
                    // Delete word
                    if (keyInfo.Modifiers.HasFlag(ConsoleModifiers.Control))
                    {
                        int delete_to = Math.Max(0, lines[lines.Count - 1].LastIndexOf(' '));
                        Console.SetCursorPosition(0, Console.CursorTop);
                        if (delete_to == 0)
                        {
                            if (lines.Count > 1)
                            {
                                // Delete line
                                Console.SetCursorPosition(0, Console.CursorTop);
                                Console.Write(new string(' ', Console.WindowWidth));
                                lines.RemoveAt(lines.Count - 1);
                                Console.SetCursorPosition(lines[lines.Count - 1].Count, Console.CursorTop - 1);
                            }
                            else
                            {
                                lines[lines.Count - 1] = new List<char>();
                                Console.SetCursorPosition(0, 4);
                                Console.Write(new string(' ', Console.WindowWidth));
                                Console.SetCursorPosition(0, 4);
                            }
                        }
                        else
                        {
                            string str = string.Concat(lines[lines.Count - 1]);
                            str = str.Substring(0, delete_to);
                            Console.Write(str + new string(' ', Console.WindowWidth - str.Length));
                            Console.CursorLeft = delete_to;
                            lines[lines.Count - 1] = str.ToList();
                        }
                    }
                    else
                    // Delete char
                    {
                        lines[lines.Count - 1].RemoveAt(lines[lines.Count - 1].Count - 1);
                        Console.Write("\b \b");
                    }
                }
                else
                {
                    // Delete line if it's empty
                    if (lines.Count > 1)
                    {
                        lines.RemoveAt(lines.Count - 1);
                        Console.SetCursorPosition(lines[lines.Count - 1].Count, Console.CursorTop - 1);
                    }
                }
            }

            // Enter
            if (keyInfo.Key == ConsoleKey.Enter)
            {
                lines.Add(new List<char>());
                Console.WriteLine();
            }

            // Type char
            if (char.IsLetterOrDigit(keyInfo.KeyChar) || "@#$%^&*()-_=+[]{};:'\",.<>/!?~ ".Contains(keyInfo.KeyChar))
            {
                if (lines[lines.Count - 1].Count >= Console.WindowWidth) continue;
                lines[lines.Count - 1].Add(keyInfo.KeyChar);
                Console.Write(keyInfo.KeyChar);
            }
        }
    }
}
