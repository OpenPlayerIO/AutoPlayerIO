using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;

namespace CloneTool
{
	static class StreamExtensions
	{
		internal static IEnumerable<long> ScanAOB(this Stream stream, params byte[] aob)
		{
			long position;
			var buffer = new byte[aob.Length - 1];

			while ((position = stream.Position) < stream.Length)
			{
				if (stream.ReadByte() != aob[0]) continue;
				if (stream.Read(buffer, 0, aob.Length - 1) == 0) continue;

				if (buffer.SequenceEqual(aob.Skip(1)))
				{
					yield return position;
				}
			}
		}
	}
}