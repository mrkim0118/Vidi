namespace Vidi;

// Recently opened files, most recent first, each with the position to resume from (0 = from the start).
public sealed class ResumeStore
{
    private const long EdgeMilliseconds = 10_000;
    private readonly string _filePath;
    private readonly int _capacity;
    private readonly List<(string Path, long Time)> _entries = [];

    public ResumeStore(string filePath, int capacity = 200)
    {
        _filePath = filePath;
        _capacity = capacity;
        try
        {
            foreach (var line in File.Exists(filePath) ? File.ReadAllLines(filePath) : [])
            {
                var tab = line.IndexOf('\t');
                if (tab > 0 && long.TryParse(line[..tab], out var time) && line.Length > tab + 1)
                {
                    _entries.Add((line[(tab + 1)..], Math.Max(time, 0)));
                }
            }
        }
        catch
        {
            // A missing or unreadable file only means no history.
        }
    }

    public long GetTime(string path) =>
        _entries.FirstOrDefault(e => e.Path.Equals(path, StringComparison.OrdinalIgnoreCase)).Time;

    public IReadOnlyList<string> RecentFiles(int count) =>
        _entries.Select(e => e.Path).Take(count).ToList();

    // Moves the file to the front. Positions near the start or end resume from the beginning; length 0 = unknown.
    public void Record(string path, long time, long length)
    {
        if (time < EdgeMilliseconds || (length > 0 && time > length - EdgeMilliseconds))
        {
            time = 0;
        }

        _entries.RemoveAll(e => e.Path.Equals(path, StringComparison.OrdinalIgnoreCase));
        _entries.Insert(0, (path, time));
        if (_entries.Count > _capacity)
        {
            _entries.RemoveRange(_capacity, _entries.Count - _capacity);
        }
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
            File.WriteAllLines(_filePath, _entries.Select(e => $"{e.Time}\t{e.Path}"));
        }
        catch
        {
            // History is a convenience; never fail playback over it.
        }
    }
}
