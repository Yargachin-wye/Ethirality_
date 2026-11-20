using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UI.Editor
{
    // One style configuration for "Custom/Text/Shimmer".
    public sealed class TextFxPreset
    {
        public string name;
        public string description;
        public int mode;
        public float speed = 0.5f;
        public float tiling = 1f;
        public float intensity = 1f;
        public float angle = 0f;
        public float hueShift = 0f;
        public int colorCount = 4;
        public Color fxColor1 = new Color(1f, 0.25f, 0.25f);
        public Color fxColor2 = new Color(1f, 0.85f, 0.2f);
        public Color fxColor3 = new Color(0.3f, 1f, 0.4f);
        public Color fxColor4 = new Color(0.35f, 0.5f, 1f);
        public float sweepWidth = 0.25f;
        public float sweepSoftness = 2f;
        public float sweepBands = 2f;
        public Color sweepColor = Color.white;
        public float noiseScale = 4f;
        public float noiseOctaves = 3f;
        public float noiseContrast = 2f;
        public bool outlineOn;
        public float outlineWidth = 0.12f;
        public float outlineSoftness = 0.15f;
        public Color outlineColor = new Color(0.15f, 0.08f, 0f, 0.85f);

        public void Apply(Material mat)
        {
            mat.SetFloat("_EffectMode", mode);
            mat.SetFloat("_Speed", speed);
            mat.SetFloat("_Tiling", tiling);
            mat.SetFloat("_Intensity", intensity);
            mat.SetFloat("_FxAngle", angle);
            mat.SetFloat("_HueShift", hueShift);
            mat.SetFloat("_FxColorCount", colorCount);
            mat.SetColor("_FxColor1", fxColor1);
            mat.SetColor("_FxColor2", fxColor2);
            mat.SetColor("_FxColor3", fxColor3);
            mat.SetColor("_FxColor4", fxColor4);
            mat.SetFloat("_SweepWidth", sweepWidth);
            mat.SetFloat("_SweepSoftness", sweepSoftness);
            mat.SetFloat("_SweepBands", sweepBands);
            mat.SetColor("_SweepColor", sweepColor);
            mat.SetFloat("_NoiseScale", noiseScale);
            mat.SetFloat("_NoiseOctaves", noiseOctaves);
            mat.SetFloat("_NoiseContrast", noiseContrast);
            if (outlineOn)
            {
                mat.EnableKeyword("OUTLINE_ON");
                mat.SetFloat("_OutlineWidth", outlineWidth);
                mat.SetFloat("_OutlineSoftness", outlineSoftness);
                mat.SetColor("_OutlineColor", outlineColor);
            }
            else
            {
                mat.DisableKeyword("OUTLINE_ON");
            }
            mat.DisableKeyword("UNDERLAY_ON");
            mat.DisableKeyword("UNDERLAY_INNER");
        }
    }

    // Tools/Text FX: apply shimmer presets to TMP_Text or create material assets.
    // The created material is a copy of the text's current material (font atlas
    // and SDF parameters are preserved), so it works with any TMP_FontAsset.
    public static class TextFxTools
    {
        public const string ShaderName = "Custom/Text/Shimmer";
        public const string MaterialsDir = "Assets/Shaders/Materials/TextFx";

        private const string LastPresetKey = "Ethereality.TextFx.LastPreset";

        public static readonly TextFxPreset[] Presets =
        {
            new TextFxPreset
            {
                name = "Радужный перелив",
                description = "Цвета текут вдоль текста",
                mode = 2, speed = 0.6f, tiling = 1f,
            },
            new TextFxPreset
            {
                name = "Аврора",
                description = "Перелив + бегущий яркий блик",
                mode = 3, speed = 0.45f, tiling = 0.8f,
                fxColor1 = new Color(0.3f, 0.9f, 1f),
                fxColor2 = new Color(0.4f, 0.5f, 1f),
                fxColor3 = new Color(0.75f, 0.35f, 1f),
                fxColor4 = new Color(1f, 0.4f, 0.8f),
                sweepBands = 2f, sweepWidth = 0.3f,
                sweepColor = new Color(1.8f, 1.8f, 1.8f, 1f),
            },
            new TextFxPreset
            {
                name = "Золото",
                description = "Классический золотой блик с тёмной обводкой",
                mode = 1, speed = 0.8f, sweepBands = 1f, sweepWidth = 0.22f, sweepSoftness = 3f,
                sweepColor = new Color(1.9f, 1.55f, 0.7f, 1f),
                outlineOn = true, outlineWidth = 0.12f, outlineSoftness = 0.15f,
                outlineColor = new Color(0.15f, 0.08f, 0f, 0.85f),
            },
            new TextFxPreset
            {
                name = "Ледяное сияние",
                description = "Медленная смена холодных оттенков",
                mode = 4, speed = 0.12f,
                fxColor1 = new Color(0.75f, 0.95f, 1f),
                fxColor2 = new Color(0.5f, 0.75f, 1f),
                fxColor3 = new Color(0.3f, 0.55f, 0.95f),
                fxColor4 = new Color(0.6f, 0.85f, 1f),
            },
            new TextFxPreset
            {
                name = "Неон",
                description = "Два быстрых розово-фиолетовых блика",
                mode = 1, speed = 1.2f, sweepBands = 2f, sweepWidth = 0.18f, sweepSoftness = 4f,
                sweepColor = new Color(2.2f, 0.4f, 2f, 1f),
                outlineOn = true, outlineWidth = 0.1f, outlineSoftness = 0.1f,
                outlineColor = new Color(0.2f, 0f, 0.25f, 0.9f),
            },
            new TextFxPreset
            {
                name = "Закат",
                description = "Тёплые тона, текущие по тексту",
                mode = 2, speed = 0.35f, tiling = 0.7f,
                fxColor1 = new Color(1f, 0.9f, 0.5f),
                fxColor2 = new Color(1f, 0.6f, 0.25f),
                fxColor3 = new Color(1f, 0.35f, 0.3f),
                fxColor4 = new Color(0.8f, 0.25f, 0.55f),
            },
            new TextFxPreset
            {
                name = "Шумовая искра",
                description = "Мерцающие шумовые блики по тексту",
                mode = 5, speed = 0.9f, noiseScale = 5f, noiseOctaves = 3f, noiseContrast = 2.5f,
                sweepColor = new Color(2.2f, 2.1f, 1.7f, 1f),
            },
            new TextFxPreset
            {
                name = "Радужный шум",
                description = "Переливающиеся шумовые пятна цвета",
                mode = 6, speed = 0.6f, noiseScale = 3.5f, noiseOctaves = 3f,
                tiling = 1.2f,
            },
            new TextFxPreset
            {
                name = "Плазма",
                description = "Текучие волны по палитре",
                mode = 7, speed = 0.8f, noiseScale = 2.5f, noiseOctaves = 2f,
                tiling = 1f,
                fxColor1 = new Color(0.2f, 0.55f, 1f),
                fxColor2 = new Color(1f, 0.25f, 0.6f),
                fxColor3 = new Color(1f, 0.7f, 0.2f),
                fxColor4 = new Color(0.25f, 1f, 0.6f),
            },
        };

        public static int LastPresetIndex
        {
            get => EditorPrefs.GetInt(LastPresetKey, 1);
            set => EditorPrefs.SetInt(LastPresetKey, value);
        }

        public static TextFxPreset LastPreset => Presets[Mathf.Clamp(LastPresetIndex, 0, Presets.Length - 1)];

        [MenuItem("Tools/Text FX/Toolbox…")]
        private static void OpenToolbox()
        {
            TextFxToolboxWindow.Open();
        }

        [MenuItem("Tools/Text FX/Apply Last Preset to Selected")]
        private static void ApplyLastPreset()
        {
            int n = ApplyToSelection(LastPreset);
            if (n > 0)
                Debug.Log($"[TextFxTools] Пресет «{LastPreset.name}» применён к {n} текстам.");
        }

        [MenuItem("Tools/Text FX/Create Material Asset")]
        private static void CreateMaterialAssetMenuItem()
        {
            string path = CreateMaterialAsset(LastPreset, FindSourceMaterial());
            if (path != null)
                Debug.Log($"[TextFxTools] Материал создан: {path}");
        }

        [MenuItem("Tools/Text FX/Create Materials for All Presets")]
        public static void CreateAllPresetMaterials()
        {
            Material source = FindSourceMaterial();
            int created = 0;
            foreach (var preset in Presets)
            {
                if (CreateMaterialAsset(preset, source) != null) created++;
            }
            Debug.Log($"[TextFxTools] Создано/обновлено материалов: {created}/{Presets.Length}.");
        }

        // Creates a material for the preset, copying all matching properties
        // (atlas, SDF parameters) from the source material.
        public static Material CreateMaterial(TextFxPreset preset, Material source)
        {
            var shader = Shader.Find(ShaderName);
            if (shader == null)
            {
                Debug.LogError($"[TextFxTools] Шейдер '{ShaderName}' не найден. Проверьте Assets/Shaders/TextShimmer.shader.");
                return null;
            }
            // Note: `new Material(source)` creates a material variant (m_Parent)
            // whose shader cannot be changed and whose overrides are not always
            // serialized — build a standalone material instead.
            var mat = new Material(shader);
            if (source != null)
                mat.CopyMatchingPropertiesFromMaterial(source);
            // The atlas is often only an internal sub-asset reference on the font
            // material and may not survive the copy — take it from the font itself.
            if (mat.GetTexture("_MainTex") == null)
            {
                var font = FindSourceFont();
                if (font != null && font.atlasTexture != null)
                    mat.SetTexture("_MainTex", font.atlasTexture);
            }
            preset.Apply(mat);
            return mat;
        }

        public static int ApplyToSelection(TextFxPreset preset)
        {
            var texts = Selection.gameObjects
                .Select(go => go.GetComponent<TMP_Text>())
                .Where(t => t != null)
                .ToArray();
            if (texts.Length == 0)
            {
                Debug.LogWarning("[TextFxTools] Выделите в сцене хотя бы один объект с TMP_Text.");
                return 0;
            }
            Undo.RecordObjects(texts, "Apply Text FX");
            int applied = 0;
            foreach (var text in texts)
            {
                var source = text.fontSharedMaterial != null
                    ? text.fontSharedMaterial
                    : text.font != null ? text.font.material : null;
                var mat = CreateMaterial(preset, source);
                if (mat == null) continue;
                text.fontSharedMaterial = mat;
                if (PrefabUtility.IsPartOfPrefabInstance(text))
                    PrefabUtility.RecordPrefabInstancePropertyModifications(text);
                applied++;
            }
            if (applied > 0)
            {
                var scene = Selection.activeGameObject != null
                    ? Selection.activeGameObject.scene
                    : SceneManager.GetActiveScene();
                if (scene.IsValid()) EditorSceneManager.MarkSceneDirty(scene);
            }
            LastPresetIndex = Array.IndexOf(Presets, preset);
            return applied;
        }

        public static int ResetSelection()
        {
            var texts = Selection.gameObjects
                .Select(go => go.GetComponent<TMP_Text>())
                .Where(t => t != null && t.font != null)
                .ToArray();
            if (texts.Length == 0) return 0;
            Undo.RecordObjects(texts, "Reset Text FX");
            int reset = 0;
            foreach (var text in texts)
            {
                text.fontSharedMaterial = text.font.material;
                if (PrefabUtility.IsPartOfPrefabInstance(text))
                    PrefabUtility.RecordPrefabInstancePropertyModifications(text);
                reset++;
            }
            var scene = Selection.activeGameObject != null
                ? Selection.activeGameObject.scene
                : SceneManager.GetActiveScene();
            if (scene.IsValid()) EditorSceneManager.MarkSceneDirty(scene);
            return reset;
        }

        public static string CreateMaterialAsset(TextFxPreset preset, Material source)
        {
            var mat = CreateMaterial(preset, source);
            if (mat == null) return null;
            if (mat.GetTexture("_MainTex") == null)
                Debug.LogWarning("[TextFxTools] У материала нет _MainTex (атлас шрифта). " +
                                 "Выделите текст с нужным шрифтом или назначьте атлас вручную.");

            EnsureFolder(MaterialsDir);
            string path = $"{MaterialsDir}/TextFx_{preset.name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null && existing.parent == null)
            {
                // Standalone material: update in place so references stay valid.
                existing.shader = mat.shader;
                existing.CopyPropertiesFromMaterial(mat);
                existing.shaderKeywords = mat.shaderKeywords;
                UnityEngine.Object.DestroyImmediate(mat);
                AssetDatabase.SaveAssets();
                Selection.activeObject = existing;
                return path;
            }
            if (existing != null)
            {
                // Material variant: shader/properties cannot be persisted in
                // place — replace the asset (references to it are lost).
                Debug.LogWarning($"[TextFxTools] Материал '{path}' является вариантом (m_Parent) и не может быть " +
                                 "обновлён на месте — он будет заменён новым ассетом.");
                AssetDatabase.DeleteAsset(path);
            }
            path = AssetDatabase.GenerateUniqueAssetPath(path);
            AssetDatabase.CreateAsset(mat, path);
            AssetDatabase.SaveAssets();
            Selection.activeObject = mat;
            return path;
        }

        // Best candidate to copy atlas/SDF properties from: selected text,
        // otherwise the default TMP font.
        public static Material FindSourceMaterial()
        {
            var selected = Selection.gameObjects
                .Select(go => go.GetComponent<TMP_Text>())
                .FirstOrDefault(t => t != null);
            if (selected != null && selected.fontSharedMaterial != null)
                return selected.fontSharedMaterial;
            if (selected != null && selected.font != null)
                return selected.font.material;
            if (TMP_Settings.defaultFontAsset != null)
                return TMP_Settings.defaultFontAsset.material;
            return null;
        }

        // Font matching the source material (selected text first, else default).
        public static TMP_FontAsset FindSourceFont()
        {
            var selected = Selection.gameObjects
                .Select(go => go.GetComponent<TMP_Text>())
                .FirstOrDefault(t => t != null);
            if (selected != null && selected.font != null)
                return selected.font;
            return TMP_Settings.defaultFontAsset;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string leaf = Path.GetFileName(path);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(leaf)) return;
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }

    public class TextFxToolboxWindow : EditorWindow
    {
        private int presetIndex;

        public static void Open()
        {
            var window = GetWindow<TextFxToolboxWindow>(true, "Text FX Toolbox");
            window.presetIndex = TextFxTools.LastPresetIndex;
            window.minSize = new Vector2(430, 340);
        }

        private void OnGUI()
        {
            var presets = TextFxTools.Presets;
            presetIndex = EditorGUILayout.Popup("Пресет", presetIndex, presets.Select(p => p.name).ToArray());
            var preset = presets[presetIndex];
            EditorGUILayout.HelpBox($"{preset.description} — {ModeName(preset.mode)}", MessageType.None);

            EditorGUILayout.Space(6);

            var texts = Selection.gameObjects
                .Select(go => go.GetComponent<TMP_Text>())
                .Where(t => t != null)
                .ToArray();

            using (new EditorGUI.DisabledScope(texts.Length == 0))
            {
                if (GUILayout.Button($"Применить к выделенным ({texts.Length})"))
                {
                    int n = TextFxTools.ApplyToSelection(preset);
                    Debug.Log($"[TextFxTools] Пресет «{preset.name}» применён к {n} текстам.");
                }
            }
            if (GUILayout.Button("Создать материал-ассет…"))
            {
                string path = TextFxTools.CreateMaterialAsset(preset, TextFxTools.FindSourceMaterial());
                if (path != null)
                    Debug.Log($"[TextFxTools] Материал создан: {path}");
            }
            using (new EditorGUI.DisabledScope(texts.Length == 0))
            {
                if (GUILayout.Button("Сбросить выделенные (материал шрифта по умолчанию)"))
                {
                    int n = TextFxTools.ResetSelection();
                    Debug.Log($"[TextFxTools] Сброшено {n} текстов.");
                }
            }

            EditorGUILayout.Space(6);
            EditorGUILayout.HelpBox(
                "Пресет копирует свойства текущего материала текста (атлас шрифта и параметры SDF), " +
                "поэтому работает с любым TMP_FontAsset. Каждый текст получает собственный материал — " +
                "можно менять скорость/фазу у отдельных строк.\n\n" +
                "Анимация идёт через _Time: в Scene view включите «Animated Materials», в Play mode работает всегда.",
                MessageType.Info);
        }

        private static string ModeName(int mode)
        {
            if (mode == 0) return "без эффекта";
            if (mode == 1) return "блик";
            if (mode == 2) return "перелив по тексту";
            if (mode == 3) return "аврора";
            if (mode == 4) return "смена цвета";
            if (mode == 5) return "шумовой блик";
            if (mode == 6) return "радужный шум";
            if (mode == 7) return "плазма";
            return "без эффекта";
        }
    }
}