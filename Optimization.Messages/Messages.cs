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
			
			while (ms.Position < ms.Length)
			{
				char b = (char)ms.ReadByte();
				
				if (b == ' ')
				{
					return int.Parse(builder.ToString());
				}
				
				builder.Append(b);
			}
			
			return 1;
		}

		public static T[] Extract<T>(Stream stream)
		{
			List<T> ret = new List<T>();

			while (stream.Position < stream.Length)
			{
				// First read the '<size> ' header
				int num = ReadMessageSize(stream);
				
				// Check if we received the full message
				if (num > (stream.Length - stream.Position))
				{
					break;
				}
				
				byte[] msg = new byte[num];
				stream.Read(msg, 0, num);

				MemoryStream ss = new MemoryStream(msg, 0, num);
				ret.Add(ProtoBuf.Serializer.Deserialize<T>(ss));
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
