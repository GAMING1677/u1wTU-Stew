using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;

public class CodeStringExtractor : EditorWindow
{
    [MenuItem("Tools/Extract Strings from Code")]
    static void ExtractStrings()
    {
        HashSet<char> allChars = new HashSet<char>();
        List<string> allStrings = new List<string>();
        
        Debug.Log("[CodeExtractor] Extracting string literals from C# files...");
        
        // Assets/Scripts フォルダ内のすべての.csファイルを検索
        string scriptsPath = Path.Combine(Application.dataPath, "Scripts");
        
        if (!Directory.Exists(scriptsPath))
        {
            Debug.LogWarning($"[CodeExtractor] Scripts folder not found: {scriptsPath}");
            return;
        }
        
        string[] csFiles = Directory.GetFiles(scriptsPath, "*.cs", SearchOption.AllDirectories);
        Debug.Log($"[CodeExtractor] Found {csFiles.Length} C# files");
        
        int fileCount = 0;
        foreach (string filePath in csFiles)
        {
            ExtractStringsFromFile(filePath, allStrings, allChars);
            
            if (++fileCount % 10 == 0)
            {
                EditorUtility.DisplayProgressBar("Extracting Strings", $"{fileCount}/{csFiles.Length}", (float)fileCount / csFiles.Length);
            }
        }
        
        EditorUtility.ClearProgressBar();
        
        // 結果を保存
        string outputPath = "Assets/code_strings.txt";
        SaveExtractedStrings(allStrings, outputPath);
        
        string charactersPath = "Assets/code_characters.txt";
        File.WriteAllText(charactersPath, new string(allChars.OrderBy(c => c).ToArray()));
        
        Debug.Log($"[CodeExtractor] ✅ Extracted {allStrings.Count} strings containing {allChars.Count} unique characters");
        Debug.Log($"[CodeExtractor] Strings saved to: {outputPath}");
        Debug.Log($"[CodeExtractor] Characters saved to: {charactersPath}");
        
        AssetDatabase.Refresh();
        EditorUtility.RevealInFinder(outputPath);
    }
    
    static void ExtractStringsFromFile(string filePath, List<string> strings, HashSet<char> chars)
    {
        string code = File.ReadAllText(filePath);
        
        // 正規表現パターン
        // 1. 通常の文字列: "..."
        // 2. 逐語的文字列: @"..."
        // 3. 補間文字列: $"..." または $@"..."
        
        // 簡易版: シングル/ダブルクォートで囲まれた文字列を抽出
        // (完全な構文解析ではないが、実用上十分)
        
        // パターン1: 通常の文字列 "..."
        Regex normalString = new Regex(@"""([^""\\]|\\.)*""");
        
        // パターン2: 逐語的文字列 @"..."
        Regex verbatimString = new Regex(@"@""([^""]|"""")*""");
        
        // パターン3: 補間文字列 $"..." または $@"..."
        Regex interpolatedString = new Regex(@"\$@?""([^""\\]|\\.)*""");
        
        // すべてのマッチを収集
        List<Match> allMatches = new List<Match>();
        allMatches.AddRange(normalString.Matches(code).Cast<Match>());
        allMatches.AddRange(verbatimString.Matches(code).Cast<Match>());
        allMatches.AddRange(interpolatedString.Matches(code).Cast<Match>());
        
        foreach (Match match in allMatches)
        {
            string rawString = match.Value;
            string content = ExtractContent(rawString);
            
            // 空文字列やDebug.Log系は除外
            if (string.IsNullOrWhiteSpace(content))
                continue;
            
            // デバッグログのタグやシステムメッセージは除外（オプション）
            if (content.StartsWith("[") && content.Contains("]") && content.Length < 30)
            {
                // [ClassName] のようなログタグはスキップ（オプション）
                // continue; // 必要に応じてコメント解除
            }
            
            strings.Add(content);
            
            // 文字を収集
            foreach (char c in content)
            {
                chars.Add(c);
            }
        }
    }
    
    static string ExtractContent(string rawString)
    {
        // クォートやプレフィックスを除去
        string content = rawString;
        
        // $@"..." または $"..." → 中身を抽出
        if (content.StartsWith("$@\"") || content.StartsWith("@$\""))
        {
            content = content.Substring(3, content.Length - 4);
        }
        else if (content.StartsWith("$\""))
        {
            content = content.Substring(2, content.Length - 3);
        }
        else if (content.StartsWith("@\""))
        {
            content = content.Substring(2, content.Length - 3);
            // 逐語的文字列の "" を " に変換
            content = content.Replace("\"\"", "\"");
        }
        else if (content.StartsWith("\""))
        {
            content = content.Substring(1, content.Length - 2);
            // エスケープシーケンスを処理
            content = Regex.Unescape(content);
        }
        
        // 補間文字列の {variable} 部分を除去
        content = Regex.Replace(content, @"\{[^}]+\}", "");
        
        return content;
    }
    
    static void SaveExtractedStrings(List<string> strings, string outputPath)
    {
        using (StreamWriter writer = new StreamWriter(outputPath))
        {
            writer.WriteLine("=== コードから抽出された文字列 ===");
            writer.WriteLine($"合計: {strings.Count} 個");
            writer.WriteLine();
            
            // 重複を除去してソート
            var uniqueStrings = strings.Distinct().OrderBy(s => s).ToList();
            
            foreach (string str in uniqueStrings)
            {
                if (str.Length > 100)
                {
                    writer.WriteLine($"- {str.Substring(0, 97)}...");
                }
                else
                {
                    writer.WriteLine($"- {str}");
                }
            }
        }
    }
}
