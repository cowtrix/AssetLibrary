using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace AssetLibrary
{
    public class AssetLibraryWindow : EditorWindow
    {
        public class SearchResult
        {
            public string AssetPath;
            public Object Object;
        }

        private List<Object> m_focusedObjects = new List<Object>();
        private AssetLabel m_tempLabel = new AssetLabel { Color = Color.white };
        private Vector2 m_tagScroll, m_searchScroll;
        private string m_search;
        private List<AssetLabel> m_searchFilter = new List<AssetLabel>();
        private List<SearchResult> m_searchResults = new List<SearchResult>();
        private Dictionary<string, string> m_guidPathCache = new Dictionary<string, string>();
        private bool m_searchDirty, m_requireLabel;

        [MenuItem("Window/General/Asset Libary")]
        public static void OpenLibraryWindow()
        {
            var w = GetWindow<AssetLibraryWindow>();
        }

        private void OnEnable()
        {
            m_searchDirty = true;
        }

        private void OnSelectionChange()
        {
            if (Selection.objects != null && Selection.objects.Length > 0)
            {
                m_focusedObjects.Clear();
                foreach (var selected in Selection.objects)
                {
                    if (selected is GameObject go)
                    {
                        var prefab = AssetDatabase.LoadAssetAtPath(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go), typeof(Object));
                        if (prefab)
                        {
                            m_focusedObjects.Add(prefab);
                        }
                    }
                    else
                    {
                        m_focusedObjects.Add(selected);
                    }
                }
            }
        }

        private string GUIDToAssetPath(string guid)
        {
            if(!m_guidPathCache.TryGetValue(guid, out var path))
            {
                path = AssetDatabase.GUIDToAssetPath(guid);
                m_guidPathCache[guid] = path;
            }
            return path;
        }

        private void Update()
        {
            if (m_searchDirty)
            {
                m_searchResults.Clear();
                var allAssets = AssetDatabase.FindAssets(m_search);
                var matchCounter = 0;
                foreach (var assetGuid in allAssets)
                {
                    var path = GUIDToAssetPath(assetGuid);
                    if ((m_searchFilter.Count > 0 || m_requireLabel) && !AssetLibrary.HasLabel(path))
                    {
                        continue;
                    }

                    var filtered = false;
                    foreach(var filter in m_searchFilter)
                    {
                        var assets = AssetLibrary.LabelData.Labels.First(l => l.Label == filter).Assets;
                        if (!assets.Contains(path))
                        {
                            filtered = true;
                            break;
                        }
                    }
                    if (filtered)
                    {
                        continue;
                    }

                    var assetObj = AssetDatabase.LoadAssetAtPath(path, typeof(Object));
                    if (!assetObj || assetObj is DefaultAsset)
                    {
                        continue;
                    }

                    m_searchResults.Add(new SearchResult
                    {
                        AssetPath = assetGuid,
                        Object = assetObj,
                    });
                    matchCounter++;
                    if(matchCounter > 100)
                    {
                        break;
                    }
                }
                m_searchDirty = false;
            }
            this.Repaint();
        }

        private void OnGUI()
        {
            titleContent = new GUIContent("Asset Library");

            EditorGUILayout.BeginHorizontal();
            DoObjectColumn();
            DoSearchColumn();
            EditorGUILayout.EndHorizontal();
        }

        private void DoSearchColumn()
        {
            EditorGUILayout.BeginVertical();
            EditorGUILayout.BeginHorizontal();
            var newSearch = EditorGUILayout.DelayedTextField(m_search, EditorStyles.toolbarSearchField);
            if (GUILayout.Button(EditorGUIUtility.IconContent("Refresh"), EditorStyles.miniButtonLeft, GUILayout.Width(20)))
            {
                m_searchDirty = true;
            }
            GUI.color = m_requireLabel ? Color.green : Color.white;
            if(GUILayout.Button(EditorGUIUtility.IconContent("FilterByLabel"), EditorStyles.miniButtonRight, GUILayout.Width(20)))
            {
                m_requireLabel = !m_requireLabel;
                m_searchDirty = true;
            }
            GUI.color = Color.white;
            EditorGUILayout.EndHorizontal();

            if (newSearch != m_search)
            {
                m_search = newSearch;
                m_searchDirty = true;
            }
            m_searchScroll = EditorGUILayout.BeginScrollView(m_searchScroll, false, true, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));     
            foreach (var result in m_searchResults.Take(100))
            {
                const int rowHeight = 16;
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(new GUIContent(AssetPreview.GetMiniTypeThumbnail(result.Object.GetType())), GUILayout.Width(16), GUILayout.Height(rowHeight));
                if (GUILayout.Button(result.Object.name, EditorStyles.linkLabel, GUILayout.Height(rowHeight)))
                {
                    Selection.objects = new[] { result.Object };
                }
                GUI.color = new Color(1, 1, 1, .5f);
                GUILayout.Label(string.Join(", ", AssetLibrary.GetLabels(result.Object).Select(l => l.ID)), EditorStyles.miniLabel, GUILayout.ExpandWidth(false), GUILayout.Height(rowHeight));
                GUI.color = Color.white;
                EditorGUILayout.EndHorizontal();
            }
            if(m_searchResults.Count == 0)
            {
                GUILayout.Label("No results found");
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DoObjectColumn()
        {
            var assetLabels = new List<AssetLabel>();
            foreach (var obj in m_focusedObjects)
            {
                foreach (var l in AssetLibrary.GetLabels(obj))
                {
                    if (!assetLabels.Contains(l))
                    {
                        assetLabels.Add(l);
                    }
                }
                AssetLibrary.UpdateLabelData(obj);
            }

            const int objectColumnWidth = 200;
            // Draw Object view
            EditorGUILayout.BeginVertical(GUILayout.Width(objectColumnWidth));
            if (m_focusedObjects.Count == 1)
            {
                GUILayout.Label(new GUIContent(AssetPreview.GetAssetPreview(m_focusedObjects[0])), GUILayout.ExpandWidth(true));
                GUI.enabled = false;
                EditorGUILayout.ObjectField(m_focusedObjects[0], typeof(Object), false);
            }
            else
            {
                GUILayout.Label($"{m_focusedObjects.Count} objects", "Box", GUILayout.ExpandWidth(true), GUILayout.Height(150));
            }
            GUI.enabled = true;

            m_tagScroll = EditorGUILayout.BeginScrollView(m_tagScroll, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));

            EditorGUILayout.BeginHorizontal();
            var sortedLabels = AssetLibrary.LabelData.Labels
                .OrderBy(l => assetLabels.Contains(l.Label))
                .ThenBy(l => l.Label.ID)
                .ToList();
            var widthCounter = 0f;
            for (int i = 0; i < sortedLabels.Count; i++)
            {
                var otherLabelID = AssetLibrary.LabelData.Labels[i];
                var label = otherLabelID.Label;
                var perLetterWidth = 10;
                if (assetLabels.Contains(otherLabelID.Label))
                {
                    GUI.color = label.Color != Color.clear ? label.Color : Color.white;
                }
                else
                {
                    GUI.color = label.Color != Color.clear ? new Color(label.Color.r, label.Color.g, label.Color.b, .5f) : new Color(1, 1, 1, .5f);
                }
                var minibuttonStyle = new GUIStyle(EditorStyles.miniButtonLeft);
                var isInFilter = m_searchFilter.Contains(label);
                if (isInFilter)
                {
                    minibuttonStyle.fontStyle = FontStyle.Bold;
                    minibuttonStyle.normal.textColor = Color.green;
                    perLetterWidth = 12;
                }
                if (GUILayout.Button($" {label.Name} ", minibuttonStyle, GUILayout.ExpandWidth(false)))
                {
                    if (isInFilter)
                    {
                        m_searchFilter.Remove(label);
                    }
                    else
                    {
                        m_searchFilter.Add(label);
                    }
                    m_searchDirty = true;
                }
                if (GUILayout.Button(assetLabels.Contains(label) ? "×" : "+", EditorStyles.miniButtonRight, GUILayout.ExpandWidth(false)))
                {
                    if (!assetLabels.Contains(label))
                    {
                        foreach (var obj in m_focusedObjects)
                        {
                            var newLabels = AssetLibrary.GetLabels(obj).ToList();
                            if (!newLabels.Contains(label))
                            {
                                newLabels.Add(label);
                                AssetLibrary.SetLabels(obj, newLabels);
                            }
                        }
                    }
                    else
                    {
                        foreach (var obj in m_focusedObjects)
                        {
                            var newLabels = AssetLibrary.GetLabels(obj).ToList();
                            if (newLabels.Contains(label))
                            {
                                newLabels.Remove(label);
                                AssetLibrary.SetLabels(obj, newLabels);
                            }
                        }
                    }
                    GUIUtility.ExitGUI();
                    return;
                }
                GUI.color = Color.white;

                widthCounter += i < AssetLibrary.LabelData.Labels.Count - 1 ? AssetLibrary.LabelData.Labels[i + 1].Label.ID.Length * perLetterWidth + 25 : 0;
                if (widthCounter > objectColumnWidth)
                {
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    widthCounter = 0f;
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndScrollView();

            GUILayout.Label("Add New Label", EditorStyles.centeredGreyMiniLabel);
            m_tempLabel.Name = AssetLabel.TransformToID(EditorGUILayout.TextField(m_tempLabel.Name));
            m_tempLabel.Color = EditorGUILayout.ColorField(m_tempLabel.Color);
            GUI.enabled = !AssetLibrary.LabelData.Labels.Any(l => l.Label == m_tempLabel);
            if (GUILayout.Button("+"))
            {
                assetLabels.Add(m_tempLabel);
                foreach (var obj in m_focusedObjects)
                {
                    var newLabels = AssetLibrary.GetLabels(obj).ToList();
                    if (!newLabels.Contains(m_tempLabel))
                    {
                        newLabels.Add(m_tempLabel);
                        AssetLibrary.SetLabels(obj, newLabels);
                    }
                }
                m_tempLabel = new AssetLabel { Color = Color.white };
            }

            EditorGUILayout.EndVertical();
        }
    }
}

