using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace AssetLibrary
{
    public class AssetLibraryProcessor : AssetPostprocessor 
    {
        private void OnPreprocessAsset()
        {
            AssetLibrary.UpdateLabelData(assetPath);
        }
    }
}

