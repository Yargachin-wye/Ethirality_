using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace CharacterComponents.CharacterStat
{
    [RequireComponent(typeof(Stats))]
    public class HitParticles : BaseCharacterComponent
    {
        public enum SpawnPoint
        {
            Entry,
            Exit,
            Center
        }

        public enum EmitDirection
        {
            AlongHit,
            TowardAttacker,
            SurfaceNormal
        }

        [Serializable]
        public class Burst
        {
            [Tooltip("ParticleSystem prefab. Particles must fly along local +X (for Cone shape set Shape > Rotation Y = 90). Simulation Space = World is recommended.")]
            public ParticleSystem prefab;
            public SpawnPoint spawnPoint = SpawnPoint.Entry;
            public EmitDirection direction = EmitDirection.TowardAttacker;
            [Tooltip("Random angle deviation (degrees) applied on each hit")]
            [Range(0f, 180f)] public float angleJitter = 10f;
            [Tooltip("Extra rotation if the prefab does not emit along +X")]
            public float rotationOffset;
            [Tooltip("Offset from the spawn point along the emit direction")]
            public float surfaceOffset;
            [Tooltip("Extra particles for each damage point above 1")]
            public int extraParticlesPerDamage;
            [Tooltip("Attach the effect to the character so it moves with it")]
            public bool followCharacter;

            [NonSerialized] public readonly List<ParticleSystem> Pool = new();
        }

        [SerializeField] private Burst[] bursts = Array.Empty<Burst>();
        [Tooltip("Used for damage without a source (dead zone, poison etc.)")]
        [SerializeField] private bool playWithoutSource = true;
        [SerializeField, HideInInspector] private Stats stats;
        [SerializeField, HideInInspector] private Collider2D hitCollider;

        private void OnValidate() => Validate();

        protected override void Validate()
        {
            base.Validate();
            if (stats == null) stats = GetComponent<Stats>();
            if (hitCollider == null) hitCollider = GetComponent<Collider2D>();
        }

        protected override void Awake()
        {
            base.Awake();
            stats.OnHitAction += OnHit;
        }

        private void OnDestroy()
        {
            if (stats != null) stats.OnHitAction -= OnHit;
        }

        public override void Init()
        {
        }

        private void OnHit(HitInfo hit)
        {
            if (!hit.HasSource && !playWithoutSource) return;

            Vector2 center = hitCollider ? hitCollider.bounds.center : transform.position;
            Vector2 dir = hit.HasDirection ? hit.Direction : Random.insideUnitCircle.normalized;
            if (dir == Vector2.zero) dir = Vector2.up;

            Vector2 entry = center, exit = center;
            if (hitCollider)
            {
                float reach = hitCollider.bounds.extents.magnitude * 2f;
                Vector2 origin = hit.HasSource ? hit.SourcePosition : center;
                entry = hitCollider.ClosestPoint(origin - dir * reach);
                exit = hitCollider.ClosestPoint(entry + dir * reach);
            }

            foreach (var burst in bursts)
            {
                if (burst.prefab == null) continue;

                Vector2 pos = burst.spawnPoint switch
                {
                    SpawnPoint.Entry => entry,
                    SpawnPoint.Exit => exit,
                    _ => center
                };

                Vector2 emitDir = burst.direction switch
                {
                    EmitDirection.AlongHit => dir,
                    EmitDirection.TowardAttacker => -dir,
                    _ => SurfaceNormal(pos, center, burst.spawnPoint == SpawnPoint.Exit ? dir : -dir)
                };

                float angle = Mathf.Atan2(emitDir.y, emitDir.x) * Mathf.Rad2Deg
                              + Random.Range(-burst.angleJitter, burst.angleJitter)
                              + burst.rotationOffset;

                Play(burst, pos + emitDir * burst.surfaceOffset, Quaternion.Euler(0f, 0f, angle), hit.Damage);
            }
        }

        private static Vector2 SurfaceNormal(Vector2 pos, Vector2 center, Vector2 fallback)
        {
            Vector2 n = pos - center;
            return n.sqrMagnitude > 0.0001f ? n.normalized : fallback;
        }

        private void Play(Burst burst, Vector2 position, Quaternion rotation, int damage)
        {
            ParticleSystem ps = GetFromPool(burst);
            Transform t = ps.transform;
            t.SetParent(burst.followCharacter ? transform : null, true);
            t.SetPositionAndRotation(new Vector3(position.x, position.y, transform.position.z), rotation);
            ps.gameObject.SetActive(true);
            ps.Clear(true);
            ps.Play(true);
            if (burst.extraParticlesPerDamage > 0 && damage > 1)
                ps.Emit(burst.extraParticlesPerDamage * (damage - 1));
        }

        private static ParticleSystem GetFromPool(Burst burst)
        {
            burst.Pool.RemoveAll(p => p == null);
            foreach (var ps in burst.Pool)
                if (!ps.gameObject.activeInHierarchy || !ps.IsAlive(true))
                    return ps;

            ParticleSystem instance = Instantiate(burst.prefab);
            var main = instance.main;
            main.playOnAwake = false;
            burst.Pool.Add(instance);
            return instance;
        }

        [ContextMenu("Test Hit (random side)")]
        private void TestHit()
        {
            if (!Application.isPlaying) return;
            Vector2 dir = Random.insideUnitCircle.normalized;
            OnHit(new HitInfo(1, (Vector2)transform.position - dir * 3f, dir));
        }
    }
}
