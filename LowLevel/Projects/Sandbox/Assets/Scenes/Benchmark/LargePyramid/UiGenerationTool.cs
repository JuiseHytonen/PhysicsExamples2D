#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public static class UIGenerationTool
{
	private const string SCRIPT_LINKING_PENDING = "SCRIPT_LINKING_PENDING";

	[MenuItem("Tools/UI/Create Popup UI from selected UXML", false, 22)]
	public static void CreatePopupUIFromSelected()
	{
		VisualTreeAsset uxmlSelection = Selection.activeObject as VisualTreeAsset;
		if (uxmlSelection != null)
		{
			string pathString = AssetDatabase.GetAssetPath(uxmlSelection);
			string templatePath = Application.dataPath + "/ScriptTemplates/81-Custom C# Scripts__Popup UI Script-NewPopupUIScript.cs.txt";
			string templateText = File.ReadAllText(templatePath);
			string ending = pathString.Split("UIDocuments/")[1];
			string originalFilename = Path.GetFileName(pathString).Replace(".uxml", "");
			templateText = templateText.Replace("#SCRIPTNAME#", originalFilename);
			templateText = templateText.Replace("#ROOTNAMESPACEBEGIN#", "");
			templateText = templateText.Replace("#ROOTNAMESPACEEND#", "");
			const string templatePopupName = "TemplatePopupPrefab";
			File.WriteAllText(Application.dataPath + "/Game/Scripts/UI/" + ending.Replace("uxml", "cs"), templateText);
			string prefabPath = $"Assets/Resources/Prefabs/UI/Popup/{templatePopupName}.prefab";
			string newPrefabPath = prefabPath.Replace(templatePopupName, originalFilename);
			AssetDatabase.CopyAsset(prefabPath, newPrefabPath);
			GameObject savedObject = AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(newPrefabPath);
			savedObject.GetComponent<UIDocument>().visualTreeAsset = uxmlSelection;
			savedObject.GetComponent<UIDocument>().panelSettings =
				AssetDatabase.LoadAssetAtPath<PanelSettings>(
					"Assets/Game/UIToolkit/PanelSettings/HighestPriorityPanelSettings.asset");
			EditorPrefs.SetString(SCRIPT_LINKING_PENDING, pathString);
			EditorUtility.SetDirty(savedObject);
			AssetDatabase.SaveAssetIfDirty(savedObject);
			AssetDatabase.Refresh();
		}
		else
		{
			EditorUtility.DisplayDialog("Error", "You need to select UXML document to use this tool", "got it");
		}
	}

	[UnityEditor.Callbacks.DidReloadScripts]
	private static void LinkNewScriptWhenReady()
	{
		if (string.IsNullOrEmpty(EditorPrefs.GetString(SCRIPT_LINKING_PENDING, "")))
		{
			return;
		}

		if (EditorApplication.isCompiling || EditorApplication.isUpdating)
		{
			EditorApplication.delayCall += LinkNewScriptWhenReady;
			return;
		}
		string pathString = EditorPrefs.GetString(SCRIPT_LINKING_PENDING);
		EditorPrefs.SetString(SCRIPT_LINKING_PENDING, "");
		bool isPopup = pathString.Contains("Popup");
		string newPrefabPath = "";
		string newClassPath = "";
		if (isPopup)
		{
			string ending = pathString.Split("UIDocuments/")[1];
			string originalFilename = Path.GetFileName(pathString).Replace(".uxml", "");
			newPrefabPath = $"Assets/Resources/Prefabs/UI/Popup/{originalFilename}.prefab";
			newClassPath = "Assets/Game/Scripts/UI/" + ending.Replace("uxml", "cs");
		}
		else
		{
			newPrefabPath = pathString.Replace("uxml", "prefab");
			newClassPath = pathString.Replace("uxml", "cs");
		}

		GameObject savedObject = AssetDatabase.LoadAssetAtPath<GameObject>(newPrefabPath);
		MonoScript newScript = AssetDatabase.LoadAssetAtPath<MonoScript>(newClassPath);
		Type newType = newScript.GetClass();
		savedObject.AddComponent(newType);
		EditorUtility.SetDirty(savedObject);
		AssetDatabase.SaveAssetIfDirty(savedObject);
		AssetDatabase.Refresh();
	}

	[MenuItem("Tools/UI/Use copied style name from UI Builder to make referencing code #%V", false, 41)]
	public static void UseCopiedStyleNameFromUIBuilder()
	{
		string styleName = GUIUtility.systemCopyBuffer.Replace(".", "");
		GUIUtility.systemCopyBuffer = $"private const string STYLE_{styleName.ToUpper()} = \"{styleName}\";";
	}

	[MenuItem("Tools/UI/Use first copied element from UI Builder to make referencing code #%B", false, 42)]
	public static void UseFirstCopiedElementFromUIBuilder()
	{
		ConvertClipBoardToCsCode(true);
	}

	[MenuItem("Tools/UI/Use copied elements from UI Builder to make referencing code #%C", false, 43)]
	public static void UseCopiedElementsFromUIBuilder()
	{
		ConvertClipBoardToCsCode();
	}

	private static void ConvertClipBoardToCsCode(bool onlyFirstElement = false)
	{
		string text = GUIUtility.systemCopyBuffer;
		if (!string.IsNullOrEmpty(text))
		{
			// XML is used instead of UXML here since VisualTreeAsset cannot be created from string. If this method somehow fails
			// it´s possible to save temp.uxml file to Resources and load that to VisualTreeAsset using Resources.Load<VisualTreeAsset>()
			XmlDocument elementsAsXML = new XmlDocument();
			try
			{
				elementsAsXML.LoadXml(text);
			}
			catch (Exception)
			{
				EditorUtility.DisplayDialog("Error", "Did you forget to Ctrl-C copy elements you chose in UI Builder?", "ok");
				return;
			}
			FormatItemsToClipboard(elementsAsXML, true, onlyFirstElement: onlyFirstElement);
			FormatItemsToClipboard(elementsAsXML, false, false, onlyFirstElement: onlyFirstElement);
		}
	}

	// Commented out as not very useful tool
	//[MenuItem("Tools/UI/Use copied elements from UI Builder to make referencing code and paste to UXML", false, 22)]
	public static void UseCopiedElementsFromUIBuilderAndPasteToSelectedUXML()
	{
		UseCopiedElementsFromUIBuilder();
		MonoScript csSelection = Selection.activeObject as MonoScript;
		if (csSelection == null)
		{
			EditorUtility.DisplayDialog("Error", "You need to select UXML document to use this tool", "got it");
			return;
		}
		string assetPath = AssetDatabase.GetAssetPath(csSelection);
		string pathStartWithoutAssets = Application.dataPath.Replace($"Assets", "");
		string fullAssetPath = pathStartWithoutAssets + assetPath;
		string csFileContents = File.ReadAllText(fullAssetPath);
		csFileContents = csFileContents.Replace("FindElements()", "FindElements()\n" + GUIUtility.systemCopyBuffer);
		File.WriteAllText(fullAssetPath, csFileContents);
		EditorUtility.SetDirty(csSelection);
		AssetDatabase.SaveAssetIfDirty(csSelection);
	}

	// Commented out as not very useful tool
	//[MenuItem("Tools/UI/Get UI Elements to Clipboard", false, 22)]
	public static void GetUIElementsToClipboard()
	{
		VisualTreeAsset uxmlSelection = Selection.activeObject as VisualTreeAsset;
		if (uxmlSelection != null)
		{
			List<VisualElement> allItems = uxmlSelection.Instantiate().Query<VisualElement>().ToList();
			FormatItemsToClipboard(allItems);
		}
		else
		{
			EditorUtility.DisplayDialog("Error", "You need to select UXML document to use this tool.", "Got it!");
		}
	}

	private static void FormatItemsToClipboard(XmlDocument xmlFromClipboard, bool isMemberReferencing, bool emptyClipboard = true, bool onlyFirstElement = false)
	{
		string toClipboard = "";
		if (emptyClipboard)
		{
			toClipboard = string.Empty;
		}
		else
		{
			toClipboard = GUIUtility.systemCopyBuffer;
		}

		HashSet<XmlNode> elementNodes = new HashSet<XmlNode>();
		int count = 0;
		// Gather all nodes recursively. First one can be skipped because it's the top level UXML
		foreach (XmlNode node in xmlFromClipboard.ChildNodes)
		{
			FindElementNodes(node, ref elementNodes, ref count);
		}
		HashSet<string> reservedNames = new HashSet<string>();
		// Try to get the element info from all nodes
		foreach (XmlNode node in elementNodes)
		{
			string elementTypeName = node.LocalName;    // VisualElement etc.
			// Remove namespace from typename
			if (elementTypeName.Contains("."))
			{
				elementTypeName = elementTypeName.Split(".")[1];
			}
			string elementName = string.Empty;	// The custom name given to this element
			XmlAttribute nameAttribute = node.Attributes["name"];
			if (nameAttribute != null)
			{
				elementName = nameAttribute.Value;
			}
			if (!string.IsNullOrEmpty(elementName) && !elementName.Contains("unity-") && IsUniqueName(elementName, reservedNames) && elementName != elementTypeName)
			{
				if (isMemberReferencing)
				{
					toClipboard += $"\tprivate {elementTypeName} m_{ToCamelCase(elementName)};\n";
				}
				else
				{
					toClipboard += $"\t\tm_{ToCamelCase(elementName)} = root.Q<{elementTypeName}>(\"{elementName}\");\n";
				}
			}
			if (onlyFirstElement)
			{
				break;
			}
		}

		// Done
		if (!string.IsNullOrEmpty(toClipboard))
		{
			GUIUtility.systemCopyBuffer = toClipboard;
		}

		bool IsUniqueName(string nameToCheck, HashSet<string> existingNames)
		{
			if (!string.IsNullOrEmpty(nameToCheck))
			{
				if (!existingNames.Contains(nameToCheck))
				{
					existingNames.Add(nameToCheck);
					return true;
				}
			}
			return false;
		}

		void FindElementNodes(XmlNode currentNode, ref HashSet<XmlNode> nodeSet, ref int count)
		{
			foreach (XmlNode node in currentNode.ChildNodes)
			{
				nodeSet.Add(node);
				FindElementNodes(node, ref nodeSet, ref count);
			}
		}
	}

	private static void FormatItemsToClipboard(List<VisualElement> allItems)
	{
		string toClipboard = "";
		allItems.ForEach(item =>
		{
			string itemTypeName = item.GetType().Name;
			if (!string.IsNullOrEmpty(item.name) && !item.name.Contains("unity-") && IsUniqueName(item, allItems) && item.name != itemTypeName)
			{
				toClipboard += $"this.{ToCamelCase(item.name)} = this.myVisualElement.Q<{itemTypeName}>(\"{item.name}\");\n";
			}
		});

		// Done
		if (!string.IsNullOrEmpty(toClipboard))
		{
			GUIUtility.systemCopyBuffer = toClipboard;
		}

		bool IsUniqueName(VisualElement itemToCheck, List<VisualElement> allItems)
		{
			int count = 0;
			allItems.ForEach(item =>
			{
				if (item.name == itemToCheck.name)
				{
					count++;
				}
			});
			return count == 1;
		}
	}

	private static string ToCamelCase(string name)
	{
		return Char.ToLowerInvariant(name[0]) + name.Substring(1);
	}


	[MenuItem("Tools/UI/Create UI from selected UXML", false, 22)]
	public static void CreateUIFromSelected()
	{
		VisualTreeAsset uxmlSelection = Selection.activeObject as VisualTreeAsset;
		if (uxmlSelection != null)
		{
			string pathString = AssetDatabase.GetAssetPath(uxmlSelection);
			string templatePath = Application.dataPath + "/ScriptTemplates/81-Custom C# Scripts__Generic UI Script-NewGenericUIScript.cs.txt";
			string templateText = File.ReadAllText(templatePath);
			string originalFilename = Path.GetFileName(pathString);
			templateText = templateText.Replace("#SCRIPTNAME#", originalFilename.Replace(".uxml", ""));
			templateText = templateText.Replace("#ROOTNAMESPACEBEGIN#", "");
			templateText = templateText.Replace("#ROOTNAMESPACEEND#", "");
			const string templatePopupName = "TemplatePopupPrefab";
			var pathStartWithoutAssets = Application.dataPath.Replace($"Assets", "");
			File.WriteAllText(pathStartWithoutAssets + pathString.Replace("uxml", "cs"), templateText);
			string prefabPath = $"Assets/Resources/Prefabs/UI/Popup/{templatePopupName}.prefab";
			string newPrefabPath = pathString.Replace("uxml", "prefab");
			AssetDatabase.CopyAsset(prefabPath, newPrefabPath);
			GameObject savedObject = AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(newPrefabPath);
			savedObject.GetComponent<UIDocument>().visualTreeAsset = uxmlSelection;
			EditorPrefs.SetString(SCRIPT_LINKING_PENDING, pathString);
			EditorUtility.SetDirty(savedObject);
			AssetDatabase.SaveAssetIfDirty(savedObject);
			AssetDatabase.Refresh();
		}
		else
		{
			EditorUtility.DisplayDialog("Error", "You need to select UXML document to use this tool", "got it");
		}
	}
}
#endif
