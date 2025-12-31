using UnityEngine;
using UnityEditor;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class TMPFontUsageFinder : EditorWindow
{
    Vector2 scrollPos;
    List<TextMeshProUGUI> foundTexts = new List<TextMeshProUGUI>();
    Dictionary<TMP_FontAsset, List<TextMeshProUGUI>> fontGroups = new Dictionary<TMP_FontAsset, List<TextMeshProUGUI>>();
    bool showGroupedView = false;
    
    [MenuItem("Tools/TextMeshPro Font Usage Finder")]
    static void ShowWindow()
    {
        var window = GetWindow<TMPFontUsageFinder>("TMP Font Finder");
        window.minSize = new Vector2(600, 400);
    }
    
    void OnGUI()
    {
        EditorGUILayout.Space(10);
        
        // Search button
        if (GUILayout.Button("🔍 Search All Scenes & Prefabs", GUILayout.Height(40)))
        {
            SearchAll();
        }
        
        EditorGUILayout.Space(5);
        
        // View toggle
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button(showGroupedView ? "📋 List View" : "📊 Group by Font", GUILayout.Height(30)))
        {
            showGroupedView = !showGroupedView;
        }
        
        GUILayout.Label($"Total: {foundTexts.Count} objects", EditorStyles.boldLabel);
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(10);
        
        // Display results
        scrollPos = GUILayout.BeginScrollView(scrollPos);
        
        if (showGroupedView)
        {
            DrawGroupedView();
        }
        else
        {
            DrawListView();
        }
        
        GUILayout.EndScrollView();
    }
    
    void SearchAll()
    {
        foundTexts.Clear();
        fontGroups.Clear();
        
        EditorUtility.DisplayProgressBar("Searching...", "Finding TextMeshPro objects...", 0f);
        
        // Search prefabs
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
        
        for (int i = 0; i < prefabGuids.Length; i++)
        {
            EditorUtility.DisplayProgressBar("Searching...", $"Scanning prefabs... {i}/{prefabGuids.Length}", (float)i / prefabGuids.Length);
            
            string path = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            
            if (prefab != null)
            {
                var tmps = prefab.GetComponentsInChildren<TextMeshProUGUI>(true);
                foundTexts.AddRange(tmps);
            }
        }
        
        // Search current scene
        var sceneTmps = GameObject.FindObjectsOfType<TextMeshProUGUI>(true);
        foundTexts.AddRange(sceneTmps);
        
        // Group by font
        foreach (var tmp in foundTexts)
        {
            if (tmp == null) continue;
            
            var font = tmp.font;
            if (font == null) font = null; // Normalize null
            
            if (!fontGroups.ContainsKey(font))
            {
                fontGroups[font] = new List<TextMeshProUGUI>();
            }
            
            fontGroups[font].Add(tmp);
        }
        
        EditorUtility.ClearProgressBar();
        
        Debug.Log($"[TMPFinder] ✅ Found {foundTexts.Count} TextMeshPro objects using {fontGroups.Count} different fonts");
    }
    
    void DrawListView()
    {
        foreach (var tmp in foundTexts)
        {
            if (tmp == null) continue;
            
            EditorGUILayout.BeginHorizontal("box");
            
            // Object name and text content
            string displayText = string.IsNullOrEmpty(tmp.text) ? "<empty>" : tmp.text;
            if (displayText.Length > 50) displayText = displayText.Substring(0, 47) + "...";
            
            if (GUILayout.Button($"{tmp.gameObject.name}", GUILayout.Width(200)))
            {
                Selection.activeGameObject = tmp.gameObject;
                EditorGUIUtility.PingObject(tmp.gameObject);
            }
            
            GUILayout.Label($"\"{displayText}\"", GUILayout.Width(250));
            
            // Font name
            string fontName = tmp.font != null ? tmp.font.name : "⚠️ None";
            GUILayout.Label($"Font: {fontName}", GUILayout.Width(200));
            
            EditorGUILayout.EndHorizontal();
        }
    }
    
    void DrawGroupedView()
    {
        // Sort by usage count
        var sortedFonts = fontGroups.OrderByDescending(kvp => kvp.Value.Count);
        
        foreach (var fontGroup in sortedFonts)
        {
            var font = fontGroup.Key;
            var objects = fontGroup.Value;
            
            // Font header
            EditorGUILayout.BeginVertical("box");
            
            string fontName = font != null ? font.name : "⚠️ NO FONT";
            string fontSizeInfo = "";
            
            if (font != null)
            {
                // Get font file size
                string assetPath = AssetDatabase.GetAssetPath(font);
                if (!string.IsNullOrEmpty(assetPath))
                {
                    var fileInfo = new System.IO.FileInfo(assetPath);
                    float sizeMB = fileInfo.Length / (1024f * 1024f);
                    fontSizeInfo = $" ({sizeMB:F2} MB)";
                }
            }
            
            EditorGUILayout.LabelField($"📁 {fontName}{fontSizeInfo}", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"   Used in {objects.Count} objects", EditorStyles.miniLabel);
            
            EditorGUI.indentLevel++;
            
            // List objects using this font
            foreach (var tmp in objects)
            {
                if (tmp == null) continue;
                
                EditorGUILayout.BeginHorizontal();
                
                string displayText = string.IsNullOrEmpty(tmp.text) ? "<empty>" : tmp.text;
                if (displayText.Length > 40) displayText = displayText.Substring(0, 37) + "...";
                
                if (GUILayout.Button($"{tmp.gameObject.name}: \"{displayText}\"", GUILayout.MinWidth(300)))
                {
                    Selection.activeGameObject = tmp.gameObject;
                    EditorGUIUtility.PingObject(tmp.gameObject);
                }
                
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUI.indentLevel--;
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }
    }
}
