using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace AssetLibrary
{
    public static class AssetLibrary
    {
        public static AssetLabelCollection LabelData
        {
            get
            {
                if (!__labelCollection)
                {
                    __labelCollection = Resources.Load<AssetLabelCollection>("AssetLabelCollection");
                }
                return __labelCollection;
            }
        }
        private static AssetLabelCollection __labelCollection;

        private static HashSet<string> m_hasLabelLookup;

        public static bool HasLabel(string assetPath)
        {
            if(m_hasLabelLookup == null)
            {
                BuildHasLabelLookupTable();
            }
            return m_hasLabelLookup.Contains(assetPath);
        }

        private static void BuildHasLabelLookupTable()
        {
            m_hasLabelLookup = new HashSet<string>();
            foreach(var l in LabelData.Labels)
            {
                foreach(var a in l.Assets)
                {
                    m_hasLabelLookup.Add(a);
                }
            }
        }

        public static AssetLabel GetLabelForID(string id)
        {
            if (!LabelData)
            {
                return null;
            }
            foreach (var data in LabelData.Labels)
            {
                if (data.Label.ID == id)
                {
                    return data.Label;
                }
            }
            return null;
        }

        public static void SetLabels(Object obj, IEnumerable<AssetLabel> labels)
        {
            if (!AssetDatabase.Contains(obj))
            {
                Debug.LogError($"Cannot add label: {obj} is not in Asset Database", obj);
                return;
            }
            var assetPath = AssetDatabase.GetAssetPath(obj);
            var metaPath = AssetDatabase.GetTextMetaFilePathFromAssetPath(assetPath);
            var metaData = File.ReadAllLines(metaPath);
            var userDataLineFound = false;
            for (int i = 0; i < metaData.Length; i++)
            {
                const string userDataPrefix = "  userData:";
                var line = metaData[i];
                if (!line.StartsWith(userDataPrefix))
                {
                    continue;
                }

                userDataLineFound = true;
                var startWriteIndex = userDataPrefix.Length;
                var endWriteIndex = line.Length - 1;
                var rgx = Regex.Match(line, "labels={(.*)}");
                if (rgx.Success)
                {
                    startWriteIndex = rgx.Index;
                    endWriteIndex = rgx.Index + rgx.Length;
                }

                var labelIDs = labels.Select(l => l.ID).ToList();
                var labelStr = labels.Any() ? $" labels={{{string.Join('|', labelIDs)}}}" : "";
                metaData[i] = line.Substring(0, startWriteIndex) + labelStr + line.Substring(endWriteIndex, line.Length - endWriteIndex);
            }
            if (!userDataLineFound)
            {
                Debug.LogError($"Unable to find userData field in: {metaPath}", obj);
                return;
            }
            File.WriteAllLines(metaPath, metaData);
            AssetDatabase.ImportAsset(assetPath);
            BuildHasLabelLookupTable();
        }

        public static IEnumerable<AssetLabel> GetLabels(Object m_focusedObject)
        {
            var assetPath = AssetDatabase.GetAssetPath(m_focusedObject);
            if (string.IsNullOrEmpty(assetPath))
            {
                yield break;
            }
            foreach (var collection in LabelData.Labels)
            {
                if (collection.Assets.Contains(assetPath))
                {
                    yield return collection.Label;
                }
            }
        }

        private static AssetLabel AddLabel(string name, Color color)
        {
            if (!LabelData)
            {
                if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                {
                    AssetDatabase.CreateFolder("Assets", "Resources");
                }
                AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<AssetLabelCollection>(), "Assets/Resources/AssetLabelCollection.asset");
            }
            var newLabel = new AssetLabel { Name = name, Color = color };
            var existingLabelIndex = LabelData.Labels.FindIndex(l => l.Label == newLabel);
            if (existingLabelIndex >= 0)
            {
                return LabelData.Labels[existingLabelIndex].Label;
            }
            LabelData.Labels.Add(new AssetLabelCollection.LabelCollection { Label = newLabel });
            EditorUtility.SetDirty(LabelData);
            return newLabel;
        }

        public static void UpdateLabelData(Object obj)
        {
            var assetPath = AssetDatabase.GetAssetPath(obj);
            if (string.IsNullOrEmpty(assetPath))
            {
                Debug.LogError($"Object {obj} was not found in Asset Database", obj);
                return;
            }
            UpdateLabelData(assetPath);
        }

        public static void UpdateLabelData(string assetPath)
        {
            var metaPath = AssetDatabase.GetTextMetaFilePathFromAssetPath(assetPath);
            var metaData = File.ReadAllLines(metaPath);
            for (int i = 0; i < metaData.Length; i++)
            {
                const string userDataPrefix = "  userData:";
                var line = metaData[i];
                if (!line.StartsWith(userDataPrefix))
                {
                    continue;
                }

                var rgx = Regex.Match(line, "labels={(.*)}");
                if (!rgx.Success)
                {
                    return;
                }
                var labels = rgx.Groups[1].Value.Split('|', System.StringSplitOptions.RemoveEmptyEntries);
                foreach (var labelID in labels)
                {
                    var labelData = LabelData.Labels.FirstOrDefault(l => l.Label.ID == labelID);
                    if (labelData == null)
                    {
                        labelData = new AssetLabelCollection.LabelCollection { Label = new AssetLabel { Name = labelID } };
                        LabelData.Labels.Add(labelData);
                        EditorUtility.SetDirty(LabelData);
                    }

                    if (!labelData.Assets.Contains(assetPath))
                    {
                        labelData.Assets.Add(assetPath);
                    }
                }
            }
        }
    }
}

