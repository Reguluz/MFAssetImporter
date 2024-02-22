using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Moonflow.MFAssetTools.MFAssetImporter
{
    [CustomEditor(typeof(ModelImporter))]
    public class MFModelImporter : Editor
    {
        Editor _modelImporterEditor;
        private GUIStyle _moduleTitle;
        private string assetPath;
        private string destPath;
        private string assetName;
    #region Common
        public enum ModelImportAssetOperation
        {
            Extract,
            Copy
        }
        [Flags]
        public enum ModelImportAssetPart
        {
            Materials = 0x1,
            Animations = 0x2,
            Avatars = 0x4,
            Meshes = 0x8
        }
        public ModelImportAssetPart assetPart;
        public bool makeUpPrefab;

        private Dictionary<Object, string> _newAssetPathMapDict;
    #endregion
    
    #region Cel Character
        private bool _smoothNormal; 
    #endregion
        
        private void OnEnable()
        {
            _modelImporterEditor = CreateEditor(target, Type.GetType("UnityEditor.ModelImporterEditor, UnityEditor"));
            _moduleTitle = new GUIStyle()
            {
                // border = new RectOffset(5,5,5,5),
                padding = new RectOffset(5,5,0,0),
                margin =  new RectOffset(0,0,20,0),
                fontSize = 14, 
                fontStyle = FontStyle.Bold,
                normal = new GUIStyleState()
                {
                    textColor = Color.white,
                    background = Texture2D.linearGrayTexture
                }
            };

            _newAssetPathMapDict = new Dictionary<Object, string>();
        }

        public override void OnInspectorGUI()
        {
            _modelImporterEditor.OnInspectorGUI();
            using (new EditorGUILayout.VerticalScope())
            {
                EditorGUILayout.LabelField("▣ Common", _moduleTitle);
                using (new EditorGUILayout.VerticalScope("box"))
                {
                    // using (new EditorGUILayout.HorizontalScope())
                    // {
                    //     EditorGUILayout.LabelField("Asset Operation");
                    //     assetOperation =
                    //         (ModelImportAssetOperation)GUILayout.Toolbar((int)assetOperation, assetOperationDisplay);
                    // }

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField("Select part to extract/copy");
                        assetPart = (ModelImportAssetPart)EditorGUILayout.MaskField((int)assetPart,
                                    new[] { "Material", "Animation", "Avatar", "Mesh" });
                    }

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        makeUpPrefab = EditorGUILayout.ToggleLeft("Make Up Prefab", makeUpPrefab);
                    }

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("Extract"))
                        {
                            TotalProcess(ModelImportAssetOperation.Extract);
                        }

                        if (GUILayout.Button("Copy"))
                        {
                            TotalProcess(ModelImportAssetOperation.Copy);
                        }
                    }
                }
            }

            using (new EditorGUILayout.VerticalScope())
            {
                EditorGUILayout.LabelField("▣ Cel Character", _moduleTitle);
                using (new EditorGUILayout.HorizontalScope("box"))
                {
                    _smoothNormal = EditorGUILayout.ToggleLeft("Smooth Normal", _smoothNormal);
                }
                if (GUILayout.Button("Create Cel Character Prefab"))
                    ExportCelCharacter();
            }
        }

        private void TotalProcess(ModelImportAssetOperation assetOperation)
        {
            ModelImporter importer = target as ModelImporter;
            assetPath = importer.assetPath;
            assetName = Path.GetFileName(assetPath);
            string assetFolder = Path.GetDirectoryName(assetPath);
            string newFolder = '\\' + assetName.Split('.')[0];
            destPath = assetFolder + newFolder;
            if (!AssetDatabase.IsValidFolder(assetFolder + newFolder))
            {
                AssetDatabase.CreateFolder(assetFolder, assetName.Split('.')[0]);
            }
            _newAssetPathMapDict.Clear();

            AssetProcess("Materials", "mat", typeof(Material), assetOperation, (assetPart & ModelImportAssetPart.Materials) > 0, out HashSet<string> newMatPathHashSet);
            AssetProcess("Animations", "anim", typeof(Animation), assetOperation, (assetPart & ModelImportAssetPart.Animations) > 0, out HashSet<string> newAnimPathHashSet);
            AssetProcess("", "mask", typeof(AvatarMask), assetOperation, (assetPart & ModelImportAssetPart.Avatars) > 0, out HashSet<string> newAvatarPathHashSet);
            AssetProcess("", "asset", typeof(Mesh), assetOperation, (assetPart & ModelImportAssetPart.Meshes) > 0, out HashSet<string> newMeshesPathHashSet);
            
            if (makeUpPrefab) 
                MakeUpPrefab();
                /*MFModelImportMatTransferEditor.FinishTransfer = MakeUpPrefab;*/
                
#if MF_MAT_PROCESSOR
            if ((assetPart & ModelImportAssetPart.Materials) > 0)
            {
                var matTransferEditor = MFModelImportMatTransferEditor.ShowWindow();
                Material[] mats = new Material[newMatPathHashSet.Count];
                var newPathArray = newMatPathHashSet.ToArray();
                for (int i = 0; i < mats.Length; i++)
                {
                    mats[i] = AssetDatabase.LoadAssetAtPath<Material>(newPathArray[i]);
                }
                matTransferEditor.InitializeData(mats);
            }
#endif
        }

        private void AssetProcess(string folderName, string extension,
            Type assetType, ModelImportAssetOperation assetOperation, bool doProcess, out HashSet<string> outsideAssetPathHashSet)
        {
            string newFolder = destPath + '\\' + folderName;
            if (!AssetDatabase.IsValidFolder(newFolder))
            {
                AssetDatabase.CreateFolder(destPath, folderName);
            }
            
            IEnumerable<Object> enumerable = from x in AssetDatabase.LoadAllAssetsAtPath(assetPath)
                where x.GetType() == assetType
                select x;
            foreach (var obj in enumerable)
            {
                _newAssetPathMapDict.TryAdd(obj, "");
            }

            HashSet<string> subAssetPathHashSet = new HashSet<string>();
            outsideAssetPathHashSet = new HashSet<string>();
            
            if (!doProcess) return;
            
            foreach (Object item in enumerable)
            {
                string targetPath = System.IO.Path.Combine(newFolder, item.name) + $".{extension}";
                targetPath = AssetDatabase.GenerateUniqueAssetPath(targetPath.Replace('\\', '/'));
                if (assetOperation == ModelImportAssetOperation.Extract)
                {
                    string extracted = AssetDatabase.ExtractAsset(item,targetPath);
                    if (string.IsNullOrEmpty(extracted))
                    {
                        subAssetPathHashSet.Add(assetPath);
                    }
                }
                else
                {
                    Object newObject = Instantiate(item);
                    if (AssetDatabase.AssetPathExists(targetPath))
                    {
                        AssetDatabase.DeleteAsset(targetPath);
                    }
                    AssetDatabase.CreateAsset(newObject, targetPath);
                    
                }
                if(_newAssetPathMapDict.ContainsKey(item))
                {
                    _newAssetPathMapDict[item] = targetPath;
                }
                
                outsideAssetPathHashSet.Add(targetPath);
            }

            if (assetOperation == ModelImportAssetOperation.Extract)
            {
                foreach (string item2 in subAssetPathHashSet)
                {
                    AssetDatabase.WriteImportSettingsIfDirty(item2);
                    AssetDatabase.ImportAsset(item2, ImportAssetOptions.ForceUpdate);
                }
            }
        }

        private void MakeUpPrefab()
        {
            GameObject original = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            GameObject waitForAssemble = Instantiate(original);
            ReplaceChildReference(waitForAssemble.transform);
            string localPath = AssetDatabase.GenerateUniqueAssetPath(destPath + "/" + original.name + ".prefab");
            PrefabUtility.SaveAsPrefabAsset(waitForAssemble, localPath);
            PrefabUtility.LoadPrefabContents(localPath);
            // MFModelImportMatTransferEditor.FinishTransfer -= MakeUpPrefab;
        }

        private void ReplaceChildReference(Transform root)
        {
            int childCount = root.childCount;
            if (childCount == 0) return;
            // GameObject[] childs = new GameObject[childCount];
            for (int i = 0; i < childCount; i++)
            {
                GameObject child = root.GetChild(i).gameObject;
                if (child.TryGetComponent(out Renderer renderer))
                {
                    if (renderer is SkinnedMeshRenderer skinRenderer)
                    {
                        Material[] mats = skinRenderer.sharedMaterials;
                        if (mats != null)
                        {
                            for (int j = 0; j < mats.Length; j++)
                            {
                                mats[j] = SwitchReferenceAsset(mats[j]);
                            }
                            skinRenderer.sharedMaterials = mats;
                        }
                        if(skinRenderer.sharedMesh!=null)
                            skinRenderer.sharedMesh = SwitchReferenceAsset(skinRenderer.sharedMesh);
                    }
                    else if (renderer is MeshRenderer meshRenderer)
                    {
                        if (child.TryGetComponent(out MeshFilter meshFilter))
                        {
                            if(meshFilter.sharedMesh!=null)
                                meshFilter.sharedMesh = SwitchReferenceAsset(meshFilter.sharedMesh);
                        }
                        else
                        {
                            Debug.LogError($"Object {child.name} is an empty static mesh");
                        }
                        Material[] mats = meshRenderer.sharedMaterials;
                        if (mats != null)
                        {
                            for (int j = 0; j < mats.Length; j++)
                            {
                                mats[j] = SwitchReferenceAsset(mats[j]);
                            }
                            meshRenderer.sharedMaterials = mats;
                        }
                    }
                }

                if (child.TryGetComponent(out Animator ani))
                {
                    if(ani.avatar!=null)
                        ani.avatar = SwitchReferenceAsset(ani.avatar);
                }

                ReplaceChildReference(child.transform);
            }
        }
        void ExportCelCharacter()
        {
            ModelImporter importer = target as ModelImporter;
        }

        private T SwitchReferenceAsset<T>(T oldAsset) where T:Object
        {
            string oldPath = AssetDatabase.GetAssetPath(oldAsset);
            try
            {
                if (_newAssetPathMapDict.TryGetValue(oldAsset, out string mappedPath))
                {
                    if (!string.IsNullOrEmpty(mappedPath))
                    {
                        T newAsset =
                            AssetDatabase.LoadAssetAtPath<T>(mappedPath);
                        return newAsset;
                    }
                    else
                    {
                        return oldAsset;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            
            return oldAsset;
        }

        static void CreateSmoothNormal(string fbxPath, GameObject fbx)
        {
            GameObject source = GameObject.Instantiate(fbx);
            SkinnedMeshRenderer[] skinnedMeshRenderers = source.GetComponentsInChildren<SkinnedMeshRenderer>();
            MeshFilter[] meshFilters = source.GetComponentsInChildren<MeshFilter>();
            List<Mesh> originalMeshes = new List<Mesh>();
            foreach (var renderer in skinnedMeshRenderers)
            {
                originalMeshes.Add(renderer.sharedMesh);
            }
            foreach (var filter in meshFilters)
            {
                originalMeshes.Add(filter.sharedMesh);
            }
            
            List<Tuple<string, Object>> subAssets = new List<Tuple<string, Object>>();
            for (int i = 0; i < originalMeshes.Count; i++)
            {
                // subAssets.Add(new Tuple<string, Object>(originalMeshes[i].name, GenerateMesh(originalMeshes[i])));
            }
            // List<KeyValuePair<string, UnityEngine.Object>> targetSubAsset = new List<KeyValuePair<string, UnityEngine.Object>>();
            // for (int i = 0; i < sourceMeshes.Count; i++)
            //     targetSubAsset.Add(new KeyValuePair<string, UnityEngine.Object>(sourceMeshes[i].name, GenerateMesh(sourceMeshes[i])));
            //
            // GameObject mainAsset = PrefabUtility.SaveAsPrefabAsset(prefabSource, assetPath);
            // UECommon.CreateOrReplaceSubAsset(assetPath, targetSubAsset.ToArray());
            // Mesh[] meshes = AssetDatabase.LoadAllAssetRepresentationsAtPath(assetPath).ToArray(obj => (Mesh)obj);
            //
            // skinnedRenderers = mainAsset.GetComponentsInChildren<SkinnedMeshRenderer>();
            // for (int i = 0; i < skinnedRenderers.Length; i++)
            //     skinnedRenderers[i].sharedMesh = meshes[i];
            //
            // meshFilters = mainAsset.GetComponentsInChildren<MeshFilter>();
            // for (int i = 0; i < meshFilters.Length; i++)
            //     meshFilters[i].sharedMesh = meshes.Find(p => p.name == meshFilters[i].sharedMesh.name);
            // PrefabUtility.SavePrefabAsset(mainAsset);
            // GameObject.DestroyImmediate(prefabSource);
            
        }
        
        // static Mesh GenerateMesh(Mesh _src)
        // {
        //     Mesh target = _src.Copy();
        //     Vector3[] smoothNormals = GenerateSmoothNormals(target);
        //     target.SetTangents( smoothNormals.ToList(vec3=>new Vector4(vec3.x,vec3.y,vec3.z,0)));
        //     return target;
        // }

    }
}