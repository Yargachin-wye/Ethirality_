using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Ethereality.EditorTools
{
    /// <summary>
    /// One-shot migration: Built-in RP -> URP (Renderer2D) plus 2D lighting rig.
    /// Idempotent — safe to re-run. Menu: Tools/Rendering/Run URP 2D Lighting Migration.
    /// </summary>
    public static class Urp2DLightingMigration
    {
        const string SettingsDir = "Assets/Settings/URP";
        const string RendererDataPath = SettingsDir + "/Renderer2D.asset";
        const string PipelineAssetPath = SettingsDir + "/URP-2D.asset";
        const string VolumeProfilePath = SettingsDir + "/DarkAtmosphere.asset";
        const string ParticleMatPath = SettingsDir + "/URP-Particles-Unlit.mat";
        const string CameraPrefabPath = "Assets/Prefabs/Boot/Camera.prefab";

        static readonly Color GlobalLightColor = new Color(0.58f, 0.66f, 0.88f);
        const float GlobalLightIntensity = 0.35f;

        const string ShaderSpriteLit = "Universal Render Pipeline/2D/Sprite-Lit-Default";
        const string ShaderParticlesUnlit = "Universal Render Pipeline/Particles/Unlit";
        const string ShaderUnlit = "Universal Render Pipeline/Unlit";
        const string ShaderLit = "Universal Render Pipeline/Lit";

        [MenuItem("Tools/Rendering/Run URP 2D Lighting Migration")]
        public static void Run()
        {
            EnsureFolders();
            var rendererData = EnsureRendererData();
            var pipeline = EnsurePipelineAsset(rendererData);
            AssignPipeline(pipeline);
            ConvertMaterialAssets();
            var particleMat = EnsureParticleMaterial();
            FixRendererMaterialsEverywhere(particleMat);
            SetupCameraPrefab();
            SetupShadowCasters();
            SetupGameplayLights();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[URP2DMigration] Finished.");
        }

        // ---------- Pipeline ----------

        static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Settings"))
                AssetDatabase.CreateFolder("Assets", "Settings");
            if (!AssetDatabase.IsValidFolder(SettingsDir))
                AssetDatabase.CreateFolder("Assets/Settings", "URP");
        }

        static Renderer2DData EnsureRendererData()
        {
            var data = AssetDatabase.LoadAssetAtPath<Renderer2DData>(RendererDataPath);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<Renderer2DData>();
                AssetDatabase.CreateAsset(data, RendererDataPath);
                ResourceReloader.TryReloadAllNullIn(data, "Packages/com.unity.render-pipelines.universal");
                Debug.Log("[URP2DMigration] Created " + RendererDataPath);
            }

            // Sprites without an explicit material must be affected by Light 2D.
            var so = new SerializedObject(data);
            var p = so.FindProperty("m_DefaultMaterialType");
            if (p != null && p.intValue != 0)
            {
                p.intValue = 0; // Renderer2DDefaultMaterialType.Lit
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(data);
            }

            // No [Reload] attribute on this field — post-processing (Bloom) silently
            // does nothing without it, so assign it explicitly.
            var ppProp = so.FindProperty("m_PostProcessData");
            if (ppProp != null && ppProp.objectReferenceValue == null)
            {
                var ppd = AssetDatabase.LoadAssetAtPath<PostProcessData>(
                    "Packages/com.unity.render-pipelines.universal/Runtime/Data/PostProcessData.asset");
                if (ppd != null)
                {
                    ppProp.objectReferenceValue = ppd;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(data);
                    Debug.Log("[URP2DMigration] Assigned PostProcessData to Renderer2D.");
                }
            }
            return data;
        }

        static UniversalRenderPipelineAsset EnsurePipelineAsset(Renderer2DData rendererData)
        {
            var asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelineAssetPath);
            if (asset == null)
            {
                asset = UniversalRenderPipelineAsset.Create(rendererData);
                AssetDatabase.CreateAsset(asset, PipelineAssetPath);
                Debug.Log("[URP2DMigration] Created " + PipelineAssetPath);
            }
            return asset;
        }

        static void AssignPipeline(RenderPipelineAsset asset)
        {
            GraphicsSettings.renderPipelineAsset = asset;
            int prev = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = asset;
            }
            QualitySettings.SetQualityLevel(prev, false);
            Debug.Log("[URP2DMigration] URP asset assigned to Graphics + all quality levels.");
        }

        // ---------- Materials ----------

        static string MapBuiltinShaderName(string n)
        {
            if (n.StartsWith("Sprites/")) return ShaderSpriteLit;
            if (n.StartsWith("Particles/") || n.StartsWith("Legacy Shaders/Particles/")) return ShaderParticlesUnlit;
            if (n.StartsWith("Unlit/")) return ShaderUnlit;
            if (n == "Standard") return ShaderLit;
            return null;
        }

        static void ConvertMaterialAssets()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Material"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null || mat.shader == null) continue;
                var targetName = MapBuiltinShaderName(mat.shader.name);
                if (targetName == null) continue;
                var shader = Shader.Find(targetName);
                if (shader == null)
                {
                    Debug.LogWarning("[URP2DMigration] Shader not found: " + targetName);
                    continue;
                }
                var old = mat.shader.name;
                mat.shader = shader;
                EditorUtility.SetDirty(mat);
                Debug.Log($"[URP2DMigration] {path}: {old} -> {targetName}");
            }
        }

        static Material EnsureParticleMaterial()
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(ParticleMatPath);
            if (mat == null)
            {
                mat = new Material(Shader.Find(ShaderParticlesUnlit)) { name = "URP-Particles-Unlit" };
                mat.SetFloat("_Surface", 1f); // transparent
                AssetDatabase.CreateAsset(mat, ParticleMatPath);
                Debug.Log("[URP2DMigration] Created " + ParticleMatPath);
            }
            return mat;
        }

        static bool IsBuiltinOrNull(Material m)
            => m == null || (m.shader != null && MapBuiltinShaderName(m.shader.name) != null);

        static int FixRendererMaterialsIn(GameObject root, Material particleMat)
        {
            int fixedCount = 0;
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                if (r is not (ParticleSystemRenderer or LineRenderer or TrailRenderer)) continue;
                var mats = r.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (!IsBuiltinOrNull(mats[i])) continue;
                    mats[i] = particleMat;
                    changed = true;
                    fixedCount++;
                }
                if (changed) r.sharedMaterials = mats;
            }
            return fixedCount;
        }

        static void FixRendererMaterialsEverywhere(Material particleMat)
        {
            foreach (var path in AllAssetPaths("t:Prefab", "Assets/Prefabs"))
            {
                var root = PrefabUtility.LoadPrefabContents(path);
                if (FixRendererMaterialsIn(root, particleMat) > 0)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    Debug.Log("[URP2DMigration] Fixed particle/line materials in " + path);
                }
                PrefabUtility.UnloadPrefabContents(root);
            }

            foreach (var path in AllAssetPaths("t:Scene", "Assets/Scenes"))
            {
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                bool dirty = false;
                foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (r is not (ParticleSystemRenderer or LineRenderer or TrailRenderer)) continue;
                    var mats = r.sharedMaterials;
                    for (int i = 0; i < mats.Length; i++)
                    {
                        if (!IsBuiltinOrNull(mats[i])) continue;
                        mats[i] = particleMat;
                        dirty = true;
                    }
                    if (dirty) r.sharedMaterials = mats;
                }
                if (dirty)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                    Debug.Log("[URP2DMigration] Fixed particle/line materials in scene " + path);
                }
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        // ---------- Camera: post-processing, global light, volume ----------

        static GameObject GetOrCreateChild(Transform parent, string name)
        {
            var t = parent.Find(name);
            if (t != null) return t.gameObject;
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go;
        }

        static void SetupCameraPrefab()
        {
            var root = PrefabUtility.LoadPrefabContents(CameraPrefabPath);
            var cam = root.GetComponentInChildren<UnityEngine.Camera>(true);
            if (cam == null)
            {
                Debug.LogWarning("[URP2DMigration] No Camera found in " + CameraPrefabPath);
            }
            else
            {
                var data = cam.GetComponent<UniversalAdditionalCameraData>();
                if (data == null) data = cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
                data.renderPostProcessing = true;
                data.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
                data.volumeLayerMask = ~0; // Everything

                var lightGo = GetOrCreateChild(root.transform, "Global Light 2D");
                var light = lightGo.GetComponent<Light2D>() ?? lightGo.AddComponent<Light2D>();
                light.lightType = Light2D.LightType.Global;
                light.intensity = GlobalLightIntensity;
                light.color = GlobalLightColor;

                var volGo = GetOrCreateChild(root.transform, "Global Volume");
                volGo.layer = 0;
                var volume = volGo.GetComponent<Volume>() ?? volGo.AddComponent<Volume>();
                volume.isGlobal = true;
                volume.priority = 0f;
                volume.sharedProfile = EnsureVolumeProfile();
            }
            PrefabUtility.SaveAsPrefabAsset(root, CameraPrefabPath);
            PrefabUtility.UnloadPrefabContents(root);
            Debug.Log("[URP2DMigration] Camera prefab updated (post-processing, Global Light 2D, Global Volume).");
        }

        static T EnsureVolumeComponent<T>(VolumeProfile p) where T : VolumeComponent
            => p.TryGet<T>(out var c) ? c : p.Add<T>(true);

        static VolumeProfile EnsureVolumeProfile()
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                profile.name = "DarkAtmosphere";
                AssetDatabase.CreateAsset(profile, VolumeProfilePath);
                Debug.Log("[URP2DMigration] Created " + VolumeProfilePath);
            }

            var bloom = EnsureVolumeComponent<Bloom>(profile);
            bloom.intensity.Override(0.9f);
            bloom.threshold.Override(0.85f);
            bloom.scatter.Override(0.7f);

            var vignette = EnsureVolumeComponent<Vignette>(profile);
            vignette.intensity.Override(0.3f);
            vignette.smoothness.Override(0.5f);

            var colors = EnsureVolumeComponent<ColorAdjustments>(profile);
            colors.postExposure.Override(-0.2f);
            colors.saturation.Override(-10f);

            EditorUtility.SetDirty(profile);
            return profile;
        }

        // ---------- Shadow casters ----------

        static SpriteRenderer FindMainSpriteRenderer(GameObject root)
        {
            SpriteRenderer best = null;
            float bestArea = -1f;
            foreach (var sr in root.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (sr.sprite == null) continue;
                float area = sr.bounds.size.sqrMagnitude;
                if (area > bestArea) { bestArea = area; best = sr; }
            }
            return best;
        }

        static void AddShadowCaster(GameObject root)
        {
            var main = FindMainSpriteRenderer(root);
            if (main == null || main.GetComponent<ShadowCaster2D>() != null) return;
            var sc = main.gameObject.AddComponent<ShadowCaster2D>();
            sc.useRendererSilhouette = true;
            sc.selfShadows = false;
            sc.castsShadows = true;
        }

        static void SetupShadowCasters()
        {
            var paths = new List<string>();
            paths.AddRange(AllAssetPaths("t:Prefab", "Assets/Prefabs/Characters"));
            paths.AddRange(AllAssetPaths("t:Prefab", "Assets/Prefabs/Deads"));
            foreach (var path in paths)
            {
                var root = PrefabUtility.LoadPrefabContents(path);
                var before = root.GetComponentsInChildren<ShadowCaster2D>(true).Length;
                AddShadowCaster(root);
                if (root.GetComponentsInChildren<ShadowCaster2D>(true).Length > before)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    Debug.Log("[URP2DMigration] ShadowCaster2D added in " + path);
                }
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // ---------- Point lights ----------

        static void AddPointLight(GameObject root, string name, float radius, float intensity, Color color, bool shadows)
        {
            var existing = root.GetComponentsInChildren<Light2D>(true);
            foreach (var l in existing)
                if (l.gameObject.name == name) return;

            var go = new GameObject(name);
            go.transform.SetParent(root.transform, false);
            var light = go.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Point;
            light.pointLightInnerAngle = 360f;
            light.pointLightOuterAngle = 360f;
            light.pointLightInnerRadius = radius * 0.35f;
            light.pointLightOuterRadius = radius;
            light.intensity = intensity;
            light.color = color;
            light.falloffIntensity = 0.6f;
            light.shadowsEnabled = shadows;
            light.shadowIntensity = 0.8f;
        }

        static void SetupGameplayLights()
        {
            var warm = new Color(1f, 0.85f, 0.6f);
            var energy = new Color(0.55f, 0.8f, 1f);
            var jobs = new Dictionary<string, (Color color, float radius, float intensity, bool shadows)>
            {
                ["Assets/Prefabs/Characters/Player/Player.prefab"] = (new Color(1f, 0.9f, 0.7f), 6f, 1.25f, true),
                ["Assets/Prefabs/Characters/Mine.prefab"] = (new Color(1f, 0.25f, 0.2f), 2.5f, 1.4f, true),
                ["Assets/Prefabs/Characters/GameOverKey.prefab"] = (new Color(1f, 0.2f, 0.15f), 4f, 1.2f, true),
                ["Assets/Prefabs/Characters/LevelUp.prefab"] = (new Color(0.4f, 1f, 0.5f), 3f, 1.0f, true),
                ["Assets/Prefabs/Projectiles/Arrow.prefab"] = (warm, 2.2f, 1.5f, false),
                ["Assets/Prefabs/Projectiles/ArrowMini.prefab"] = (warm, 1.8f, 1.4f, false),
                ["Assets/Prefabs/Projectiles/DefaultProjectile.prefab"] = (energy, 2.2f, 1.5f, false),
            };
            foreach (var path in AllAssetPaths("t:Prefab", "Assets/Prefabs/Improvements"))
                jobs[path] = (energy, 2f, 1.1f, false);

            foreach (var (path, j) in jobs)
            {
                if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
                {
                    Debug.LogWarning("[URP2DMigration] Prefab not found, skipped: " + path);
                    continue;
                }
                var root = PrefabUtility.LoadPrefabContents(path);
                AddPointLight(root, "Light 2D", j.radius, j.intensity, j.color, j.shadows);
                PrefabUtility.SaveAsPrefabAsset(root, path);
                PrefabUtility.UnloadPrefabContents(root);
                Debug.Log("[URP2DMigration] Light 2D added in " + path);
            }
        }

        // ---------- Helpers ----------

        static IEnumerable<string> AllAssetPaths(string type, params string[] folders)
        {
            var guids = folders is { Length: > 0 }
                ? AssetDatabase.FindAssets(type, folders)
                : AssetDatabase.FindAssets(type);
            foreach (var g in guids)
                yield return AssetDatabase.GUIDToAssetPath(g);
        }
    }
}
