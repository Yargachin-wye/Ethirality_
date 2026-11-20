using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace UI.Editor
{
    // Diagnostic for the outline flood: dumps the font atlas as PNG plus a
    // report with the real material/mesh values, so the shader math can be
    // verified. Menu: Tools/Text FX/Diagnose Outline…
    public static class TextFxDiagnose
    {
        private const string OutDir = "Assets/Docs/TextFxDiagnose";

        [MenuItem("Tools/Text FX/Diagnose Outline…")]
        public static void Run()
        {
            var texts = Selection.gameObjects
                .Select(go => go.GetComponent<TMP_Text>())
                .Where(t => t != null)
                .ToArray();
            if (texts.Length == 0)
            {
                Debug.LogWarning("[TextFxDiagnose] Выделите в сцене объект с TMP_Text.");
                return;
            }

            if (!AssetDatabase.IsValidFolder("Assets/Docs"))
                AssetDatabase.CreateFolder("Assets", "Docs");
            if (!AssetDatabase.IsValidFolder(OutDir))
                AssetDatabase.CreateFolder("Assets/Docs", "TextFxDiagnose");

            var sb = new StringBuilder();
            foreach (var text in texts)
            {
                sb.AppendLine($"=== {text.name} ===");
                try { text.ForceMeshUpdate(); } catch { }

                var font = text.font;
                var mat = text.fontSharedMaterial ?? (font != null ? font.material : null);
                var canvas = text.canvas;
                var rect = text.rectTransform;

                sb.AppendLine($"font: {(font != null ? font.name : "null")}");
                if (font != null)
                    sb.AppendLine($"font: pointSize={font.faceInfo.pointSize} faceScale={font.faceInfo.scale} " +
                                  $"atlas={font.atlasWidth}x{font.atlasHeight} atlasTexture={(font.atlasTexture != null ? font.atlasTexture.name : "null")}");
                sb.AppendLine($"text: fontSize={text.fontSize} lossyScale={rect.lossyScale} " +
                              $"rect={rect.rect.size.x:0.##}x{rect.rect.size.y:0.##}");
                sb.AppendLine($"canvas: renderMode={canvas?.renderMode} scaleFactor={canvas?.scaleFactor}");

                if (mat != null)
                {
                    sb.AppendLine($"material: {mat.name} shader={mat.shader?.name}");
                    sb.AppendLine($"  OUTLINE_ON={mat.IsKeywordEnabled("OUTLINE_ON")}");
                    foreach (var p in new[]
                             {
                                 "_GradientScale", "_ScaleRatioA", "_ScaleRatioB", "_ScaleRatioC",
                                 "_WeightNormal", "_WeightBold", "_FaceDilate", "_OutlineWidth",
                                 "_OutlineSoftness", "_Sharpness", "_PerspectiveFilter", "_ScaleX", "_ScaleY",
                             })
                        sb.AppendLine($"  {p}={(mat.HasProperty(p) ? mat.GetFloat(p).ToString("0.###") : "N/A")}");
                    var atlasTex = mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") : null;
                    sb.AppendLine($"  _MainTex={(atlasTex != null ? atlasTex.name : "NULL")}");
                }

                var meshInfo = text.textInfo.meshInfo[0];
                if (meshInfo.uvs2 != null && meshInfo.uvs2.Length > 0)
                {
                    float minY = float.MaxValue, maxY = float.MinValue;
                    foreach (var v in meshInfo.uvs2) { minY = Mathf.Min(minY, v.y); maxY = Mathf.Max(maxY, v.y); }
                    sb.AppendLine($"mesh uvs2.y (shader texcoord1.y): min={minY:0.###} max={maxY:0.###} first={meshInfo.uvs2[0].y:0.###}");
                }

                if (font != null && font.atlasTexture != null)
                {
                    var readable = MakeReadable(font.atlasTexture);
                    if (readable != null)
                    {
                        File.WriteAllBytes($"{OutDir}/atlas_{Sanitize(font.name)}.png", readable.EncodeToPNG());
                        sb.AppendLine($"atlas readable: {readable.width}x{readable.height} format={readable.format}");
                        if (meshInfo.uvs0 != null && meshInfo.uvs0.Length >= 4)
                        {
                            float w = readable.width, h = readable.height;
                            var uv0 = meshInfo.uvs0[0];
                            var uv3 = meshInfo.uvs0[3];
                            sb.AppendLine("alpha profile (first glyph quad BL->TR diagonal, 17 samples):");
                            for (int i = 0; i <= 16; i++)
                            {
                                float t = i / 16f;
                                var uv = Vector2.Lerp(uv0, uv3, t);
                                float a = readable.GetPixel(Mathf.RoundToInt(uv.x * w), Mathf.RoundToInt(uv.y * h)).a;
                                sb.AppendLine($"  t={t:0.00} alpha={a:0.###}");
                            }
                            sb.AppendLine("corner alpha (quad corners):");
                            var corners = new[] { uv0, new Vector2(uv0.x, uv3.y), uv3, new Vector2(uv3.x, uv0.y) };
                            foreach (var c in corners)
                            {
                                float a = readable.GetPixel(Mathf.RoundToInt(c.x * w), Mathf.RoundToInt(c.y * h)).a;
                                sb.AppendLine($"  uv=({c.x:0.###},{c.y:0.###}) alpha={a:0.###}");
                            }
                        }
                    }
                }
                sb.AppendLine();
            }

            string report = $"{OutDir}/report.txt";
            File.WriteAllText(report, sb.ToString());
            AssetDatabase.Refresh();
            Debug.Log($"[TextFxDiagnose] Отчёт: {report}");
        }

        private static Texture2D MakeReadable(Texture2D src)
        {
            var rt = RenderTexture.GetTemporary(src.width, src.height, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(src, rt);
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var copy = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false);
            copy.ReadPixels(new Rect(0, 0, src.width, src.height), 0, 0);
            copy.Apply();
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
            return copy;
        }

        private static string Sanitize(string s)
        {
            foreach (var c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
            return s;
        }
    }
}