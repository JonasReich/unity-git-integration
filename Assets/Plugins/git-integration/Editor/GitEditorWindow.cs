//-------------------------------------------
// (c) 2017 - Jonas Reich
//-------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
		public static GitEditorWindow Window;
		
		[MenuItem("Window/Git")]
		static void OpenWindow()
		{
			Window = GetWindow<GitEditorWindow>("Git");
		}
		
		bool repaintAsap = false;
		string commitMessage = "";
		Vector2 outputScrollPosition, stageScrollPosition, unstageScrollPosition;
		
		void OnGUI()
		{
			if (GUILayout.Button("test"))
			{
				Git.Command("status --porcelain");
			}

			EditorGUILayout.Space();

			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("Staged Changes");
			if (GUILayout.Button(new GUIContent("-", "Unstage all files"), GUILayout.Width(20)) && Git.IsReady())
			{
				Git.Command("reset .");
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
						Git.Command("reset " + gitFile.path);
						EditorGUILayout.EndHorizontal();
						repaintAsap = true;
						break;
					}
					EditorGUILayout.EndHorizontal();
				}
			}
			GUILayout.EndScrollView();

			EditorGUILayout.Space();

			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("Changes");
			if (GUILayout.Button(new GUIContent("+", "Stage all changed files"), GUILayout.Width(20)) && Git.IsReady())
			{
				Git.Command("add .");
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
						Git.Add(gitFile);
						EditorGUILayout.EndHorizontal();
						repaintAsap = true;
						break;
					}
					EditorGUILayout.EndHorizontal();
				}
			}
			GUILayout.EndScrollView();

			EditorGUILayout.Space();

			commitMessage = EditorGUILayout.TextArea(commitMessage, GUILayout.Height(75));
			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button("Commit"))
			{
				if (commitMessage != "")
				{
					Git.Command("commit -m \"" + commitMessage + "\"");
					commitMessage = "";
					repaintAsap = true;
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
				Git.output = "";
			}
			if (Git.output.Length > 15000)
			{
				Git.output = Git.output.Substring(Git.output.Length - 15000);
			}

			EditorGUILayout.EndHorizontal();
			outputScrollPosition = GUILayout.BeginScrollView(outputScrollPosition, GUILayout.Height(150));
			GUILayout.TextArea(Git.output, GUILayout.ExpandHeight(true));
			GUILayout.EndScrollView();
		}
		
		void Update()
		{
			if (repaintAsap && Git.IsReady())
			{
				Git.RefreshStatus();
				repaintAsap = false;
				Repaint();
			}

			Git.Update();
		}
	}
}
