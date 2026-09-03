#nullable enable
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using SamMul.UnityHelpers.Sounds;

namespace Demo.Editor
{
    /// <summary>
    /// 코드와 정적 데이터가 참조하는 "Sounds/….prefab" 경로마다 SoundObject + AudioSource 프리팹을 만든다.
    /// 오디오 클립은 비워 두며(무음), 이미 있는 프리팹은 건드리지 않는다.
    /// 메뉴: Demo > Build Sound Prefabs  /  배치: -executeMethod Demo.Editor.DemoSoundPrefabBuilder.BuildAll
    /// </summary>
    public static class DemoSoundPrefabBuilder
    {
        private static readonly Regex s_pathPattern = new Regex("\"(Sounds/[A-Za-z0-9_/ .-]+\\.prefab)\"");

        [MenuItem("Demo/Build Sound Prefabs")]
        public static void BuildAll()
        {
            var paths = new SortedSet<string>();
            foreach (var file in Directory.GetFiles("Assets/Scripts", "*.cs", SearchOption.AllDirectories))
            {
                Collect(File.ReadAllText(file), paths);
            }
            if (Directory.Exists("Assets/Resources/StaticData"))
            {
                foreach (var file in Directory.GetFiles("Assets/Resources/StaticData", "*.json"))
                {
                    Collect(File.ReadAllText(file), paths);
                }
            }

            int created = 0;
            foreach (var path in paths)
            {
                string assetPath = "Assets/Resources/" + path;
                if (File.Exists(assetPath))
                {
                    continue;
                }
                Directory.CreateDirectory(Path.GetDirectoryName(assetPath)!);

                var go = new GameObject(Path.GetFileNameWithoutExtension(path));
                var source = go.AddComponent<AudioSource>();
                source.playOnAwake = false;
                go.AddComponent<SoundObject>();
                PrefabUtility.SaveAsPrefabAsset(go, assetPath);
                Object.DestroyImmediate(go);
                ++created;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[DemoSoundPrefabBuilder] referenced={paths.Count} created={created}");
        }

        private static void Collect(string text, SortedSet<string> paths)
        {
            foreach (Match match in s_pathPattern.Matches(text))
            {
                paths.Add(match.Groups[1].Value);
            }
        }
    }
}
