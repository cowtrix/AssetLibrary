using System;
using System.Collections.Generic;
using UnityEngine;

namespace AssetLibrary
{
    public class AssetLabelCollection : ScriptableObject
    {
        [Serializable]
        public class LabelCollection
        {
            public AssetLabel Label;
            public List<string> Assets = new List<string>();
        }
        public List<LabelCollection> Labels = new List<LabelCollection>();
    }
}

