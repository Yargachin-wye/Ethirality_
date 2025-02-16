using System;
using CharacterComponents;
using CharacterComponents.Animations;
using Definitions;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(Rigidbody2D))]
public class Projectile : MonoBehaviour
{
    [SerializeField, HideInInspector] private Rigidbody2D rb2D;
    private Fraction _fraction;

    private ProjectileDefinition _projectileDefinition;
    private GameObject _owner;
    private GameObject _trigger;
    private FixedJoint2D joint;

    [SerializeField] private Rope2D rope2D;

    private float _timer;
    private float _attachedForceTimer;
    private bool _inited = false;
    private bool _isAttached;
    private bool _isForceOnAttached = false;
    private bool _isJoined = false;
    private bool _isTrigered = false;

    public void Init(ProjectileDefinition projectileDefinition)
    {
        _projectileDefinition = projectileDefinition;
        rb2D.bodyType = _projectileDefinition.RigidbodyType2D;
        rb2D.gravityScale = _projectileDefinition.GravityScale;
        _timer = _projectileDefinition.LifeDelay;
    }

    private void FixedUpdate()
    {
        if (!_inited) return;
        if (_timer <= 0)
        {
            gameObject.SetActive(false);
            return;
        }

        _timer -= Time.fixedDeltaTime;

        if (!_isForceOnAttached) return;
        if (_attachedForceTimer <= 0 && !_isAttached)
        {
            _isAttached = true;
            Rigidbody2D rb2Down = _owner.GetComponent<Rigidbody2D>();
            Rigidbody2D rb2Dtr = _trigger.GetComponent<Rigidbody2D>();
            if (rb2Down != null)
                rb2Down.AddForce(
                    (_owner.transform.position - _trigger.transform.position).normalized *
                    _projectileDefinition.TargetForce,
                    ForceMode2D.Impulse);
            if (rb2Dtr != null)
                rb2Dtr.AddForce(
                    (_trigger.transform.position - _owner.transform.position).normalized *
                    _projectileDefinition.TargetForce,
                    ForceMode2D.Impulse);
        }

        _attachedForceTimer -= Time.fixedDeltaTime;
    }

    private void OnDrawGizmos()
    {
        if (!_inited) return;
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, _owner.transform.position);
    }

    private void OnValidate()
    {
        if (rb2D == null) rb2D = GetComponent<Rigidbody2D>();
    }

    private void OnDisable()
    {
        if (_isJoined)
        {
            _isJoined = false;
            Destroy(joint);
        }
    }

    public void Shoot(Vector2 direction, float speed, GameObject owner, Fraction fraction)
    {
        _inited = true;
        _owner = owner;
        _fraction = fraction;
        _isTrigered = false;

        if (rb2D.bodyType == RigidbodyType2D.Dynamic)
        {
            float angle = -Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(new Vector3(0, 0, angle + 90));
            rb2D.AddForce(speed * _projectileDefinition.SpeedMultiply * direction.normalized, ForceMode2D.Impulse);
        }

        if (_projectileDefinition.Recoil != 0)
        {
            LumpMeatMovable lumpMeatMovable = owner.GetComponent<LumpMeatMovable>();
            if (lumpMeatMovable != null) lumpMeatMovable.Dash(direction, _projectileDefinition.Recoil);
        }

        if (!_projectileDefinition.HasRope)
        {
            //rope2D.Off();
        }
        else
        {
            rope2D.Set(_owner.transform);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_isTrigered) return;
        Character character = other.GetComponent<Character>();
        if (character == null ||
            character.characterDefinition.Fraction == _fraction &&
            _fraction != Fraction.All)
        {
            return;
        }

        if (_projectileDefinition.IsDestroyOnTrigger)
        {
            gameObject.SetActive(false);
            return;
        }


        _isTrigered = true;
        _isAttached = false;
        _isForceOnAttached = false;
        _trigger = other.gameObject;

        if (_projectileDefinition.IsAttachedOnTrigger && !_isJoined)
        {
            if (other.GetComponent<Rigidbody2D>() != null)
            {
                joint = gameObject.AddComponent<FixedJoint2D>();
                _isJoined = true;

                joint.connectedBody = other.GetComponent<Rigidbody2D>();
                joint.autoConfigureConnectedAnchor = false;
            }
        }

        if (_projectileDefinition.TargetForce != 0 ||
            _projectileDefinition.OwnerForce != 0)
        {
            _isForceOnAttached = true;
            _attachedForceTimer = _projectileDefinition.AttachedForceDelay;
        }
    }
}