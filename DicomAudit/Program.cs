// See https://aka.ms/new-console-template for more information

using FellowOakDicom;
using LibArchive.Net;

long objectCount = 0;

Parallel.ForEach(Directory.EnumerateFiles(".", "*", SearchOption.AllDirectories), ProcessFile);
return;

bool ProcessStream(Stream s, string path)
{
    Span<byte> preamble = stackalloc byte[4];
    s.Seek(128, SeekOrigin.Begin);
    if (s.Read(preamble) != 4 || "DICM"u8 != preamble) return false;

    s.Seek(0, SeekOrigin.Begin);
    var ds = DicomFile.Open(s).Dataset;
    Console.WriteLine($"{ds.GetString(DicomTag.Modality)},{ds.GetString(DicomTag.StudyInstanceUID)},{ds.GetString(DicomTag.SeriesInstanceUID)},{ds.GetString(DicomTag.SOPInstanceUID)},{path}");
    var counter = Interlocked.Increment(ref objectCount);
    if (counter % 1024 == 0) Console.Error.Write($"{counter}\r");
    return true;
}

void ProcessArchive(string path)
{
    using var arc = new LibArchiveReader(path);
    foreach (var entry in arc.Entries())
    {
        using var s = entry.Stream;
        ProcessStream(s, $"{path}!{entry.Name}");
    }
}

void ProcessFile(string path)
{
    var s = File.OpenRead(path);
    if (s.Length < 132 || ProcessStream(s, path)) return;

    ProcessArchive(path);
}