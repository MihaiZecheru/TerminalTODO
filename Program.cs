using Spectre.Console;
using TerminalTODO;

public class Program
{
    private static readonly string help_text = "Press Ctrl+H anywhere to open the help menu which will display the following:\n\nKeys on the main screen:\n- Ctrl+Q - Quit the app.\n- Ctrl+D - Delete the note at the top of the screen.\n- UpArrow - Scrolls up.\n- DownArrow - Scrolls down.\n- N - Focus the new note entry field where you can write down a new TODO note.\n- Ctrl+N - Open the multi-line new note entry editor.\n\nKeys in the note entry field:\n- Type to add characters. Alphanumeric plus some common special characters like @ or % are allowed.\n- Backspace to delete a char.\n- Ctrl+Backspace to delete a word.\n- Escape to unfocus the note entry field.\n- Enter to save the note.\n\nKeys in the multi-line new note entry editor:\n- Type to add characters. Alphanumeric plus some common special characters like @ or % are allowed.\n- Backspace to delete a char.\n- Ctrl+Backspace to delete a word.\n- Enter to insert a new line.\n- Alt+S to save the note.\n- Escape to go back to main screen, deleting the unfinished note.\n\n[plum2]Press any key to exit[/]";
    private static List<Note> notes = new List<Note>();
    private static readonly string default_footer_text = "[gray]Press 'n' to write new note[/]";
    private static string footer_text = "";
    private static int notes_start_index = 0;

    public static void Main(string[] args)
    {
        if (!File.Exists("notes.txt"))
        {
            File.CreateText("notes.txt");
            Console.Clear();
            Console.WriteLine("No notes file found. A new one was created, but you need to restart the program.");
            Environment.Exit(0);
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
            
            if (keyInfo.Key == ConsoleKey.N)
            {
                if (keyInfo.Key == ConsoleKey.N && keyInfo.Modifiers.HasFlag(ConsoleModifiers.Control))
                {
                    Note? note = new MuliLineNoteEditor().MainLoop();
                    if (note != null)
                    {
                        notes.Add(note);
                        note.Save();
                    }
                }
                else
                {
                    FocusAddNoteSection();
                }

                render_notes = true;
            }
            else if (keyInfo.Key == ConsoleKey.UpArrow)
            {
                if (notes_start_index > 0)
                {
                    notes_start_index--;
                    render_notes = true;
                }
            }
            else if (keyInfo.Key == ConsoleKey.DownArrow)
            {
                if (notes_start_index < notes.Count - 1)
                {
                    notes_start_index++;
                    render_notes = true;
                }
            }
            else if (keyInfo.Key == ConsoleKey.D && keyInfo.Modifiers.HasFlag(ConsoleModifiers.Control))
            {
                // Delete the top note
                if (notes.Count == 0) continue;
                notes[notes_start_index].Delete();
                notes.RemoveAt(notes_start_index);
                notes_start_index = Math.Max(0, notes_start_index - 1);
                render_notes = true;
            }
            else if (keyInfo.Key == ConsoleKey.Q && keyInfo.Modifiers.HasFlag(ConsoleModifiers.Control))
            {
                Console.Clear();
                Environment.Exit(0);
            }
            else if (keyInfo.Key == ConsoleKey.H && keyInfo.Modifiers.HasFlag(ConsoleModifiers.Control))
            {
                ShowHelpScreen();
                RenderScreen();
                continue;
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

            if (keyInfo.Key == ConsoleKey.Enter)
            {
                if (footer_text.Length == 0) continue;
                Console.CursorVisible = false;
                var note = new Note(footer_text);
                notes.Add(note);
                note.Save();
                footer_text = "";
                RenderScreen();
                return;
            }
            else if (keyInfo.Key == ConsoleKey.Escape)
            {
                Console.CursorVisible = false;
                footer_text = "";
                Console.SetCursorPosition(8, Console.WindowHeight - 3);
                AnsiConsole.Write(new Markup(default_footer_text));
                return;
            }
            else if (keyInfo.Key == ConsoleKey.Backspace)
            {
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
                    // Delete on char
                    footer_text = footer_text.Remove(footer_text.Length - 1);
                    Console.Write("\b \b");
                }

                Console.CursorVisible = true;
            }
            else if (keyInfo.Key == ConsoleKey.H && keyInfo.Modifiers.HasFlag(ConsoleModifiers.Control))
            {
                ShowHelpScreen();
                RenderScreen();
                Console.SetCursorPosition(8, Console.WindowHeight - 3);
                AnsiConsole.Write(new Markup(footer_text + new string(' ', Console.WindowWidth - footer_text.Length - 9), Color.Plum2));
                Console.SetCursorPosition(8 + footer_text.Length, Console.WindowHeight - 3);
                continue;
            }
            else if (keyInfo.Key == ConsoleKey.N && keyInfo.Modifiers.HasFlag(ConsoleModifiers.Control))
            {
                // Move the note to the multiline editor
                Console.CursorVisible = false;
                var note = new MuliLineNoteEditor(footer_text).MainLoop();
                if (note != null)
                {
                    notes.Add(note);
                    note.Save();
                    footer_text = "";
                    RenderScreen();
                    return;
                }
                else
                {
                    Console.CursorVisible = false;
                    footer_text = "";
                    Console.SetCursorPosition(8, Console.WindowHeight - 3);
                    AnsiConsole.Write(new Markup(default_footer_text));
                    return;
                }
            }
            else
            {
                if (char.IsLetterOrDigit(keyInfo.KeyChar) || "@#$%^&*()-_=+[]{};:'\",.<>/!?~ ".Contains(keyInfo.KeyChar))
                {
                    footer_text += keyInfo.KeyChar;
                    AnsiConsole.Write(new Markup(keyInfo.KeyChar.ToString(), Color.Plum2));
                }
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

    public static void ShowHelpScreen()
    {
        bool cursorVisible = Console.CursorVisible;
        Console.Clear();
        AnsiConsole.Cursor.Hide();
        AnsiConsole.Write(new Panel(new Markup(help_text, Color.Blue)).Expand().BorderColor(Color.Blue));
        Console.ReadKey(true); 
        if (cursorVisible) AnsiConsole.Cursor.Show();
        Console.Clear();
    }
}