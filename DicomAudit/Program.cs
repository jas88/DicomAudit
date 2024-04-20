// See https://aka.ms/new-console-template for more information

using FellowOakDicom;
using LibArchive.Net;

long objectCount = 0;

Parallel.ForEach(new LineReader.LineReader(Console.OpenStandardInput(), '\0').ReadLines(), ProcessFile);
return;

bool ProcessStream(Stream s, string path)
{
    try
    {
        Span<byte> preamble = stackalloc byte[4];
        s.Seek(128, SeekOrigin.Begin);
        if (s.Read(preamble) != 4 || "DICM"u8.SequenceCompareTo(preamble) != 0) return false;

        s.Seek(0, SeekOrigin.Begin);
        var ds = DicomFile.Open(s).Dataset;
        Console.WriteLine($"{ds.GetString(DicomTag.Modality)},{ds.GetString(DicomTag.StudyInstanceUID)},{ds.GetString(DicomTag.SeriesInstanceUID)},{ds.GetString(DicomTag.SOPInstanceUID)},{path}");
        var counter = Interlocked.Increment(ref objectCount);
        if (counter % 1024 == 0) Console.Error.Write($"{counter}\r");
        return true;
    }
    catch (Exception e)
    {
        Console.Error.WriteLine($"{path}:{e.Message}");
        return false;
    }
}

void ProcessArchive(string path)
{
    using var arc = new LibArchiveReader(path);
    foreach (var entry in arc.Entries())
    {
        using var s = entry.Stream;
        using var ms = new MemoryStream();
        s.CopyTo(ms, 1 << 20);
        ms.Seek(0, SeekOrigin.Begin);
        ProcessStream(ms, $"{path}!{entry.Name}");
    }
}

void ProcessFile(string path)
{
    try
    {
        var s = File.OpenRead(path);
        if (s.Length < 132 || ProcessStream(s, path)) return;

        ProcessArchive(path);
    }
    catch (Exception e)
    {
        if (string.Compare(e.Message, "Missing type keyword in mtree specification", StringComparison.Ordinal) != 0 &&
            string.Compare(e.Message, "Unrecognized archive format", StringComparison.Ordinal) != 0)
            Console.Error.WriteLine($"{path}:{e.Message}");
    }
}