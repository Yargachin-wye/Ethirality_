using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UI.Editor
{
    // Custom inspector for "Custom/Text/Shimmer":
    // effect mode popup + outline/underlay toggles (TMP keywords),
    // the rest of the properties are drawn by the default GUI.
    public class TextFxShaderGUI : ShaderGUI
    {
        private const string OutlineKw = "OUTLINE_ON";
        private const string UnderlayKw = "UNDERLAY_ON";
        private const string UnderlayInnerKw = "UNDERLAY_INNER";

        private static readonly string[] ModeLabels =
        {
            "0 — Без эффекта",
            "1 — Блик (shine sweep)",
            "2 — Перелив по тексту (палитра)",
            "3 — Аврора (палитра + блик)",
            "4 — Смена цвета (палитра)",
            "5 — Шумовой блик (noise sparkle)",
            "6 — Радужный шум (палитра + шум)",
            "7 — Плазма (палитра)",
        };

        // TMP-internal properties and the ones drawn in custom sections.
        private static readonly HashSet<string> Hidden = new HashSet<string>
        {
            "_EffectMode", "_FaceColor", "_FaceDilate",
            "_OutlineColor", "_OutlineWidth", "_OutlineSoftness",
            "_UnderlayColor", "_UnderlayOffsetX", "_UnderlayOffsetY", "_UnderlayDilate", "_UnderlaySoftness",
            "_WeightNormal", "_WeightBold", "_ShaderFlags",
            "_ScaleRatioA", "_ScaleRatioB", "_ScaleRatioC",
            "_TextureWidth", "_TextureHeight", "_GradientScale", "_ScaleX", "_ScaleY",
            "_PerspectiveFilter", "_Sharpness", "_VertexOffsetX", "_VertexOffsetY",
            "_ClipRect", "_MaskSoftnessX", "_MaskSoftnessY",
            "_StencilComp", "_Stencil", "_StencilOp", "_StencilWriteMask", "_StencilReadMask",
            "_CullMode", "_ColorMask",
        };

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            var mat = (Material)materialEditor.target;
            if (mat == null) return;

            EditorGUILayout.HelpBox(
                "Анимация идёт через _Time: в Scene view включите «Animated Materials», в Play mode работает всегда.",
                MessageType.Info);

            var mode = FindProperty("_EffectMode", properties);
            EditorGUI.BeginChangeCheck();
            int value = EditorGUILayout.Popup("Effect", Mathf.RoundToInt(mode.floatValue), ModeLabels);
            if (EditorGUI.EndChangeCheck())
            {
                materialEditor.RegisterPropertyChangeUndo("Change Effect Mode");
                mode.floatValue = value;
            }
            EditorGUILayout.Space(4);

            DrawFace(materialEditor, properties);
            DrawOutline(materialEditor, mat, properties);
            DrawUnderlay(materialEditor, mat, properties);

            foreach (var p in properties)
            {
                if (Hidden.Contains(p.name)) continue;
                if (p.name == "_Intensity")
                    EditorGUILayout.LabelField("Text FX", EditorStyles.boldLabel);
                materialEditor.ShaderProperty(p, p.displayName);
            }
        }

        private static void DrawFace(MaterialEditor editor, MaterialProperty[] props)
        {
            editor.ShaderProperty(FindProperty("_FaceColor", props), "Face Color");
            editor.ShaderProperty(FindProperty("_FaceDilate", props), "Face Dilate");
        }

        private static void DrawOutline(MaterialEditor editor, Material mat, MaterialProperty[] props)
        {
            bool on = mat.IsKeywordEnabled(OutlineKw);
            EditorGUI.BeginChangeCheck();
            on = EditorGUILayout.Toggle("Outline", on);
            if (EditorGUI.EndChangeCheck())
            {
                editor.RegisterPropertyChangeUndo("Toggle Outline");
                if (on) mat.EnableKeyword(OutlineKw);
                else mat.DisableKeyword(OutlineKw);
            }
            if (!on) return;
            editor.ShaderProperty(FindProperty("_OutlineColor", props), "Outline Color");
            editor.ShaderProperty(FindProperty("_OutlineWidth", props), "Outline Thickness");
            editor.ShaderProperty(FindProperty("_OutlineSoftness", props), "Outline Softness");
        }

        private static void DrawUnderlay(MaterialEditor editor, Material mat, MaterialProperty[] props)
        {
            int mode = mat.IsKeywordEnabled(UnderlayInnerKw) ? 2 : mat.IsKeywordEnabled(UnderlayKw) ? 1 : 0;
            EditorGUI.BeginChangeCheck();
            mode = EditorGUILayout.Popup("Underlay", mode, new[] { "None", "Underlay", "Underlay Inner" });
            if (EditorGUI.EndChangeCheck())
            {
                editor.RegisterPropertyChangeUndo("Change Underlay");
                mat.DisableKeyword(UnderlayKw);
                mat.DisableKeyword(UnderlayInnerKw);
                if (mode == 1) mat.EnableKeyword(UnderlayKw);
                if (mode == 2) mat.EnableKeyword(UnderlayInnerKw);
            }
            if (mode == 0) return;
            editor.ShaderProperty(FindProperty("_UnderlayColor", props), "Border Color");
            editor.ShaderProperty(FindProperty("_UnderlayOffsetX", props), "Border OffsetX");
            editor.ShaderProperty(FindProperty("_UnderlayOffsetY", props), "Border OffsetY");
            editor.ShaderProperty(FindProperty("_UnderlayDilate", props), "Border Dilate");
            editor.ShaderProperty(FindProperty("_UnderlaySoftness", props), "Border Softness");
        }
    }
}