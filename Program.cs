using Spectre.Console;
using System.Runtime.InteropServices;
using TerminalTODO;

public class Program
{
    [DllImport("kernel32.dll")]
    private static extern IntPtr GetStdHandle(int nStdHandle);

    [DllImport("kernel32.dll")]
    private static extern bool SetConsoleMode(IntPtr hConsoleHandle, int dwMode);

    private static readonly string help_text = "Press Ctrl+H anywhere to open the help menu which will display the following:\n\nKeys on the main screen:\n- Ctrl+Q - Quit the app.\n- Ctrl+D - Delete the note at the top of the screen.\n- UpArrow - Scrolls up.\n- DownArrow - Scrolls down.\n- N - Focus the new note entry field where you can write down a new TODO note.\n- Ctrl+N - Open the multi-line new note entry editor.\n- Ctrl+Alt+Shift+L to sync notes across clients or to sync notes to the cloud\n\nKeys in the note entry field:\n- Type to add characters. Alphanumeric plus some common special characters like @ or % are allowed.\n- Backspace to delete a char.\n- Ctrl+Backspace to delete a word.\n- Escape to unfocus the note entry field.\n- Enter to save the note.\n\nKeys in the multi-line new note entry editor:\n- Type to add characters. Alphanumeric plus some common special characters like @ or % are allowed.\n- Backspace to delete a char.\n- Ctrl+Backspace to delete a word.\n- Enter to insert a new line.\n- Ctrl+Enter to save the note.\n- Ctrl+S also saves the note.\n- Escape to go back to main screen, deleting the unfinished note.\n\n[plum2]Press any key to exit[/]";
    private static List<Note> notes = new List<Note>();
    private static readonly string default_footer_text = "[grey70]Press 'n' to write new note[/]";
    private static string footer_text = "";
    private static int notes_start_index = 0;
    public static Guid? UUID = GetUUID();

    public static void Main(string[] args)
    {
        IntPtr consoleHandle = GetStdHandle(-10);

        // Set the console mode to disable processed input
        SetConsoleMode(consoleHandle, 0x0001);

        if (!File.Exists("notes.txt"))
        {
            File.CreateText("notes.txt");
            Console.Clear();
            Console.WriteLine("No notes file found. A new one was created for you, but you need to restart the program.");
            Environment.Exit(0);
        }
        FireSharpClient.TryPairingCodeAsync("123456").Wait();
        if (UUID != null)
        {
            notes = FireSharpClient.GetCloudNotes((Guid)UUID).Result;

            // If the pairing code no longer exists in the database, it means that the user is permanently linked to another client
            // Therefore, the pairing code is no longer needed
            if (File.Exists("pairing_code.txt") && FireSharpClient.TryPairingCodeAsync(File.ReadAllText("pairing_code.txt")).Result == null)
            {
                File.Delete("pairing_code.txt");
            }
        }
        else
        {
            notes = Note.GetAllNotes();
        }

        MainLoop();
    }

    private static async void MainLoop()
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
            else if (keyInfo.Key == ConsoleKey.Home)
            {
                if (notes_start_index > 0)
                {
                    notes_start_index = 0;
                    render_notes = true;
                }
            }
            else if (keyInfo.Key == ConsoleKey.End)
            {
                if (notes_start_index < notes.Count - 1)
                {
                    notes_start_index = notes.Count - 1;
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
            else if (keyInfo.Key == ConsoleKey.L && keyInfo.Modifiers.HasFlag(ConsoleModifiers.Control) && keyInfo.Modifiers.HasFlag(ConsoleModifiers.Alt) && keyInfo.Modifiers.HasFlag(ConsoleModifiers.Shift))
            {
                if (UUID != null)
                {
                    Console.Clear();

                    // The pairing code file only exists on the Master
                    // Only the Master is able to sync notes to the cloud
                    // The Slave can only link to a master.
                    // Therefore, if the pairing code file does not exist, the user is a Slave and therefore MUST be linked to another client
                    if (!File.Exists("pairing_code.txt"))
                    {
                        AnsiConsole.Markup($"You are already synced to another client. The relationship ID is [plum2]{UUID}[/]\n\nPress any key to quit.");
                        Console.ReadKey(true);
                        Environment.Exit(0);
                    }

                    // Once this point is reached, the client MUST be a master.
                    // Meaning, there is a possibility that the client has synced to the cloud but is not linked to another client
                    string pairing_code = File.ReadAllText("pairing_code.txt");

                    // If the code is unused (still exists, hasn't bene deleted), it means that the user is synced to the cloud but not paired with another client
                    if (FireSharpClient.TryPairingCodeAsync(pairing_code).Result == null)
                    {
                        AnsiConsole.Markup($"You are already synced to another client. The relationship ID is [plum2]{UUID}[/]\n\nPress any key to quit.");
                    }
                    else
                    {
                        AnsiConsole.Markup($"You are synced to the cloud but not linked with any client.\n\nTo link with another client, enter Ctrl+Alt+Shfit+L, select Slave, and enter the following code: [plum2]{pairing_code}[/]\n\nPress any key to exit.");
                    }

                    Console.ReadKey(true);
                }
                else
                {
                    AnsiConsole.Cursor.Hide();
                    BeginLinking();
                }

                Environment.Exit(0);
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
                var note = new Note(footer_text.Trim());
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
                // If the key is a letter or a digit and there is space, add it to the footer
                if (char.IsLetterOrDigit(keyInfo.KeyChar) || "@#$%^&*()-_=+{};:'\",.<>/!?~ ".Contains(keyInfo.KeyChar))
                {
                    if (footer_text.Length == Console.WindowWidth - 9) continue;
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
        int remaining_notes_on_bottom = notes.Count - displayed_notes_count - notes_start_index;
        if (remaining_notes_on_bottom > 0)
        {
            AnsiConsole.Write(
                new Markup($"[plum2]+{remaining_notes_on_bottom} more[/]")
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

    /// <summary>
    /// When the user presses Ctrl+Alt+Shift+L, the app will enter linking mode.
    /// This function is used to link two TerminalTODO clients together.
    /// </summary>
    private static void BeginLinking()
    {
        Console.Clear();
        AnsiConsole.Write(new Panel(new Markup("Linking mode", Color.Blue).Centered()).Expand().BorderColor(Color.Blue));
        AnsiConsole.Write("When pairing two clients, choose the 'Master' option on one of them, and the 'Slave' option on the other.\n" +
            "It doesn't matter which is which. The Master will give a pairing code, and the Slave will use that pairing code to link the two.\n\n" +
            "To instead sync your notes to the cloud (without linking another client), select Master then exit the application without pairing.\n\n" +
            "Note that once two clients are linked, they cannot be unlinked, nor can they be linked to other clients.\n\n");
        string choice = AnsiConsole.Prompt(new SelectionPrompt<string>()
            .Title("Choose the client's role")
            .AddChoices("Master", "Slave", "Quit")
        );

        if (choice == "Master")
        {
            DoMasterLinking().Wait();
        }
        else if (choice == "Slave")
        {
            DoSlaveLinking().Wait();
        }
        else
        {
            AnsiConsole.Write("\nYour notes were not synced to the cloud or with another client. Press any key to quit.");
            Console.ReadKey(true);
            Environment.Exit(0);
        }
    }

    /// <summary>
    /// Do the linking process for the master client. This will generate a random 6-digit pairing code and post it to the database so that the slave can pair to it.
    /// </summary>
    /// <see cref="BeginLinking"/>
    private static async Task DoMasterLinking()
    {
        // Random 6-digit number - leads to the link_UUID, makes it easier to pair as it's a shorter string
        string pairing_code;
        
        while (true)
        {
            pairing_code = new Random().Next(100000, 999999).ToString();
            if (await FireSharpClient.TryPairingCodeAsync(pairing_code) == null) break; // The code doesn't yet exist
        }

        // Save the pairing code so that it can be displayed to the user for pairing later on
        File.WriteAllText("pairing_code.txt", pairing_code);

        // The UUID is used in the database.
        Guid link_UUID = Guid.NewGuid();
        AnsiConsole.Markup($"(Master) - Your pairing code is: [plum2]{pairing_code}[/]\n\nEnter the code on a slave client to link the two.\n\n[red]Do not exit the app until the process is complete.[/]");

        // Post status code to database
        try
        {
            FireSharpClient.PostPairingCodeAsync(pairing_code, link_UUID.ToString()).Wait();
        }
        catch (Exception e)
        {
            AnsiConsole.Write(new Panel(new Markup($"An error occurred while trying to post the pairing code to the database: {e.Message}", Color.Red)).Expand().BorderColor(Color.Red));
            Console.ReadKey(true);
            Console.Clear();
            return;
        }

        SaveUUID(link_UUID);
        foreach (Note note in notes) await FireSharpClient.UploadNoteToCloud(link_UUID, note);
        AnsiConsole.Markup($"\n\nThe pairing is complete once the slave receives his confirmation message. When that happens, press any key to close the app.");
        Console.ReadKey(true);
        Console.Clear();
    }

    /// <summary>
    /// Do the linking process for the slave client. This will prompt the user to enter the pairing code given by the master, and then link the two clients together.
    /// </summary>
    /// <see cref="BeginLinking"/>
    private static async Task DoSlaveLinking()
    {
        string pairingCode = "";
        while (pairingCode.Length != 6 && pairingCode.All(char.IsDigit))
        {
            pairingCode = AnsiConsole.Ask<string>("(Slave) -  Enter the pairing code given by the master: ").Trim();
            Console.CursorTop--;
        }

        Guid uuid;
        try
        {
            Guid? _uuid = await FireSharpClient.TryPairingCodeAsync(pairingCode);
            if (_uuid == null)
            {
                AnsiConsole.Markup("[red]Pairing failed[/]. The pairing code is invalid. Press any key to exit.");
                Console.ReadKey(true);
                return;
            }
            else
            {
                uuid = (Guid)_uuid;
            }
        } catch (Exception)
        {
            AnsiConsole.Markup("[red]Pairing failed[/] due to internal server error. Press any key to exit.");
            Console.ReadKey(true);
            return;
        }

        SaveUUID(uuid);
        foreach (Note note in notes) await FireSharpClient.UploadNoteToCloud(uuid, note);
        await FireSharpClient.DeletePairingCode(pairingCode);
        AnsiConsole.Write(new Panel(new Markup("The two clients are now linked. Press any key to close the app.", Color.Green)).Expand().BorderColor(Color.Green));
        Console.ReadKey(true);
        Console.Clear();
    }

    /// <summary>
    /// Save the UUID to file
    /// </summary>
    /// <param name="uuid"></param>
    private static void SaveUUID(Guid uuid)
    {
        File.WriteAllText("uuid.txt", uuid.ToString());
    }

    /// <summary>
    /// Get the UUID from file if it exists
    /// </summary>
    /// <returns></returns>
    private static Guid? GetUUID()
    {
        if (!File.Exists("uuid.txt"))
        {
            return null;
        }

        string text = File.ReadAllText("uuid.txt");

        if (text.Length != 36)
        {
            return null;
        }

        try
        {
            return Guid.Parse(text);
        }
        catch (Exception)
        {
            return null;
        }
    }
}