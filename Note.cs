using Spectre.Console;

namespace TerminalTODO;

internal class Note
{
    private static readonly string notes_filename = "notes.txt";

    private string Content { get; set; }
    public string? databaseID { get; set; } = null; // This is the ID of the note in the database, if it exists

    public int Lines => Content.Split('\n').Length;

    public Note(string content)
    {
        Content = content;
    }

    public Note(string content, string databaseID)
    {
        Content = content;
        this.databaseID = databaseID;
    }

    public void Save()
    {
        if (Program.UUID == null)
        {
            string txt = File.ReadAllText(notes_filename);
            // The pipe is used to separate notes as you can't type the pipe in the footer, it's not allowed
            File.WriteAllText(notes_filename, txt + this.ToString() + "|");
        }
        else
        {
            // Save to the cloud
            Task.Run(() => FireSharpClient.UploadNoteToCloud((Guid)Program.UUID, this));
        }
    }

    public void Delete()
    {
        if (Program.UUID == null)
        {
            File.WriteAllText(notes_filename, string.Join('|', Note.GetAllNotes().Where(n => n.ToString() != this.ToString())) + "|");
        }
        else
        {
            // Delete from the cloud
            Task.Run(() => FireSharpClient.DeleteNoteInCloud((Guid)Program.UUID, this));
        }
    }

    public void Render()
    {
        AnsiConsole.Write(
            new Panel(new Markup(Content, Color.Grey70))
            .Expand()
            .BorderColor(Color.Blue)
            .AsciiBorder()
        );
    }

    public override string ToString()
    {
        return Content;
    }

    public static List<Note> GetAllNotes()
    {
        var notes = new List<Note>();
        File.ReadAllText(notes_filename).Split('|').ToList().ForEach(txt =>
        {
            if (txt.Length > 0) notes.Add(new Note(txt));
        });
        return notes;
    }
}
