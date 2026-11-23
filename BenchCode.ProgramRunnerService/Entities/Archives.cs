using System.Formats.Tar;

namespace BenchCode.ProgramRunnerService.Entities;

internal sealed record TarFile(string Path, byte[] Data, UnixFileMode Mode);

internal static class Archives
{
    private const UnixFileMode DirMode = (UnixFileMode)0b111_111_111; // 0777

    public static Stream Create(IReadOnlyList<TarFile> files)
    {
        var ms = new MemoryStream();
        using (var writer = new TarWriter(ms, TarEntryFormat.Pax, leaveOpen: true))
        {
            // Каталоги-родители обязательны: путь-назначение должен существовать.
            // Именно запись "workspace/" создаёт /workspace в rw-слое контейнера.
            foreach (var dir in files.Select(f => f.Path[..(f.Path.LastIndexOf('/') + 1)]).Distinct())
            {
                writer.WriteEntry(new PaxTarEntry(TarEntryType.Directory, dir)
                {
                    Mode = DirMode,
                    Uid = 65534,
                    Gid = 65534,
                });
            }

            foreach (var file in files)
            {
                writer.WriteEntry(new PaxTarEntry(TarEntryType.RegularFile, file.Path)
                {
                    DataStream = new MemoryStream(file.Data),
                    Mode = file.Mode,
                    Uid = 65534,
                    Gid = 65534,
                });
            }
        }

        ms.Position = 0;
        return ms;
    }

    public static async Task<byte[]> ReadSingleAsync(Stream tar, CancellationToken ct)
    {
        // Docker returns a chunked HTTP stream. Buffer the complete tar before parsing it,
        // so TarReader never owns or outlives the response stream from the daemon.
        await using var archive = new MemoryStream();
        await tar.CopyToAsync(archive, ct);
        archive.Position = 0;

        await using var reader = new TarReader(archive, leaveOpen: true);
        while (await reader.GetNextEntryAsync(copyData: false, cancellationToken: ct) is { } entry)
        {
            if (entry.EntryType is not (TarEntryType.RegularFile or TarEntryType.V7RegularFile))
                continue;

            using var contents = new MemoryStream();
            await (entry.DataStream ?? Stream.Null).CopyToAsync(contents, ct);
            return contents.ToArray();
        }

        throw new InvalidOperationException("Демон вернул архив без файла с артефактом");
    }
}