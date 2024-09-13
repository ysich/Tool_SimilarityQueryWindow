/*---------------------------------------------------------------------------------------
-- 负责人: onemt
-- 创建时间: 2023-12-14 11:07:19
-- 概述:
---------------------------------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Editor.Tools
{
    [Serializable]
    public class SimilarityQueryInfo
    {
        public string Name;
        public SimilarityQueryType QueryType = SimilarityQueryType.AssetSimilarityQuery;
        public AssetTypeFlag AssetTypeFlag = AssetTypeFlag.Texture;
        public string Path;
        public bool isSkipSameFolder;
    }
    public class SimilarityQuerySettingData:ScriptableObject
    {
        [SerializeField]
        public List<SimilarityQueryInfo> SimilarityQueryDatas;
    }
}