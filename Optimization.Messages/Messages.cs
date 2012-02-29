using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace Optimization.Messages
{
	public class Messages
	{
		private static int ReadMessageSize(Stream ms)
		{
			StringBuilder builder = new StringBuilder();

			while (true)
			{
				int b = ms.ReadByte();

				if (b == -1)
				{
					throw new IOException("End of stream");
				}

				if ((char)b == ' ')
				{
					return int.Parse(builder.ToString());
				}

				builder.Append((char)b);
			}
		}

		public static T ExtractOne<T>(Stream stream) where T : class
		{
			if (!stream.CanRead)
			{
				return null;
			}

			// First read the '<size> ' header
			int num;

			try
			{
				num = ReadMessageSize(stream);
			}
			catch (IOException)
			{
				return null;
			}

			byte[] msg = new byte[num];

			try
			{
				int start = 0;

				while (num > 0)
				{
					int len = stream.Read(msg, start, num);

					num -= len;
					start += len;
				}
			}
			catch (IOException)
			{
				return null;
			}

			MemoryStream ss = new MemoryStream(msg, 0, msg.Length);
			return ProtoBuf.Serializer.Deserialize<T>(ss);
		}

		public static T[] Extract<T>(Stream stream) where T : class
		{
			List<T> ret = new List<T>();

			if (!stream.CanRead)
			{
				return new T[] {};
			}

			while (true)
			{
				T one = ExtractOne<T>(stream);

				if (one != null)
				{
					ret.Add(one);
				}
				else
				{
					break;
				}
			}

			return ret.ToArray();
		}

		public static byte[] Create<T>(T message)
		{
			MemoryStream stream = new MemoryStream();

			try
			{
				ProtoBuf.Serializer.Serialize(stream, message);
			}
			catch
			{
				return null;
			}

			byte[] msg = stream.GetBuffer();
			byte[] header = Encoding.ASCII.GetBytes(((uint)msg.Length).ToString() + " ");

			byte[] ret = new byte[header.Length + msg.Length];
			Array.Copy(header, ret, header.Length);
			Array.Copy(msg, 0, ret, header.Length, msg.Length);

			return ret;
		}
	}
}
