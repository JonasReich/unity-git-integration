﻿//-------------------------------------------
// (c) 2017 - Jonas Reich
//-------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using EStatus = GitIntegration.Git.EStatus;

namespace GitIntegration
{
	/// <summary>
	/// Manages everything related to the visualization of git status + context menu entries in the project browser
	/// </summary>
	[InitializeOnLoad]
	public static class GitOverlay
	{
		static Texture addedTexture, ignoredTexture, modifiedTexture, modifiedAddedTexture, unresolvedTexture, untrackedTexture;
		static string currentSelectionPath = "";

		static GitOverlay()
		{
			EditorApplication.projectWindowItemOnGUI += ProjectWindowItemOnGui;
			EditorApplication.projectWindowChanged += delegate { Git.dirty = true; };

			addedTexture = Resources.Load<Texture>("GitIcons/added");
			ignoredTexture = Resources.Load<Texture>("GitIcons/ignored");
			modifiedTexture = Resources.Load<Texture>("GitIcons/modified");
			modifiedAddedTexture = Resources.Load<Texture>("GitIcons/modifiedAdded");
			unresolvedTexture = Resources.Load<Texture>("GitIcons/unresolved");
			untrackedTexture = Resources.Load<Texture>("GitIcons/untracked");

		}

		public static void Update()
		{
			if (Git.dirty && Git.IsReady())
			{
				Git.dirty = false;
				Git.RefreshStatus();
				if (GitEditorWindow.Window)
					GitEditorWindow.Window.Repaint();
			}

			if (UpdateCurrentSelectionPath())
				Git.dirty = true;
		}


		static void ProjectWindowItemOnGui(string guid, Rect selectionRect)
		{
			var path = AssetDatabase.GUIDToAssetPath(guid);

			selectionRect.height = selectionRect.width = 16;
			foreach (var file in Git.files)
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
		

		static List<Git.File> FindSelectedGitFiles(Func<Git.File, bool> conditionFunction)
		{
			List<Git.File> files = new List<Git.File>();

			foreach (UnityEngine.Object obj in Selection.GetFiltered(typeof(UnityEngine.Object), SelectionMode.Assets))
			{
				var path = AssetDatabase.GetAssetPath(obj);
				foreach (var file in Git.files)
				{
					bool isMatchingFile = path.Contains(file.path.Replace("\"", ""));
					bool isMatchingFolderMetaFile = file.isMetaFile && file.isFolder && path.Contains(file.path.Replace(".meta\"", ""));
					if (isMatchingFile || isMatchingFolderMetaFile)
					{
						if (conditionFunction(file))
						{
							files.Add(file);
						}
					}
				}
			}
			return files;
		}


		[MenuItem("Assets/Git/Add", true)]
		public static bool AddValidate()
		{
			return FindSelectedGitFiles(file => file.HasStatus(EStatus.HasUnstagedChanges)).Count > 0;
		}

		[MenuItem("Assets/Git/Add", false)]
		public static void Add()
		{
			Git.Command(Git.ECommand.Add, FindSelectedGitFiles(file => file.HasStatus(EStatus.HasUnstagedChanges)));
		}

		[MenuItem("Assets/Git/Reset", true)]
		public static bool ResetValidate()
		{
			return FindSelectedGitFiles(file => file.HasStatus(EStatus.HasStagedChanges)).Count > 0;
		}

		[MenuItem("Assets/Git/Reset", false)]
		public static void Reset()
		{
			Git.Command(Git.ECommand.Reset, FindSelectedGitFiles(file => file.HasStatus(EStatus.HasStagedChanges)));
		}

		[MenuItem("Assets/Git/Diff", true)]
		public static bool DiffValidate()
		{
			return FindSelectedGitFiles(file => file.HasStatus(EStatus.HasStagedChanges) || file.HasStatus(EStatus.HasUnstagedChanges)).Count > 0;
		}

		[MenuItem("Assets/Git/Diff", false)]
		public static void Diff()
		{
			Git.Command(Git.ECommand.Diff, FindSelectedGitFiles(file => file.HasStatus(EStatus.HasStagedChanges) || file.HasStatus(EStatus.HasUnstagedChanges)));
		}
	}
}
