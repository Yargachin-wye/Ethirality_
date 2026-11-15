using UnityEngine;

namespace CharacterComponents.Moving
{
    public static class PixelExplosionFx
    {
        private static Material _material;
        private static Texture2D _texture;

        public static void Spawn(Vector3 position, float radius)
        {
            var root = new GameObject("PixelExplosionFx");
            root.transform.position = position;
            CreateLayer(root.transform, "Flash", 1, 0.12f, 0.2f, 0f, radius * 0.9f, 0.35f, 0.5f,
                Gradient(Color.white, new Color(1f, 0.95f, 0.5f), 0f), 1);
            CreateLayer(root.transform, "Fire", 45, 0.45f, 0.9f, radius * 1.4f, radius * 3.2f, 0.12f, 0.3f,
                Gradient(new Color(1f, 0.95f, 0.5f), new Color(1f, 0.25f, 0.05f), 0f), 2);
            CreateLayer(root.transform, "Sparks", 30, 0.5f, 1.1f, radius * 2.5f, radius * 5f, 0.06f, 0.14f,
                Gradient(Color.white, new Color(1f, 0.5f, 0.1f), 0f), 3);
            CreateLayer(root.transform, "Smoke", 18, 0.8f, 1.4f, radius * 0.4f, radius * 1.2f, 0.2f, 0.4f,
                Gradient(new Color(0.35f, 0.3f, 0.3f, 0.9f), new Color(0.1f, 0.1f, 0.1f), 0f), 0);
            Object.Destroy(root, 2f);
        }

        private static Gradient Gradient(Color start, Color end, float endAlpha)
        {
            var g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(start, 0f), new GradientColorKey(end, 0.6f), new GradientColorKey(end * 0.4f, 1f) },
                new[] { new GradientAlphaKey(start.a, 0f), new GradientAlphaKey(start.a, 0.6f), new GradientAlphaKey(endAlpha, 1f) });
            return g;
        }

        private static void CreateLayer(Transform parent, string name, int count, float lifeMin, float lifeMax,
            float speedMin, float speedMax, float sizeMin, float sizeMax, Gradient color, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.duration = 0.1f;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifeMin, lifeMax);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speedMin, speedMax);
            main.startSize = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
            main.startRotation = 0f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = count;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.1f;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            col.color = color;

            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 1f), new Keyframe(1f, 0.2f)));

            var vel = ps.limitVelocityOverLifetime;
            vel.enabled = true;
            vel.limit = 0f;
            vel.dampen = 0.08f;

            var r = go.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.sharedMaterial = GetMaterial();
            r.sortingOrder = 100 + order;

            ps.Play();
        }

        private static Material GetMaterial()
        {
            if (_material != null) return _material;
            _texture = new Texture2D(1, 1, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            _texture.SetPixel(0, 0, Color.white);
            _texture.Apply();
            _material = new Material(Shader.Find("Sprites/Default")) { mainTexture = _texture };
            return _material;
        }
    }
}
