// Copyright 2014 Google Inc. All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using UnityEngine; 
using UnityEditor; 
using System; 

public class CustomAssetMaker 
{
    public static T CreateAsset<T>(string name) where T : ScriptableObject
    {
        T asset = ScriptableObject.CreateInstance(typeof(T)) as T;
        SerializeAsset( asset, name );
        Selection.activeObject = asset;  
        return asset;
    }
    
    public static void SerializeAsset( ScriptableObject asset, string name )
    {
        string selectedPath = AssetDatabase.GetAssetPath(Selection.activeObject);
        if(selectedPath.Contains("Assets/"))
        {
            if(Selection.activeObject.GetType() == typeof(UnityEngine.Object))
            {
                selectedPath+="/";
            }
            
            int dir = selectedPath.LastIndexOf('/');
            if(dir != -1)
            {
                selectedPath = selectedPath.Substring(0,dir+1);
                Debug.Log(selectedPath);
            }
            else
            {
                selectedPath = "Assets/";   
            }
        }
        else
        {
            selectedPath = "Assets/";
        }
        AssetDatabase.CreateAsset(asset, AssetDatabase.GenerateUniqueAssetPath(selectedPath+name+".asset")); 
        AssetDatabase.SaveAssets();
    }
}
