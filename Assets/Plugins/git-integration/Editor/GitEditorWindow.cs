//-------------------------------------------
// (c) 2017 - Jonas Reich
//-------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace GitIntegration
{
	/// <summary>
	/// Next to changelists and log output this editor window is responsible for sending Update events to Git and GitOverlay
	/// </summary>
	public class GitEditorWindow : EditorWindow
	{
		public static GitEditorWindow Window;

		[MenuItem("Window/Git")]
		static void OpenWindow()
		{
			Window = GetWindow<GitEditorWindow>("Git");
		}

		bool repaintAsap = false;
		string commitMessage = "";
		Vector2 outputScrollPosition, stageScrollPosition, unstageScrollPosition;


		void Update()
		{
			if (repaintAsap && Git.IsReady())
			{
				Git.RefreshStatus();
				repaintAsap = false;
				Repaint();
			}
		}

		void OnGUI()
		{
			EditorGUILayout.Space();
			StagingArea();
			EditorGUILayout.Space();
			ChangesArea();
			EditorGUILayout.Space();
			CommitMessageArea();
			EditorGUILayout.Space();
			LogArea();
		}


		void StagingArea()
		{
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("Staged Changes");
			if (GUILayout.Button(new GUIContent("-", "Unstage all files"), GUILayout.Width(20)) && Git.IsReady())
			{
				Git.Command(Git.ECommand.Reset);
				repaintAsap = true;
			}
			EditorGUILayout.EndHorizontal();
			stageScrollPosition = GUILayout.BeginScrollView(stageScrollPosition, "TextArea", GUILayout.Height(150));
			foreach (var gitFile in Git.files)
			{
				if (gitFile.HasStatus(Git.EStatus.HasStagedChanges))
				{
					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.LabelField(gitFile.status_string, GUILayout.Width(30));
					GUILayout.Label(gitFile.name, GUILayout.Width(300));
					if (GUILayout.Button("-", GUILayout.Width(20)))
					{
						Git.Command(Git.ECommand.Reset, gitFile);
						EditorGUILayout.EndHorizontal();
						repaintAsap = true;
						break;
					}
					if (GUILayout.Button("diff", GUILayout.Width(40)))
					{
						Git.Command(Git.ECommand.Diff, gitFile);
						EditorGUILayout.EndHorizontal();
					}
					EditorGUILayout.EndHorizontal();
				}
			}
			GUILayout.EndScrollView();
		}

		void ChangesArea()
		{
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("Changes");
			if (GUILayout.Button(new GUIContent("+", "Stage all changed files"), GUILayout.Width(20)) && Git.IsReady())
			{
				Git.Command(Git.ECommand.Add);
				repaintAsap = true;
			}
			EditorGUILayout.EndHorizontal();
			unstageScrollPosition = GUILayout.BeginScrollView(unstageScrollPosition, "TextArea", GUILayout.Height(150));
			foreach (var gitFile in Git.files)
			{
				if (gitFile.HasStatus(Git.EStatus.HasUnstagedChanges))
				{
					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.LabelField(gitFile.status_string, GUILayout.Width(30));
					GUILayout.Label(gitFile.name, GUILayout.Width(300));
					if (GUILayout.Button("+", GUILayout.Width(20)))
					{
						Git.Command(Git.ECommand.Add, gitFile);
						EditorGUILayout.EndHorizontal();
						repaintAsap = true;
						break;
					}
					if (GUILayout.Button("diff", GUILayout.Width(40)))
					{
						Git.Command(Git.ECommand.Diff, gitFile);
						EditorGUILayout.EndHorizontal();
					}
					if (GUILayout.Button("discard", GUILayout.Width(60)) && EditorUtility.DisplayDialog("Discard local changes?", "Are you sure you want to discard your local changes to " + gitFile.name + "?", "Discard", "Cancel"))
					{
						Git.Command(Git.ECommand.Discard, gitFile);
						EditorGUILayout.EndHorizontal();
						repaintAsap = true;
						break;
					}
					EditorGUILayout.EndHorizontal();
				}
			}
			GUILayout.EndScrollView();
		}

		void CommitMessageArea()
		{
			commitMessage = EditorGUILayout.TextArea(commitMessage, GUILayout.Height(75));
			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button("Commit"))
			{
				if (Git.files.Find(file => file.HasStatus(Git.EStatus.HasStagedChanges)) != null)
				{
					if (commitMessage != "")
					{
						Git.CommandExplicit("commit -m \"" + commitMessage + "\"");
						commitMessage = "";
						repaintAsap = true;
					}
					else
					{
						Debug.LogError("Please provide a message before commiting");
					}
				}
				else
				{
					Debug.LogWarning("Nothing to commit");
				}
			}
			EditorGUILayout.EndHorizontal();
		}

		void LogArea()
		{
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("Log:");
			if (GUILayout.Button("Clear", GUILayout.Width(80)))
			{
				Git.output = "";
			}
			if (Git.output.Length > 15000)
			{
				Git.output = Git.output.Substring(Git.output.Length - 15000);
			}

			EditorGUILayout.EndHorizontal();
			outputScrollPosition = GUILayout.BeginScrollView(outputScrollPosition);
			GUILayout.TextArea(Git.output, GUILayout.ExpandHeight(true));
			GUILayout.EndScrollView();
		}
	}
}
