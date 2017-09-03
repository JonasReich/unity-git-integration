//-------------------------------------------
// (c) 2017 - Jonas Reich
//-------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using Debug = UnityEngine.Debug;

namespace GitIntegration
{
	/// <summary>
	/// 
	/// </summary>
	public class GitEditorWindow : EditorWindow
	{
		static GitEditorWindow Window;
		static Texture texture;

		[MenuItem("Window/Git")]
		static void OpenWindow()
		{
			Window = GetWindow<GitEditorWindow>("Git");
			EditorApplication.projectWindowItemOnGUI -= ProjectWindowItemOnGui;
			EditorApplication.projectWindowItemOnGUI += ProjectWindowItemOnGui;
			texture = Resources.Load<Texture>("GitIcons/added");
		}

		static void ProjectWindowItemOnGui(string guid, Rect selectionRect)
		{
			var path = AssetDatabase.GUIDToAssetPath(guid);
			//if (!AssetDatabase.IsValidFolder(path)) return;

			selectionRect.height = selectionRect.width = 16;

			foreach (var windowGitFile in Window.gitFiles)
			{
				if(path.Contains(windowGitFile.path.Replace("\"","")))
					GUI.DrawTexture(selectionRect, texture);
			}
		}
		

		string gitOutput = "";
		string commitMessage = "";
		Process gitProcess;
		Vector2 outputScrollPosition, stageScrollPosition, unstageScrollPosition;
		List<GitFile> gitFiles = new List<GitFile>();

		void OnGUI()
		{
			if (GUILayout.Button("status") && IsGitReady())
			{
				RefreshStatus();
			}

			if (GUILayout.Button("status log") && IsGitReady())
			{
				GitCommand("status");
			}

			if (gitProcess != null && gitProcess.HasExited == false)
			{
				if (gitProcess.StartInfo.Arguments.Contains("status -s"))
				{
					string line = "";

					while (gitProcess.StandardOutput.EndOfStream == false)
					{
						line = gitProcess.StandardOutput.ReadLine();
						GitFile file = new GitFile();
						
						file.status_string = line.Substring(0, 2);
						file.path += line.Substring(3);

						file.path = file.path.Replace("\"", "");
						file.path = file.path.Trim();

						var substrings = file.path.Split('/');
						if (file.path.EndsWith("/"))
							file.name = substrings[substrings.Length - 2] + "/";
						else
							file.name = substrings[substrings.Length - 1];

						file.path = "\"" + file.path + "\"";

						if (file.status_string.StartsWith("#"))
						{
							continue;
						}

						if (file.status_string.Contains("?"))
						{
							file.status |= (uint)EGitStatus.Untracked;
						}
						if (file.status_string.Contains("A"))
						{
							file.status |= (uint)EGitStatus.HasStagedChanges;
						}
						if (line[1] != ' ')
						{
							file.status |= (uint) EGitStatus.HasUnstagedChanges;
						}

						gitFiles.Add(file);
					}
				}
				else
				{

					string newLine = gitProcess.StandardOutput.ReadToEnd();
					if (newLine != "")
					{
						Debug.Log(newLine);
						gitOutput += newLine;
					}
					string newError = gitProcess.StandardError.ReadToEnd();
					if (newError != "")
					{
						gitOutput += newError;
						Debug.LogError(newError);
					}
				}
			}

			EditorGUILayout.Space();

			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("Staged Changes");
			if (GUILayout.Button(new GUIContent("-", "Unstage all files"), GUILayout.Width(20)) && IsGitReady())
			{
				GitCommand("reset .");
				repaint_asap = true;
			}
			EditorGUILayout.EndHorizontal();
			stageScrollPosition = GUILayout.BeginScrollView(stageScrollPosition, "TextArea", GUILayout.Height(150));
			foreach (var gitFile in gitFiles)
			{
				if (gitFile.HasStatus(EGitStatus.HasStagedChanges))
				{
					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.LabelField(gitFile.status_string, GUILayout.Width(30));
					GUILayout.Label(gitFile.name, GUILayout.Width(300));
					if (GUILayout.Button("-", GUILayout.Width(20)))
					{
						GitCommand("reset " + gitFile.path);
						EditorGUILayout.EndHorizontal();
						repaint_asap = true;
						break;
					}
					EditorGUILayout.EndHorizontal();
				}
			}
			GUILayout.EndScrollView();

			EditorGUILayout.Space();

			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("Changes");
			if (GUILayout.Button(new GUIContent("+", "Stage all changed files"), GUILayout.Width(20)) && IsGitReady())
			{
				GitCommand("add .");
				repaint_asap = true;
			}
			EditorGUILayout.EndHorizontal();
			unstageScrollPosition = GUILayout.BeginScrollView(unstageScrollPosition, "TextArea", GUILayout.Height(150));
			foreach (var gitFile in gitFiles)
			{
				if (gitFile.HasStatus(EGitStatus.HasUnstagedChanges))
				{
					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.LabelField(gitFile.status_string, GUILayout.Width(30));
					GUILayout.Label(gitFile.name, GUILayout.Width(300));
					if (GUILayout.Button("+", GUILayout.Width(20)))
					{
						GitCommand("add " + gitFile.path);
						EditorGUILayout.EndHorizontal();
						repaint_asap = true;
						break;
					}
					EditorGUILayout.EndHorizontal();
				}
			}
			GUILayout.EndScrollView();

			EditorGUILayout.Space();
			
			commitMessage = EditorGUILayout.TextArea(commitMessage);
			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button("Commit"))
			{
				if (commitMessage != "")
				{
					GitCommand("commit -m \"" + commitMessage + "\"");
					commitMessage = "";
					repaint_asap = true;
				}
				else
				{
					Debug.LogError("Please provide a message before commiting");
				}
			}
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.Space();

			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("Log:");
			if (GUILayout.Button("Clear", GUILayout.Width(80)))
			{
				gitOutput = "";
			}
			if (gitOutput.Length > 15000)
			{
				gitOutput = gitOutput.Substring(gitOutput.Length - 15000);
			}

			EditorGUILayout.EndHorizontal();
			outputScrollPosition = GUILayout.BeginScrollView(outputScrollPosition, GUILayout.Height(150));
			GUILayout.TextArea(gitOutput, GUILayout.ExpandHeight(true));
			GUILayout.EndScrollView();
		}

		private void RefreshStatus()
		{
			GitCommand("status -s");
			gitFiles.Clear();
			Repaint();
		}

		bool repaint_asap = false;

		void GitCommand(string arguments)
		{
			gitProcess = new Process();
			gitProcess.StartInfo.FileName = "git";
			gitProcess.StartInfo.Arguments = arguments;
			gitProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
			gitProcess.StartInfo.RedirectStandardError = true;

			gitProcess.StartInfo.RedirectStandardOutput = true;
			gitProcess.StartInfo.UseShellExecute = false;

			gitProcess.Start();
		}

		bool IsGitReady()
		{
			return (gitProcess != null && gitProcess.HasExited == true) || gitProcess == null;
		}

		void Update()
		{
			if (repaint_asap && IsGitReady())
			{
				RefreshStatus();
				repaint_asap = false;
				Repaint();
			}
		}
	}

	class GitFile
	{
		public string name;
		public string path;
		public string status_string;
		public uint status = (uint)EGitStatus.None;

		public bool HasStatus(EGitStatus status)
		{
			return (this.status & (uint) status) == (uint)status;
		}
	}

	[Flags]
	enum EGitStatus : uint
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
