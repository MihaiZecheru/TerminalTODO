using FireSharp.Config;
using FireSharp.EventStreaming;
using FireSharp.Interfaces;
using FireSharp.Response;
using System.Reflection.Metadata.Ecma335;
using System.Text.RegularExpressions;

namespace TerminalTODO;

internal static class FireSharpClient
{
    private static FirebaseConfig config = new FirebaseConfig
    {
        AuthSecret = "y6PkSlfo6IEPlEbdMEZJGAUSC41sMVzxVQJTdyc9",
        BasePath = "https://terminaltodo-8fffa-default-rtdb.firebaseio.com/"
    };

    private static IFirebaseClient client = new FireSharp.FirebaseClient(config) ?? throw new Exception("Error creating firebase client");

    /// <summary>
    /// Post the pairing code to the database and associate it with a uuid
    /// </summary>
    /// <exception cref="Exception">If the status code is not OK</exception>
    public static async Task PostPairingCodeAsync(string pairing_code, string uuid)
    {
        FirebaseResponse response = await client.SetAsync($"pairing-code/{pairing_code}", uuid);
        
        if (response.StatusCode != System.Net.HttpStatusCode.OK)
        {
            throw new Exception("Status code not OK");
        }
    }

    /// <summary>
    /// See if a pairing code exists. If it does, return the uuid associated with it
    /// </summary>
    /// <returns>The UUID associated with the pairing code if it exists</returns>
    /// <exception cref="Exception">If the status code is not OK</exception>
    /// <exception cref="Exception">If there was an error parsing the UUID received from the server</exception>
    public static async Task<Guid?> TryPairingCodeAsync(string pairing_code)
    {
        FirebaseResponse response = await client.GetAsync($"pairing-code/{pairing_code}");

        if (response.StatusCode != System.Net.HttpStatusCode.OK)
        {
            throw new Exception("Status code not OK");
        }    

        string uuid = response.Body;
        if (uuid == "null")
        {
            return null;
        }

        uuid = uuid.Substring(1, uuid.Length - 2);
        try
        {
            return Guid.Parse(uuid);
        }
        catch (FormatException)
        {
            throw new Exception("UUID is not in the correct format. Error parsing received UUID");
        }
    }

    public static async Task<List<Note>> GetCloudNotes(Guid uuid)
    {
        FirebaseResponse response = await client.GetAsync("notes/" + uuid.ToString());

        if (response.StatusCode != System.Net.HttpStatusCode.OK)
        {
            throw new Exception("Status code not OK");
        }

        string regex = @"\""-(.*?)\"":\""(.*?)\""";

        var notes = new List<Note>();
        foreach (Match match in Regex.Matches(response.Body, regex))
        {
            notes.Add(new Note(match.Groups[2].Value.Replace("\\n", "\n"), match.Groups[1].Value));
        }
        return notes;
    }

    public static async Task UploadNoteToCloud(Guid uuid, Note note)
    {
        Program.noteJustAdded = true;
        FirebaseResponse response = await client.PushAsync($"notes/{uuid.ToString()}", note.ToString());
        if (response.StatusCode != System.Net.HttpStatusCode.OK)
        {
            throw new Exception("Status code not OK");
        }
    }

    public static async Task DeletePairingCode(string pairing_code)
    {
        FirebaseResponse response = await client.DeleteAsync($"pairing-code/{pairing_code}");
        if (response.StatusCode != System.Net.HttpStatusCode.OK)
        {
            throw new Exception("Status code not OK");
        }
    }

    public static async Task DeleteNoteInCloud(Guid uuid, Note note)
    {
        FirebaseResponse response = await client.DeleteAsync($"notes/{uuid.ToString()}/-{note.databaseID}");
        if (response.StatusCode != System.Net.HttpStatusCode.OK)
        {
            throw new Exception("Status code not OK");
        }
    }

    public delegate void OnNewNoteCreatedInDB(string databaseID, string data);
    public delegate void OnNoteDelectedInDB(string databaseID);

    /// <summary>
    /// Start the note post/delete event listener
    /// </summary>
    /// <param name="uuid">The client UUID. Will only listen to updates/deletes for this client</param>
    /// <param name="onNewNoteCreatedInDB">Logic to perform when a note is added</param>
    /// <param name="onNoteDeletedInDB">Logic to perform when a note is deleted</param>
    public static async void StartUpdateEventListener(Guid uuid, OnNewNoteCreatedInDB onNewNoteCreatedInDB, OnNoteDelectedInDB onNoteDeletedInDB)
    {
        EventStreamResponse response = await client.OnAsync(
            path: $"notes/{uuid.ToString()}",
            added: (sender, args, context) =>
            {
                onNewNoteCreatedInDB(args.Path.Substring(2), args.Data);
            },
            changed: null,
            removed: (sender, args, context) =>
            {
                onNoteDeletedInDB(args.Path.Substring(2));
            }
        );
    }
}
