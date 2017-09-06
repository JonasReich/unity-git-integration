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

		static Texture texture;
		static bool dirty = false;
		static string currentSelectionPath = "";


		static Git()
		{
			EditorApplication.projectWindowItemOnGUI += ProjectWindowItemOnGui;
			EditorApplication.projectWindowChanged += delegate
			{
				dirty = true;
			};
			texture = Resources.Load<Texture>("GitIcons/added");

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
			{
				dirty = true;
			}

			// -

			if (process != null && process.HasExited == false)
			{
				if (process.StartInfo.Arguments.Contains("status --porcelain"))
				{
					string line = "";

					while (process.StandardOutput.EndOfStream == false)
					{
						line = process.StandardOutput.ReadLine();
						File file = new File();

						file.status_string = line.Substring(0, 2);
						file.path += line.Substring(3);

						file.path = file.path.Replace("\"", "");
						file.path = file.path.Trim();

						{
							var substrings = file.path.Split('/');
							if (file.path.EndsWith("/"))
							{
								file.path = file.path.Remove(file.path.Length - 1);
								file.name = substrings[substrings.Length - 2] + "/";
							}
							else
								file.name = substrings[substrings.Length - 1];
						}

						if (file.path.Contains("Assets"))
						{
							file.isUnityFile = true;
						}
						if (file.path.EndsWith(".meta"))
						{
							file.isMetaFile = true;
						}

						file.path = "\"" + file.path + "\"";
						
						if (file.status_string.StartsWith("#"))
						{
							continue;
						}
						
						if (file.status_string[0] == '?')
						{
							file.status |= (uint)EStatus.Untracked;
						}
						else if (file.status_string[0] != ' ')
						{
							file.status |= (uint)EStatus.HasStagedChanges;
						}

						if (file.status_string[0] == 'R')
						{
							file.status |= (uint)EStatus.Renamed;
						}
						else if (file.status_string[0] == 'D')
						{
							file.status |= (uint)EStatus.Deleted;
						}

						if (line[1] != ' ')
						{
							file.status |= (uint)EStatus.HasUnstagedChanges;
						}

						files.Add(file);
					}
				}
				else
				{

					string newLine = process.StandardOutput.ReadToEnd();
					if (newLine != "")
					{
						Debug.Log(newLine);
						output += newLine;
					}
					while (process.StandardError.EndOfStream == false)
					{
						string newError = process.StandardError.ReadLine();
						if (newError != "")
						{
							if (newError.Contains("warning:") && newError.Contains("will be replaced by"))
							{
								newError += "\n" + process.StandardError.ReadLine();
								Debug.LogWarning(newError);
							}
							else
							{
								Debug.LogError(newError);
							}
							output += newError;
						}
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
			Command("status --porcelain", false);
			files.Clear();
		}


		public static void Add(File file)
		{
			if (file.isMetaFile)
			{
				Command("add " + file.path + " " + file.path.Replace(".meta", ""));
			}
			else if(file.isUnityFile)
			{
				Command("add " + file.path + " \"" + file.path.Replace("\"", "") + ".meta\"");
			}
			else
			{
				Command("add " + file.path);
			}
		}


		static bool UpdateCurrentSelectionPath()
		{
			var path = "";
			var obj = Selection.activeObject;
			if (obj == null) path = "Assets";
			else path = AssetDatabase.GetAssetPath(obj.GetInstanceID());
			if (currentSelectionPath == path)
			{
				return false;
			}
			else
			{
				currentSelectionPath = path;
				return true;
			}
		}

		static void ProjectWindowItemOnGui(string guid, Rect selectionRect)
		{
			var path = AssetDatabase.GUIDToAssetPath(guid);
			//if (!AssetDatabase.IsValidFolder(path)) return;

			//selectionRect.x = selectionRect.width;
			selectionRect.height = selectionRect.width = 16;

			foreach (var file in files)
			{
				if (path.Contains(file.path.Replace("\"", "")))
				{
					GUIStyle style = new GUIStyle("Box");
					style.fontSize = 8;
					style.fontStyle = FontStyle.Bold;
					GUI.Box(selectionRect, file.status_string, style);
					//GUI.DrawTexture(selectionRect, texture);
				}
			}
		}


		public class File
		{
			public string name;
			public string path;
			public string status_string;
			public uint status = (uint)EStatus.None;
			public bool isUnityFile;
			public bool isMetaFile;

			public bool HasStatus(EStatus status)
			{
				return (this.status & (uint)status) == (uint)status;
			}
		}

		[Flags]
		public enum EStatus : uint
		{
			None = 0,
			Untracked = 1 << 0,

			Unmodified = 1 << 1,
			Modified = 1 << 2,

			HasStagedChanges = 1 << 3,
			HasUnstagedChanges = 1 << 4,

			Deleted = 1 << 5,
			Renamed = 1 << 6,
			Copied = 1 << 7,

			Unmerged = 1 << 8
		}
	}
}
