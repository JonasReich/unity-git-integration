//-------------------------------------------
// Copyright (c) 2017 - JonasReich
//-------------------------------------------

using UnityEditor;
using UnityEngine;

namespace Doodads.Editor
{
	/// <summary>
	/// Extends the number of keywords that can be used in editor templates
	/// </summary>
	public class ScriptTemplateKeywordReplacer : UnityEditor.AssetModificationProcessor
	{
		//--------------------------------
		// Source: https://forum.unity3d.com/threads/c-script-template-how-to-make-custom-changes.273191/
		//--------------------------------
		// This only really makes sence if you have custom script templates placed at
		// [Unity Install Root]\Editor\Data\Resources\ScriptTemplates\
		//--------------------------------

		// This method is called after an asset has been created
		/// <summary>
		/// Replace keywords such as #AUTHOR# in newly created source code files
		/// </summary>
		/// <param name="path"></param>
		public static void OnWillCreateAsset(string path)
		{
			path = path.Replace(".meta", "");
			int index = path.LastIndexOf(".");
			if (index < 0)
				return;

			string file = path.Substring(index);
			if (file != ".cs" && file != ".js" && file != ".boo")
				return;

			index = Application.dataPath.LastIndexOf("Assets");
			path = Application.dataPath.Substring(0, index) + path;
			if (!System.IO.File.Exists(path))
				return;

			string fileContent = System.IO.File.ReadAllText(path);
			fileContent = fileContent.Replace("#YEAR#", Year);
			fileContent = fileContent.Replace("#AUTHOR#", Author);
			fileContent = fileContent.Replace("#NAMESPACE#", GetNamespaceForPath(path));

			System.IO.File.WriteAllText(path, fileContent);
			AssetDatabase.Refresh();
		}

		static string GetNamespaceForPath(string path)
		{
			string spacename = RemoveIllegalChars(ProductName);

			if (path.Contains("Editor"))
				spacename += ".Editor";

			if (path.Contains("UI"))
				spacename += ".UI";

			return spacename;
		}

		static string illegalCharacters = "0123456789 -_#+,.;:!?(){}[]&%$";

		static string RemoveIllegalChars(string text)
		{
			foreach (char c in illegalCharacters)
				text = text.Replace(c.ToString(), "");

			return text;
		}

		static string Year { get { return System.DateTime.Today.Year.ToString(); } }
		static string Author { get { return PlayerSettings.companyName; } }
		static string ProductName { get { return PlayerSettings.productName; } }
	}
}
