using ICSharpCode.SharpZipLib.Checksums;
using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace CiWong.OpenAPI.ToolsAndPackage.Helper
{
	/// <summary>
	/// Zip压缩与解压缩 
	/// </summary>
	public class ZipHelper
	{
		/// <summary>
		/// 压缩单个文件
		/// </summary>
		/// <param name="fileToZip">要压缩的文件</param>
		/// <param name="zipedFile">压缩后的文件</param>
		/// <param name="compressionLevel">压缩等级</param>
		/// <param name="blockSize">每次写入大小</param>
		public static void ZipFile(string fileToZip, string zipedFile, int compressionLevel, int blockSize)
		{
			//如果文件没有找到，则报错
			if (!System.IO.File.Exists(fileToZip))
			{
				throw new System.IO.FileNotFoundException("指定要压缩的文件: " + fileToZip + " 不存在!");
			}

			using (System.IO.FileStream zipFile = System.IO.File.Create(zipedFile))
			{
				using (ZipOutputStream zipStream = new ZipOutputStream(zipFile))
				{
					using (System.IO.FileStream streamToZip = new System.IO.FileStream(fileToZip, System.IO.FileMode.Open, System.IO.FileAccess.Read))
					{
						string fileName = fileToZip.Substring(fileToZip.LastIndexOf("\\") + 1);

						ZipEntry zipEntry = new ZipEntry(fileName);

						zipStream.PutNextEntry(zipEntry);

						zipStream.SetLevel(compressionLevel);

						byte[] buffer = new byte[blockSize];

						int sizeRead = 0;

						try
						{
							do
							{
								sizeRead = streamToZip.Read(buffer, 0, buffer.Length);
								zipStream.Write(buffer, 0, sizeRead);
							}
							while (sizeRead > 0);
						}
						catch (System.Exception ex)
						{
							throw ex;
						}

						streamToZip.Close();
					}

					zipStream.Finish();
					zipStream.Close();
				}

				zipFile.Close();
			}
		}

		/// <summary>
		/// 压缩单个文件
		/// </summary>
		/// <param name="fileToZip">要进行压缩的文件名</param>
		/// <param name="zipedFile">压缩后生成的压缩文件名</param>
		public static void ZipFile(string fileToZip, string zipedFile)
		{
			//如果文件没有找到，则报错
			if (!File.Exists(fileToZip))
			{
				throw new System.IO.FileNotFoundException("指定要压缩的文件: " + fileToZip + " 不存在!");
			}

			using (FileStream fs = File.OpenRead(fileToZip))
			{
				byte[] buffer = new byte[fs.Length];
				fs.Read(buffer, 0, buffer.Length);
				fs.Close();

				using (FileStream zipFile = File.Create(zipedFile))
				{
					using (ZipOutputStream zipStream = new ZipOutputStream(zipFile))
					{
						string fileName = fileToZip.Substring(fileToZip.LastIndexOf("\\") + 1);
						ZipEntry zipEntry = new ZipEntry(fileName);
						zipStream.PutNextEntry(zipEntry);
						zipStream.SetLevel(5);

						zipStream.Write(buffer, 0, buffer.Length);
						zipStream.Finish();
						zipStream.Close();
					}
				}
			}
		}

		/// <summary>
		/// 压缩多层目录
		/// </summary>
		/// <param name="strDirectory">The directory.</param>
		/// <param name="zipedFile">The ziped file.</param>
		public static void ZipFileDirectory(string strDirectory, string zipedFile)
		{
			using (System.IO.FileStream zipFile = System.IO.File.Create(zipedFile))
			{
				using (ZipOutputStream s = new ZipOutputStream(zipFile))
				{
					string zipedFileName = Path.GetFileName(zipedFile);
					ZipSetp(strDirectory, s, "", zipedFileName);
					s.Flush();
				}
			}
		}

		/// <summary>
		/// 递归遍历目录
		/// </summary>
		/// <param name="strDirectory">The directory.</param>
		/// <param name="s">The ZipOutputStream Object.</param>
		/// <param name="parentPath">The parent path.</param>
		private static void ZipSetp(string strDirectory, ZipOutputStream s, string parentPath, string zipedFileName)
		{
			if (strDirectory[strDirectory.Length - 1] != Path.DirectorySeparatorChar)
			{
				strDirectory += Path.AltDirectorySeparatorChar;
			}
			Crc32 crc = new Crc32();//循环冗余校验码
			string[] filenames = Directory.GetFileSystemEntries(strDirectory).Where(p => zipedFileName == "" || !p.Contains(zipedFileName)).ToArray();
			foreach (string file in filenames)// 遍历所有的文件和目录
			{
				if (Directory.Exists(file))// 先当作目录处理如果存在这个目录就递归Copy该目录下面的文件
				{
					string pPath = parentPath;
					pPath += file.Substring(file.LastIndexOf("\\") + 1);
					pPath += "\\";
					ZipSetp(file, s, pPath, "");
				}
				else // 否则直接压缩文件
				{
					//打开压缩文件
					using (FileStream fs = File.OpenRead(file))
					{
						byte[] buffer = new byte[fs.Length];
						fs.Read(buffer, 0, buffer.Length);

						string fileName = file.Substring(file.LastIndexOf("\\") + 1);
						ZipEntry entry = new ZipEntry(fileName);

						entry.DateTime = DateTime.Now;
						entry.Size = fs.Length;

						fs.Close();

						crc.Reset();
						crc.Update(buffer);

						entry.Crc = crc.Value;
						s.PutNextEntry(entry);

						s.Write(buffer, 0, buffer.Length);
					}
				}
			}
		}

		/// <summary>
		/// 解压缩一个 zip 文件。
		/// </summary>
		/// <param name="zipedFile">The ziped file.</param>
		/// <param name="strDirectory">The STR directory.</param>
		/// <param name="password">zip 文件的密码。</param>
		/// <param name="overWrite">是否覆盖已存在的文件。</param>
		public static void UnZip(string zipedFile, string strDirectory, string password, bool overWrite)
		{

			if (strDirectory == "")
				strDirectory = Directory.GetCurrentDirectory();
			if (!strDirectory.EndsWith("\\"))
				strDirectory = strDirectory + "\\";

			using (ZipInputStream s = new ZipInputStream(File.OpenRead(zipedFile)))
			{
				s.Password = password;
				ZipEntry theEntry;

				while ((theEntry = s.GetNextEntry()) != null)
				{
					string directoryName = "";
					string pathToZip = "";
					pathToZip = theEntry.Name;

					if (pathToZip != "")
						directoryName = Path.GetDirectoryName(pathToZip) + "\\";

					string fileName = Path.GetFileName(pathToZip);

					Directory.CreateDirectory(strDirectory + directoryName);

					if (fileName != "")
					{
						if ((File.Exists(strDirectory + directoryName + fileName) && overWrite) || (!File.Exists(strDirectory + directoryName + fileName)))
						{
							using (FileStream streamWriter = File.Create(strDirectory + directoryName + fileName))
							{
								int size = 2048;
								byte[] data = new byte[2048];
								while (true)
								{
									size = s.Read(data, 0, data.Length);

									if (size > 0)
										streamWriter.Write(data, 0, size);
									else
										break;
								}
								streamWriter.Close();
							}
						}
					}
				}

				s.Close();
			}
		}
	}
}