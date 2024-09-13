/*---------------------------------------------------------------------------------------
-- 负责人: onemt
-- 创建时间: 2023-12-14 10:34:36
-- 概述:
---------------------------------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Editor.Tools
{
    public static class SimilarityQueryHelper
    {
        public const string kSimilarityQuerySettingDataName = "SimilarityQuerySettingData";
        public const string kSimilarityQuerySettingDataPath = "Assets/Settings/Editor/SimilarityQuerySettingData.asset";

        private static string GetFindFilter(AssetTypeFlag assetTypeFlag)
        {
            string findTag = string.Empty;
            foreach (AssetTypeFlag tag in Enum.GetValues(typeof(AssetTypeFlag)))
            {
                if (tag != AssetTypeFlag.None && (assetTypeFlag & tag) == tag)
                {
                    string name = Enum.GetName(typeof(AssetTypeFlag), tag);
                    findTag += "t:" + name + " ";
                }
            }

            return findTag;
        }
        public static void FindSimilarityAsset(string queryPath,AssetTypeFlag assetTypeFlag,
            ref HashSet<string> similaritySet,
            ref Dictionary<string, List<string>> queryDict,
            bool isSkipSameFolder = false)
        {
            if (string.IsNullOrEmpty(queryPath))
            {
                Debug.LogError("查找路径不能为空");
                return;
            }

            string[] folders = queryPath.Split(',');
            string findTag = GetFindFilter(assetTypeFlag);

            string[] guids = AssetDatabase.FindAssets(findTag, folders);
            for (int i = 0; i < guids.Length; i++)
            {
                string guid = guids[i];
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                string md5Code = GetMD5CodeByAssetPath(assetPath);
                List<string> md5AssetPaths = null;
                if (queryDict.ContainsKey(md5Code))
                {
                    md5AssetPaths = queryDict[md5Code];
                    similaritySet.Add(md5Code);
                }
                else
                {
                    md5AssetPaths = new List<string>();
                    queryDict[md5Code] = md5AssetPaths;
                }

                md5AssetPaths.Add(assetPath);
            }

            if (isSkipSameFolder)
            {
                SkipSameFolder(ref similaritySet,ref queryDict);
            }
        }

        public static void FindSameNameAsset(string queryPath,AssetTypeFlag assetTypeFlag,
            ref HashSet<string> similaritySet,
            ref Dictionary<string, List<string>> queryDict)
        {
            if (string.IsNullOrEmpty(queryPath))
            {
                Debug.LogError("查找路径不能为空");
                return;
            }
            
            string[] folders = queryPath.Split(',');
            string findTag = GetFindFilter(assetTypeFlag);
            
            string[] guids = AssetDatabase.FindAssets(findTag, folders);
            for (int i = 0; i < guids.Length; i++)
            {
                string guid = guids[i];
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                string assetName = Path.GetFileName(assetPath);
                List<string> assetPaths = null;
                if (queryDict.ContainsKey(assetName))
                {
                    assetPaths = queryDict[assetName];
                    similaritySet.Add(assetName);
                }
                else
                {
                    assetPaths = new List<string>();
                    queryDict[assetName] = assetPaths;
                }

                assetPaths.Add(assetPath);
            }
        }

        public static void SkipSameFolder(ref HashSet<string> similarityMD5Set,
            ref Dictionary<string, List<string>> queryDict)
        {
            HashSet<string> directoryHashSet = new HashSet<string>();
            List<string> removeList = new List<string>();
            foreach (string md5Code in similarityMD5Set)
            {
                directoryHashSet.Clear();
                List<string> assetPaths = queryDict[md5Code];
                for (int i = 0; i < assetPaths.Count; i++)
                {
                    string assetPath = assetPaths[i];
                    string directoryName = Path.GetDirectoryName(assetPath);
                    directoryHashSet.Add(directoryName);
                }

                if (directoryHashSet.Count <= 1)
                {
                    removeList.Add(md5Code);
                }
            }

            foreach (var md5Code in removeList)
            {
                similarityMD5Set.Remove(md5Code);
                queryDict.Remove(md5Code);
            }
        }
        private static string GetMD5CodeByAssetPath(string assetPath)
        {
            FileStream fs = new FileStream(assetPath, FileMode.Open);
            System.Security.Cryptography.MD5 md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
            byte[] retVal = md5.ComputeHash(fs);
            fs.Close();
            string md5Code = BitConverter.ToString(retVal).ToLower().Replace("-", "");
            return md5Code;
        }
        /// <summary>
        /// 出包前根据配置信息进行资源查重
        /// </summary>
        public static void SimilarityQueryByConfigData()
        {
            SimilarityQuerySettingData similarityQuerySettingData = null;
            if(!EditorBuildSettings.TryGetConfigObject(kSimilarityQuerySettingDataName, out similarityQuerySettingData))
            {
                similarityQuerySettingData = AssetDatabase.LoadAssetAtPath<SimilarityQuerySettingData>(kSimilarityQuerySettingDataPath);
                if (similarityQuerySettingData != null)
                {
                    EditorBuildSettings.AddConfigObject(kSimilarityQuerySettingDataPath,similarityQuerySettingData,true);
                }
            }

            if (similarityQuerySettingData == null)
            {
                return;
            }

            Dictionary<string, List<string>> queryDict = new Dictionary<string, List<string>>();
            HashSet<string> similaritySet = new HashSet<string>();

            Dictionary<string, List<string>> serializeDict = new Dictionary<string, List<string>>();
            
            List<SimilarityQueryInfo> similarityQueryDatas = similarityQuerySettingData.SimilarityQueryDatas;
            for (int i = 0; i < similarityQueryDatas.Count; i++)
            {
                queryDict.Clear();
                similaritySet.Clear();
                SimilarityQueryInfo similarityQueryInfo = similarityQueryDatas[i];
                string path = similarityQueryInfo.Path;
                AssetTypeFlag assetTypeFlag = similarityQueryInfo.AssetTypeFlag;
                bool isSkipSameFolder = similarityQueryInfo.isSkipSameFolder;
                
                SimilarityQueryType queryType = similarityQueryInfo.QueryType;
                switch (queryType)
                {
                    case SimilarityQueryType.AssetSimilarityQuery:
                        FindSimilarityAsset(path, assetTypeFlag, ref similaritySet, ref queryDict, isSkipSameFolder);
                        break;
                    case SimilarityQueryType.NameSimilarityQuery:
                        FindSameNameAsset(path, assetTypeFlag, ref similaritySet, ref queryDict);
                        break;
                }
                
                if (similaritySet.Count > 0)
                {
                    List<string> assetPaths = new List<string>();
                    foreach (var key in similaritySet)
                    {
                        List<string> similarityAssetPaths = queryDict[key];
                        assetPaths.AddRange(similarityAssetPaths);
                    }
                    serializeDict.Add(similarityQueryInfo.Name,assetPaths);
                }
            }

            if (serializeDict.Count > 0)
            {
                SerializeResult(serializeDict);
                SimilarityQueryWindow.ShowWindow(true);
            }
            else
            {
                Debug.Log("资源查重结束，未发现重复资源");
            }
           
        }
        public static void SerializeResult(Dictionary<string, List<string>> serializeDict)
        {
            if (serializeDict.Count == 0)
            {
                return;
            }
            FileStream stream = new FileStream(Application.dataPath + "/SimilarityQueryResult.txt", FileMode.Create, FileAccess.Write, FileShare.None);
            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(stream, serializeDict);
            stream.Close();
            Debug.Log("SimilarityQueryResult：序列化完成");
        }
        public static Dictionary<string, List<string>> DeSerializeResult()
        {
            string path = Application.dataPath + "/SimilarityQueryResult.txt";
            if (!File.Exists(path))
            {
                return null;
            }
            FileStream readstream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            BinaryFormatter formatter = new BinaryFormatter();
            Dictionary<string, List<string>> serializeDict = (Dictionary<string, List<string>>)formatter.Deserialize(readstream);
            readstream.Close();
            return serializeDict;
        }
    }
}