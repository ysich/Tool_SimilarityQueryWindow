using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;

namespace Editor.Tools
{
    public class SimilarityQueryWindow : EditorWindow
    {
        [MenuItem("Tools/Window/相同资源查找窗口",false,2)]
        public static void ShowWindow()
        {
            SimilarityQueryWindow.ShowWindow(false);
        }
        
        public static void ShowWindow(bool isDeSerializeResult = false)
        {
            SimilarityQueryWindow window = (SimilarityQueryWindow)GetWindow(typeof(SimilarityQueryWindow));
            window.titleContent.text = "相同资源查找窗口";
            window.minSize = new Vector2(500, 600);
            window.Show();
            if (isDeSerializeResult)
            {
                window.DeSerializeResult();
            }
        }

        private static readonly GUIContent r_ContentFindPath = new GUIContent("查找路径：");
        private static readonly GUIContent r_ContentFindType = new GUIContent("查找类型：");
        private static readonly GUIContent r_ContentFindSameName = new GUIContent("相同名字查询");
        private static readonly GUIContent r_ContentSkipSameFolder = new GUIContent("跳过相同文件夹检测","TP图集使用，TP图集打两个相同图片会只保留一份但是有两份配置数据。");
        private static readonly GUIContent r_ContentFind = new GUIContent("查找");
        private static readonly GUIContent r_ContentSerialize = new GUIContent("序列化结果");
        private static readonly GUIContent r_ContentDeSerialize = new GUIContent("反序列化结果");
        private static readonly GUIContent r_ContentResultList = new GUIContent("结果列表：");
        private static readonly GUIContent r_ContentCopy = new GUIContent("复制");
        private Vector2 m_ScrollPos = Vector2.zero;
        
        private bool m_IsSkipSameFolder = false;
        private SimilarityQueryType m_SimilarityQueryType = SimilarityQueryType.AssetSimilarityQuery;
        private AssetTypeFlag m_AssetTypeFlag = AssetTypeFlag.Texture;

        private string m_QueryPath;

        private Dictionary<string, List<string>> m_DeSerializeResult;

        //md5Code/name,path
        private Dictionary<string, List<string>> m_QueryDict = new Dictionary<string, List<string>>();
        private HashSet<string> m_SimilaritySet = new HashSet<string>();

        private Dictionary<string, bool> m_FoldoutDic = new Dictionary<string, bool>();

        private void OnGUI()
        {
            DrawMenu();
            DrawResult();
            DrawDeSerializeResult();
        }

        private void ClearData()
        {
            m_QueryDict.Clear();
            m_SimilaritySet.Clear();
            m_FoldoutDic.Clear();
            m_DeSerializeResult = null;
        }

        private void SerializeResult()
        {
            Dictionary<string, List<string>> serializeDict = new Dictionary<string, List<string>>();
            if (m_SimilaritySet.Count > 0)
            {
                List<string> assetPaths = new List<string>();
                foreach (var key in m_SimilaritySet)
                {
                    List<string> similarityAssetPaths = m_QueryDict[key];
                    assetPaths.AddRange(similarityAssetPaths);
                }
                string name = Enum.GetName(typeof(SimilarityQueryType), m_SimilarityQueryType);
                serializeDict.Add(name,assetPaths);
            }

            SimilarityQueryHelper.SerializeResult(serializeDict);
        }
        private void DeSerializeResult()
        {
            ClearData();
            m_DeSerializeResult = SimilarityQueryHelper.DeSerializeResult();
        }

        private void DrawMenu()
        {
            m_AssetTypeFlag = (AssetTypeFlag)EditorGUILayout.EnumFlagsField(r_ContentFindType, m_AssetTypeFlag);
            m_IsSkipSameFolder = EditorGUILayout.Toggle(r_ContentSkipSameFolder, m_IsSkipSameFolder);
            m_QueryPath = EditorGUILayout.TextField(r_ContentFindPath, m_QueryPath);
            GUI.color = Color.green;
            if (GUILayout.Button(r_ContentFind, GUILayout.Height(30), GUILayout.ExpandWidth(true)))
            {
                ClearData();
                m_SimilarityQueryType = SimilarityQueryType.AssetSimilarityQuery;
                SimilarityQueryHelper.FindSimilarityAsset(m_QueryPath, m_AssetTypeFlag, ref m_SimilaritySet,
                    ref m_QueryDict, m_IsSkipSameFolder);
            }

            if (GUILayout.Button(r_ContentFindSameName, GUILayout.Height(30), GUILayout.ExpandWidth(true)))
            {
                ClearData();
                m_SimilarityQueryType = SimilarityQueryType.NameSimilarityQuery;
                SimilarityQueryHelper.FindSameNameAsset(m_QueryPath, m_AssetTypeFlag,ref m_SimilaritySet,ref m_QueryDict);
            }

            if (GUILayout.Button(r_ContentSerialize, GUILayout.Height(30), GUILayout.ExpandWidth(true)))
            {
                SerializeResult();
            }
            if (GUILayout.Button(r_ContentDeSerialize,GUILayout.Height(30), GUILayout.ExpandWidth(true)))
            {
                DeSerializeResult();
            }
            GUI.color = Color.white;
        }

        private void DrawResult()
        {
            if (m_SimilaritySet.Count == 0)
            {
                return;
            }

            EditorGUILayout.LabelField(r_ContentResultList);
            m_ScrollPos = EditorGUILayout.BeginScrollView(m_ScrollPos);

            foreach (var md5Code in m_SimilaritySet)
            {
                if (!m_FoldoutDic.ContainsKey(md5Code))
                {
                    m_FoldoutDic.Add(md5Code, true);
                }

                GUIStyle style = new GUIStyle();
                style.normal.textColor = Color.red;
                m_FoldoutDic[md5Code] = EditorGUILayout.Foldout(m_FoldoutDic[md5Code], md5Code);

                if (m_FoldoutDic[md5Code])
                {
                    List<string> assetPaths = m_QueryDict[md5Code];
                    for (int i = 0; i < assetPaths.Count; i++)
                    {
                        string assetPath = assetPaths[i];
                        GUILayout.BeginHorizontal();
                        // 复制名称按钮
                        if (GUILayout.Button(r_ContentCopy, GUILayout.MaxWidth(100f)))
                        {
                            GUIUtility.systemCopyBuffer = assetPath;
                        }

                        GUILayout.Space(5);
                        Object obj = AssetDatabase.LoadAssetAtPath(assetPath, typeof(Object));
                        EditorGUILayout.ObjectField(obj.name, obj, typeof(Object), true);
                        GUILayout.EndHorizontal();
                    }
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawDeSerializeResult()
        {
            if (m_DeSerializeResult == null)
            {
                return;
            }
            EditorGUILayout.LabelField(r_ContentResultList);
            m_ScrollPos = EditorGUILayout.BeginScrollView(m_ScrollPos);

            foreach (var kv in m_DeSerializeResult)
            {
                string name = kv.Key;
                if (!m_FoldoutDic.ContainsKey(name))
                {
                    m_FoldoutDic.Add(name, true);
                }

                GUIStyle style = new GUIStyle();
                style.normal.textColor = Color.red;
                m_FoldoutDic[name] = EditorGUILayout.Foldout(m_FoldoutDic[name], name);

                if (m_FoldoutDic[name])
                {
                    List<string> assetPaths = kv.Value;
                    for (int i = 0; i < assetPaths.Count; i++)
                    {
                        string assetPath = assetPaths[i];
                        GUILayout.BeginHorizontal();
                        // 复制名称按钮
                        if (GUILayout.Button(r_ContentCopy, GUILayout.MaxWidth(100f)))
                        {
                            GUIUtility.systemCopyBuffer = assetPath;
                        }

                        GUILayout.Space(5);
                        Object obj = AssetDatabase.LoadAssetAtPath(assetPath, typeof(Object));
                        EditorGUILayout.ObjectField(obj.name, obj, typeof(Object), true);
                        GUILayout.EndHorizontal();
                    }
                }
            }
            
            EditorGUILayout.EndScrollView();
        }
    }
}