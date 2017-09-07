//-------------------------------------------
// (c) 2017 - Jonas Reich
//-------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace GitIntegration
{
	/// <summary>
	/// 
	/// </summary>
	[InitializeOnLoad]
	public static class Git
	{
		public static string output = "";
		public static Process process;
		public static List<File> files = new List<File>();

		static Texture addedTexture, ignoredTexture, modifiedTexture, modifiedAddedTexture, unresolvedTexture, untrackedTexture;
		static bool dirty = false;
		static string currentSelectionPath = "";


		static Git()
		{
			EditorApplication.projectWindowItemOnGUI += ProjectWindowItemOnGui;
			EditorApplication.projectWindowChanged += delegate { dirty = true; };
			
			addedTexture = Resources.Load<Texture>("GitIcons/added");
			ignoredTexture = Resources.Load<Texture>("GitIcons/ignored");
			modifiedTexture = Resources.Load<Texture>("GitIcons/modified");
			modifiedAddedTexture = Resources.Load<Texture>("GitIcons/modifiedAdded");
			unresolvedTexture = Resources.Load<Texture>("GitIcons/unresolved");
			untrackedTexture = Resources.Load<Texture>("GitIcons/untracked");

			RefreshStatus();
		}


		public static void Update()
		{
			if (dirty && IsReady())
			{
				dirty = false;
				RefreshStatus();
				if (GitEditorWindow.Window)
					GitEditorWindow.Window.Repaint();
			}

			if (UpdateCurrentSelectionPath())
				dirty = true;

			if (process != null && process.HasExited == false)
				ReadGitOutput();
		}

		static void ProjectWindowItemOnGui(string guid, Rect selectionRect)
		{
			var path = AssetDatabase.GUIDToAssetPath(guid);

			selectionRect.height = selectionRect.width = 16;
			foreach (var file in files)
			{
				bool isMatchingFile = path.Contains(file.path.Replace("\"", ""));
				bool isMatchingFolderMetaFile = file.isMetaFile && file.isFolder && path.Contains(file.path.Replace(".meta\"", ""));
				if (isMatchingFile || isMatchingFolderMetaFile)
				{
					if (file.HasStatus(EStatus.Unresolved))
					{
						GUI.DrawTexture(selectionRect, unresolvedTexture);
					}
					else if (file.HasStatus(EStatus.Untracked))
					{
						GUI.DrawTexture(selectionRect, untrackedTexture);
					}
					else if (file.HasStatus(EStatus.HasStagedChanges))
					{
						if (file.HasStatus(EStatus.HasUnstagedChanges))
						{
							GUI.DrawTexture(selectionRect, modifiedAddedTexture);
						}
						else
						{
							GUI.DrawTexture(selectionRect, addedTexture);
						}
					}
					else if (file.HasStatus(EStatus.HasUnstagedChanges))
					{
						GUI.DrawTexture(selectionRect, modifiedTexture);
					}
					else if (file.HasStatus(EStatus.Ignored))
					{
						GUI.DrawTexture(selectionRect, ignoredTexture);
					}
				}
			}
		}

		
		public static bool IsReady()
		{
			return (process != null && process.HasExited == true) || process == null;
		}

		public static void Command(string arguments, bool setDirty = true)
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

		public static void RefreshStatus()
		{
			Command("status --porcelain --ignored", false);
			files.Clear();
		}


		public static void Add(File file)
		{
			if (file.isMetaFile)
				Command("add " + file.path + " " + file.path.Replace(".meta", ""));
			else if (file.isUnityFile)
				Command("add " + file.path + " \"" + file.path.Replace("\"", "") + ".meta\"");
			else
				Command("add " + file.path);
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

		/// <summary>
		/// Update the currentSelectionPath member 
		/// </summary>
		/// <returns>
		/// Has currentSelectionPath changed?
		/// </returns>
		static bool UpdateCurrentSelectionPath()
		{
			string path = (Selection.activeObject == null) ? "Assets" : AssetDatabase.GetAssetPath(Selection.activeObject.GetInstanceID());

			if (currentSelectionPath == path)
				return false;

			currentSelectionPath = path;
			return true;
		}


		public class File
		{
			public string name;
			public string path;
			public string status_string;
			public uint status = (uint)EStatus.None;
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
					status |= (uint)EStatus.Renamed;
				else if (status_string[0] == 'D')
					status |= (uint)EStatus.Deleted;
				else if (status_string[0] != ' ')
					status |= (uint)EStatus.HasStagedChanges;
				
				if (status_string[1] != ' ' && status_string[1] != '!')
					status |= (uint)EStatus.HasUnstagedChanges;
			}
		}

		[Flags]
		public enum EStatus : uint
		{
			None = 0,
			Untracked = 1 << 0,

			HasStagedChanges = 1 << 1,
			HasUnstagedChanges = 1 << 2,

			Deleted = 1 << 3,
			Renamed = 1 << 4,

			Unresolved = 1 << 5,

			Ignored = 1 << 6
		}
	}
}
