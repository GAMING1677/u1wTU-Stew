using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;

public class FontCharacterCollector : EditorWindow
{
    [MenuItem("Tools/Collect All Used Characters")]
    static void CollectAllCharacters()
    {
        HashSet<char> allChars = new HashSet<char>();
        
        Debug.Log("[CharCollector] Starting comprehensive character collection...");
        
        // 1. すべてのシーン内のTextMeshProを検索
        CollectFromScenes(allChars);
        
        // 2. すべてのプレハブ内のTextMeshProを検索
        CollectFromPrefabs(allChars);
        
        // 3. ScriptableObject（すべてのstringフィールド）を検索
        CollectFromAllScriptableObjects(allChars);
        
        // 4. 基本文字セットを追加（念のため）
        AddBasicCharacters(allChars);
        
        // ソート済み文字列を出力
        string result = new string(allChars.OrderBy(c => c).ToArray());
        
        // ファイルに保存
        string outputPath = "Assets/used_characters.txt";
        File.WriteAllText(outputPath, result);
        
        // 読みやすい形式でも保存
        string detailedPath = "Assets/used_characters_detailed.txt";
        SaveDetailedCharacterList(allChars, detailedPath);
        
        Debug.Log($"[CharCollector] ✅ Collected {allChars.Count} unique characters.");
        Debug.Log($"[CharCollector] Saved to: {outputPath}");
        Debug.Log($"[CharCollector] Detailed list: {detailedPath}");
        
        AssetDatabase.Refresh();
        EditorUtility.RevealInFinder(outputPath);
    }
    
    static void CollectFromScenes(HashSet<char> chars)
    {
        string[] sceneGuids = AssetDatabase.FindAssets("t:Scene");
        Debug.Log($"[CharCollector] Searching {sceneGuids.Length} scenes...");
        
        int scannedCount = 0;
        foreach (string guid in sceneGuids)
        {
            string scenePath = AssetDatabase.GUIDToAssetPath(guid);
            
            // 読み取り専用パッケージ内のシーンはスキップ
            if (scenePath.StartsWith("Packages/"))
            {
                Debug.Log($"[CharCollector] Skipping read-only scene: {scenePath}");
                continue;
            }
            
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            
            var tmpTexts = GameObject.FindObjectsOfType<TextMeshProUGUI>();
            foreach (var tmp in tmpTexts)
            {
                AddChars(chars, tmp.text);
            }
            
            EditorSceneManager.CloseScene(scene, true);
            scannedCount++;
        }
        
        Debug.Log($"[CharCollector] Scanned {scannedCount} scenes (skipped read-only packages)");
    }
    
    static void CollectFromPrefabs(HashSet<char> chars)
    {
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
        Debug.Log($"[CharCollector] Searching {prefabGuids.Length} prefabs...");
        
        int count = 0;
        foreach (string guid in prefabGuids)
        {
            string prefabPath = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            
            if (prefab != null)
            {
                var tmpTexts = prefab.GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (var tmp in tmpTexts)
                {
                    AddChars(chars, tmp.text);
                }
            }
            
            if (++count % 50 == 0)
            {
                EditorUtility.DisplayProgressBar("Scanning Prefabs", $"{count}/{prefabGuids.Length}", (float)count / prefabGuids.Length);
            }
        }
        
        EditorUtility.ClearProgressBar();
    }
    
    static void CollectFromAllScriptableObjects(HashSet<char> chars)
    {
        // すべてのScriptableObjectアセットを検索
        string[] allAssetGuids = AssetDatabase.FindAssets("t:ScriptableObject");
        Debug.Log($"[CharCollector] Searching {allAssetGuids.Length} ScriptableObject assets...");
        
        int count = 0;
        foreach (string guid in allAssetGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Object obj = AssetDatabase.LoadAssetAtPath<Object>(path);
            
            if (obj != null)
            {
                // リフレクションを使ってすべてのstringフィールドを取得
                CollectStringFieldsFromObject(obj, chars);
            }
            
            if (++count % 50 == 0)
            {
                EditorUtility.DisplayProgressBar("Scanning ScriptableObjects", $"{count}/{allAssetGuids.Length}", (float)count / allAssetGuids.Length);
            }
        }
        
        EditorUtility.ClearProgressBar();
    }
    
    static void CollectStringFieldsFromObject(Object obj, HashSet<char> chars)
    {
        if (obj == null) return;
        
        System.Type type = obj.GetType();
        
        // すべてのフィールドを取得（publicとprivate、SerializeField含む）
        FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        
        foreach (FieldInfo field in fields)
        {
            // stringフィールドの場合
            if (field.FieldType == typeof(string))
            {
                object value = field.GetValue(obj);
                if (value != null)
                {
                    string stringValue = value as string;
                    AddChars(chars, stringValue);
                }
            }
            // List<string>フィールドの場合
            else if (field.FieldType == typeof(List<string>))
            {
                object value = field.GetValue(obj);
                if (value != null)
                {
                    List<string> stringList = value as List<string>;
                    foreach (string str in stringList)
                    {
                        AddChars(chars, str);
                    }
                }
            }
        }
    }
    
    static void AddBasicCharacters(HashSet<char> chars)
    {
        // 基本文字セット（念のため）
        string basics = "、。！？「」（）：・～ー…";
        AddChars(chars, basics);
        
        // 数字と基本記号は確実に含める
        string numbers = "0123456789";
        string symbols = "+-×÷=<>%";
        AddChars(chars, numbers);
        AddChars(chars, symbols);
    }
    
    static void AddChars(HashSet<char> set, string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        foreach (char c in text)
        {
            set.Add(c);
        }
    }
    
    static void SaveDetailedCharacterList(HashSet<char> chars, string outputPath)
    {
        using (StreamWriter writer = new StreamWriter(outputPath))
        {
            writer.WriteLine("=== 使用文字一覧 ===");
            writer.WriteLine($"合計文字数: {chars.Count}");
            writer.WriteLine();
            
            // カテゴリごとに分類
            var hiragana = chars.Where(c => c >= 'ぁ' && c <= 'ん').OrderBy(c => c).ToList();
            var katakana = chars.Where(c => c >= 'ァ' && c <= 'ヶ').OrderBy(c => c).ToList();
            var kanji = chars.Where(c => c >= 0x4E00 && c <= 0x9FFF).OrderBy(c => c).ToList();
            var alphabet = chars.Where(c => (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z')).OrderBy(c => c).ToList();
            var numbers = chars.Where(c => c >= '0' && c <= '9').OrderBy(c => c).ToList();
            var symbols = chars.Except(hiragana).Except(katakana).Except(kanji).Except(alphabet).Except(numbers).OrderBy(c => c).ToList();
            
            writer.WriteLine($"ひらがな ({hiragana.Count}文字):");
            writer.WriteLine(new string(hiragana.ToArray()));
            writer.WriteLine();
            
            writer.WriteLine($"カタカナ ({katakana.Count}文字):");
            writer.WriteLine(new string(katakana.ToArray()));
            writer.WriteLine();
            
            writer.WriteLine($"漢字 ({kanji.Count}文字):");
            writer.WriteLine(new string(kanji.ToArray()));
            writer.WriteLine();
            
            writer.WriteLine($"英字 ({alphabet.Count}文字):");
            writer.WriteLine(new string(alphabet.ToArray()));
            writer.WriteLine();
            
            writer.WriteLine($"数字 ({numbers.Count}文字):");
            writer.WriteLine(new string(numbers.ToArray()));
            writer.WriteLine();
            
            writer.WriteLine($"記号 ({symbols.Count}文字):");
            writer.WriteLine(new string(symbols.ToArray()));
        }
    }
}
