//-------------------------------------------
// (c) 2017 - Jonas Reich
//-------------------------------------------

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;

namespace GitIntegration
{
	/// <summary>
	/// Adds context menu item to open a selected project folder in Powershell
	/// </summary>
	public static class OpenPowershell
	{
		[MenuItem("Assets/Open in Powershell")]
		static bool IsSelectedFolderValid()
		{
			return AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(Selection.activeObject.GetInstanceID()));
		}

		[MenuItem("Assets/Open in Powershell")]
		static void OpenSelectedFolderInPowershell()
		{
			Process p = new Process();
			p.StartInfo.FileName = "powershell";
			p.StartInfo.WorkingDirectory = AssetDatabase.GetAssetPath(Selection.activeObject.GetInstanceID());
			p.Start();
		}
	}
}
