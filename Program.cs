using Spectre.Console;
using TerminalTODO;

public class Program
{
    private static List<Note> notes = new List<Note>();
    private static readonly string default_footer_text = "[gray]Press 'n' to write new note[/]";
    private static string footer_text = "";
    private static int notes_start_index = 0;

    public static void Main(string[] args)
    {
        if (!File.Exists("notes.txt"))
        {
            File.CreateText("notes.txt");
        }

        notes = Note.GetAllNotes();
        MainLoop();
    }

    private static void MainLoop()
    {
        bool render_notes = true;
        Console.CursorVisible = false;

        while (true)
        {
            if (render_notes)
            {
                Console.Clear();
                RenderScreen();
                render_notes = false;
            }

            if (!Console.KeyAvailable) continue;
            ConsoleKeyInfo keyInfo = Console.ReadKey(true);

            switch (keyInfo.Key)
            {
                case ConsoleKey.N:
                    if (keyInfo.Modifiers.HasFlag(ConsoleModifiers.Control))
                    {
                        Note? note = new MuliLineNoteEditor().MainLoop();
                        if (note != null)
                        {
                            notes.Add(note);
                            note.Save();
                            render_notes = true;
                        }
                    }
                    else
                    {
                        FocusAddNoteSection();
                    }
                    break;
                case ConsoleKey.UpArrow:
                    if (notes_start_index > 0)
                    {
                        notes_start_index--;
                        render_notes = true;
                    }
                    break;
                case ConsoleKey.DownArrow:
                    if (notes_start_index < notes.Count - 1)
                    {
                        notes_start_index++;
                        render_notes = true;
                    }
                    break;
                case ConsoleKey.D:
                    if (!keyInfo.Modifiers.HasFlag(ConsoleModifiers.Control)) continue;
                    // Delete the top note
                    if (notes.Count == 0) continue;
                    notes[notes_start_index].Delete();
                    notes.RemoveAt(notes_start_index);
                    notes_start_index = Math.Max(0, notes_start_index - 1);
                    render_notes = true;
                    break;
                case ConsoleKey.Q:
                    if (!keyInfo.Modifiers.HasFlag(ConsoleModifiers.Control)) continue;
                    Console.Clear();
                    Environment.Exit(0);
                    break;
            }
        }
    }

    private static void FocusAddNoteSection()
    {
        Console.SetCursorPosition(8, Console.WindowHeight - 3);
        Console.Write(new string(' ', Console.WindowWidth - 9));
        Console.SetCursorPosition(8, Console.WindowHeight - 3);
        Console.CursorVisible = true;

        while (true)
        {
            if (!Console.KeyAvailable) continue;
            ConsoleKeyInfo keyInfo = Console.ReadKey(true);
            switch (keyInfo.Key)
            {
                case ConsoleKey.Enter:
                    if (footer_text.Length == 0) continue;
                    Console.CursorVisible = false;
                    var note = new Note(footer_text);
                    notes.Add(note);
                    note.Save();
                    footer_text = "";
                    RenderScreen();
                    return;
                case ConsoleKey.Escape:
                    Console.CursorVisible = false;
                    footer_text = "";
                    Console.SetCursorPosition(8, Console.WindowHeight - 3);
                    AnsiConsole.Write(new Markup(default_footer_text));
                    return;
                case ConsoleKey.Backspace:
                    if (keyInfo.Modifiers.HasFlag(ConsoleModifiers.Control))
                    {
                        if (footer_text.Length == 0)
                        {
                            Console.CursorVisible = true;
                            continue;
                        }

                        // Delete one word
                        Console.CursorVisible = false;
                        int delete_to = Math.Max(0, footer_text.LastIndexOf(' ') - 1);
                        Console.SetCursorPosition(8, Console.WindowHeight - 3);
                        if (delete_to == 0)
                        {
                            footer_text = "";
                        }
                        else
                        {
                            footer_text = footer_text.Remove(delete_to + 1);
                        }
                        AnsiConsole.Write(new Markup(footer_text + new string(' ', Console.WindowWidth - 8 - footer_text.Length), Color.Plum2));
                        Console.SetCursorPosition(8 + footer_text.Length, Console.WindowHeight - 3);
                    }
                    else if (footer_text.Length > 0)
                    {
                        footer_text = footer_text.Remove(footer_text.Length - 1);
                        Console.Write("\b \b");
                    }
                    Console.CursorVisible = true;
                    break;
                default:
                    if (char.IsLetterOrDigit(keyInfo.KeyChar) || "@#$%^&*()-_=+[]{};:'\",.<>/!?~ ".Contains(keyInfo.KeyChar))
                    {
                        footer_text += keyInfo.KeyChar;
                        AnsiConsole.Write(new Markup(keyInfo.KeyChar.ToString(), Color.Plum2));
                    }
                    break;
            }
        }
    }

    private static void RenderScreen()
    {
        Console.SetCursorPosition(0, 0);
        int lines = 0;
        int displayed_notes_count = 0;

        if (notes_start_index > 0)
        {
            AnsiConsole.Write(
                new Markup($"[plum2]+{notes_start_index} more[/]\n")
            );
        }

        foreach (var note in notes.GetRange(notes_start_index, notes.Count - notes_start_index))
        {
            // +2 for the panel border
            lines += note.Lines + 2;
            // If the note won't fit, don't render it
            // -4 for the new-note footer, -1 for the "+x more" message on the bottom, and a possible -1 for the "+x more" message on the top
            if (lines > Console.WindowHeight - 4 - 1 - (notes_start_index > 0 ? 1 : 0)) break;
            // Render the note if it fits
            note.Render();
            displayed_notes_count++;
        }
        // If there are more notes than can be displayed, show a "+x more" line
        if (lines > Console.WindowHeight - 4 - 1)
        {
            AnsiConsole.Write(
                new Markup($"[plum2]+{notes.Count - displayed_notes_count - notes_start_index} more[/]")
            );
        }

        // Render the new-note footer
        Console.SetCursorPosition(0, Console.WindowHeight - 4);
        AnsiConsole.Write(
            new Panel($"[blue]TODO:[/] [plum2]{default_footer_text}[/]")
                .Expand()
                .BorderColor(Color.Blue)
        );
    }
}