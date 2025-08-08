using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Unosys.SDK
{
	internal static class EncryptionDecryption
	{
		internal static byte[] Encrypt( byte[] dataToEncrypt, ref byte[] key, ref byte[] iv, bool use32ByteIV = true )
		{
			byte[] foggyBytes = null;
			using (RijndaelManaged provider = new RijndaelManaged())
			{
				provider.KeySize = 256;
				if (use32ByteIV)
					provider.BlockSize = 256;

				if (key == null)
				{
					provider.GenerateKey();
					key = provider.Key;
				}
				if (iv == null)
				{
					provider.GenerateIV();
					iv = provider.IV;
				}
				foggyBytes = Transform( dataToEncrypt, provider.CreateEncryptor( key, iv ) );
			}
			return foggyBytes;
		}

		internal static byte[] Decrypt( byte[] foggyBytes, byte[] key, byte[] iv, bool use32ByteIV = true )
		{
			byte[] clearBytes = null;
			using (RijndaelManaged provider = new RijndaelManaged())
			{
				provider.KeySize = 256;
				if (use32ByteIV)
					provider.BlockSize = 256;
				clearBytes = Transform( foggyBytes, provider.CreateDecryptor( key, iv ) );
			}
			return clearBytes;
		}

		private static byte[] Transform( byte[] textBytes, ICryptoTransform transform )
		{
			using (MemoryStream buf = new MemoryStream())
			{
				using (CryptoStream stream = new CryptoStream( buf, transform, CryptoStreamMode.Write ))
				{
					stream.Write( textBytes, 0, textBytes.Length );
					stream.FlushFinalBlock();
					return buf.ToArray();
				}
			}
		}

	}
}
