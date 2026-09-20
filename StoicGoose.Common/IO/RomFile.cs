using System;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace StoicGoose.Common.IO
{
	public static class RomFile
	{
		public static readonly string[] Extensions = [".ws", ".wsc", ".zip"];

		public static bool IsSupported(string filename) =>
			!string.IsNullOrEmpty(filename) &&
			File.Exists(filename) &&
			Extensions.Contains(Path.GetExtension(filename), StringComparer.OrdinalIgnoreCase);

		public static byte[] Read(string filename)
		{
			using var stream = Open(filename);
			using var memory = new MemoryStream();
			stream.CopyTo(memory);
			return memory.ToArray();
		}

		public static Stream Open(string filename)
		{
			if (Path.GetExtension(filename).Equals(".zip", StringComparison.OrdinalIgnoreCase))
			{
				using var archive = new ZipArchive(new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite), ZipArchiveMode.Read);
				var entry = archive.Entries.FirstOrDefault(e =>
					!string.IsNullOrEmpty(e.Name) &&
					(e.Name.EndsWith(".ws", StringComparison.OrdinalIgnoreCase) || e.Name.EndsWith(".wsc", StringComparison.OrdinalIgnoreCase)))
					?? archive.Entries.FirstOrDefault(e => !string.IsNullOrEmpty(e.Name));

				if (entry == null)
					throw new InvalidDataException("The ZIP archive does not contain a ROM.");

				var memory = new MemoryStream();
				using (var entryStream = entry.Open())
					entryStream.CopyTo(memory);
				memory.Position = 0;
				return memory;
			}

			return new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
		}
	}
}
