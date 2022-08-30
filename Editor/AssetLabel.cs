using System;
using System.Collections.Generic;
using UnityEngine;

namespace AssetLibrary
{
    [Serializable]
    public class AssetLabel
    {
        public static string TransformToID(string name) => name?.ToLower().Replace(' ', '_').Replace('\n', '_');
        public string ID => TransformToID(Name);
        public string Name;
        public Color Color;

        public override bool Equals(object obj)
        {
            return obj is AssetLabel label &&
                   ID == label.ID;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ID);
        }

        public static bool operator ==(AssetLabel left, AssetLabel right)
        {
            return EqualityComparer<AssetLabel>.Default.Equals(left, right);
        }

        public static bool operator !=(AssetLabel left, AssetLabel right)
        {
            return !(left == right);
        }
    }
}

