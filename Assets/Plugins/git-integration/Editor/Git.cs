//-------------------------------------------
// (c) 2017 - Jonas Reich
//-------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace GitIntegration
{
	/// <summary>
	/// Unity/C# friendly interface for the git client
	/// </summary>
	[InitializeOnLoad]
	public static class Git
	{
		public static string output = "";
		public static Process process;
		public static List<File> files = new List<File>();

		public static bool dirty = false;


		static Git()
		{
			RefreshStatus();
		}


		public static void Update()
		{
			if (process != null && process.HasExited == false)
				ReadGitOutput();
		}
		

		public static bool IsReady()
		{
			return (process != null && process.HasExited == true) || process == null;
		}
		
		public static void RefreshStatus()
		{
			CommandExplicit("status --porcelain --ignored", false);
			files.Clear();
		}


		public static void CommandExplicit(string arguments, bool setDirty = true)
		{
			process = new Process();
			process.StartInfo.FileName = "git";
			process.StartInfo.Arguments = arguments;
			process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
			process.StartInfo.CreateNoWindow = true;
			process.StartInfo.RedirectStandardError = true;

			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.UseShellExecute = false;

			process.Start();

			if (setDirty)
				dirty = true;
		}


		public static void Command(ECommand command)
		{
			CommandExplicit(CommandList[(int)command] + " .");
		}

		public static void Command(ECommand command, File file)
		{
			CommandExplicit(CommandList[(int)command] + " " + file.GetPaths());
		}

		public static void Command(ECommand command, ICollection<File> files)
		{
			string fileList = "";
			foreach (File file in files)
				fileList += file.GetPaths() + " ";
			CommandExplicit(CommandList[(int)command] + " " + fileList);
		}

		
		static void ReadGitOutput()
		{
			if (process.StartInfo.Arguments.Contains("status --porcelain --ignored"))
			{
				while (process.StandardOutput.EndOfStream == false)
				{
					var file = CreateFileFromStatusLine(process.StandardOutput.ReadLine());
					if (file != null)
						files.Add(file);
				}
			}
			else
			{
				// Read standard output at once
				string standardLogOutput = process.StandardOutput.ReadToEnd();
				if (standardLogOutput != "")
				{
					Debug.Log(standardLogOutput);
					output += standardLogOutput;
				}
				// Read error output line by line
				while (process.StandardError.EndOfStream == false)
				{
					string errorLine = process.StandardError.ReadLine();
					if (errorLine != "")
					{
						if (errorLine.Contains("warning:") && errorLine.Contains("will be replaced by"))
						{
							errorLine += "\n" + process.StandardError.ReadLine();
							Debug.LogWarning("git " + errorLine + "/n");
						}
						else
						{
							Debug.LogError("git " + errorLine + "/n");
						}
						output += errorLine;
					}
				}
			}
		}

		static File CreateFileFromStatusLine(string newLine)
		{
			File file = new File();

			file.status_string = newLine.Substring(0, 2);

			// Can't create file from comments
			if (file.status_string.StartsWith("#")) return null;


			file.path = newLine.Substring(3);

			{
				var substrings = file.path.Split(new string[] { "->" }, StringSplitOptions.None);
				file.path = substrings.Last();
			}

			// Remove quotation marks and whitespace
			file.path = file.path.Replace("\"", "");
			file.path = file.path.Trim();

			{
				var substrings = file.path.Split('/');
				if (file.path.EndsWith("/")) // is folder
				{
					file.path = file.path.Remove(file.path.Length - 1);
					file.name = substrings[substrings.Length - 2] + "/";
					file.isFolder = true;
				}
				else // is file
					file.name = substrings[substrings.Length - 1];
			}

			if (file.path.Contains("Assets"))
				file.isUnityFile = true;

			if (file.path.EndsWith(".meta"))
				file.isMetaFile = true;

			file.path = "\"" + file.path + "\"";

			file.UpdateStatus();
			return file;
		}


		public class File
		{
			public string name;
			public string path;
			public string status_string;
			public uint status = 0;
			public bool isUnityFile, isMetaFile, isFolder;

			public bool HasStatus(EStatus status)
			{
				return (this.status & (uint)status) == (uint)status;
			}

			/// <summary>
			/// Set status flag from status string
			/// </summary>
			public void UpdateStatus()
			{
				if (status_string.Contains("U") || status_string == "AA" || status_string == "DD")
					status |= (uint)EStatus.Unresolved;
				else if (status_string[0] == '!')
					status |= (uint)EStatus.Ignored;
				else if (status_string[0] == '?')
					status |= (uint)EStatus.Untracked;
				else if (status_string[0] == 'R')
				{
					status |= (uint)EStatus.HasStagedChanges;
					status |= (uint)EStatus.Renamed;
				}
				else if (status_string[0] == 'D')
					status |= (uint)EStatus.Deleted;
				else if (status_string[0] != ' ')
					status |= (uint)EStatus.HasStagedChanges;

				if (status_string[1] != ' ' && status_string[1] != '!')
					status |= (uint)EStatus.HasUnstagedChanges;
			}

			public string GetPaths()
			{
				if (isMetaFile)
					return path + " " + path.Replace(".meta", "");
				if (isUnityFile)
					return path + " \"" + path.Replace("\"", "") + ".meta\"";
				return path;
			}
		}

		[Flags]
		public enum EStatus : uint
		{
			Untracked = 1 << 0,

			HasStagedChanges = 1 << 1,
			HasUnstagedChanges = 1 << 2,

			Deleted = 1 << 3,
			Renamed = 1 << 4,

			Unresolved = 1 << 5,

			Ignored = 1 << 6
		}

		public enum ECommand
		{
			Add,
			Reset,
			Diff,
			Discard
		}

		static readonly string[] CommandList =
		{
			"add",
			"reset",
			"difftool --no-prompt HEAD",
			"checkout "
		};
	}
}
