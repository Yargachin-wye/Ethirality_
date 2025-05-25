using System;
using UnityEngine;

public class DeadBody : MonoBehaviour
{
    [SerializeField] private float durationTime = 1;
    [SerializeField] private float forceMagnitude = 1;

    private Color _curentColor;
    private float _timer = 0;
    private Rigidbody2D[] _childRb2d;
    private Vector2[] _childObjectsAwakePositions;
    private SpriteRenderer[] _childObjectsAwakeSprites;

    private void Awake()
    {
        _childRb2d = new Rigidbody2D[transform.childCount];
        _childObjectsAwakePositions = new Vector2[transform.childCount];
        _childObjectsAwakeSprites = new SpriteRenderer[transform.childCount];
        for (int i = 0; i < transform.childCount; i++)
        {
            var gobj = transform.GetChild(i).gameObject;
            _childRb2d[i] = gobj.GetComponent<Rigidbody2D>();
            _childObjectsAwakePositions[i] = gobj.transform.localPosition;
            _childObjectsAwakeSprites[i] = gobj.GetComponent<SpriteRenderer>();
        }
    }

    private void OnEnable()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            _childRb2d[i].transform.localPosition = _childObjectsAwakePositions[i];
            _childRb2d[i].transform.localRotation = new Quaternion(0, 0, 0, 0);
        }

        _timer = durationTime;
        _curentColor = Color.white;
        for (int i = 0; i < _childRb2d.Length; i++)
        {
            Vector2 direction = (_childRb2d[i].transform.position - transform.position).normalized;
            _childRb2d[i].AddForce(direction * forceMagnitude, ForceMode2D.Impulse);
        }
    }

    private void OnDisable()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            _childRb2d[i].angularVelocity = 0;
            _childRb2d[i].velocity = Vector2.zero;
            _childRb2d[i].transform.localPosition = _childRb2d[i].transform.localPosition;
        }
    }

    private void FixedUpdate()
    {
        _timer -= Time.fixedDeltaTime;
        _curentColor.a = _timer / durationTime;

        for (int i = 0; i < _childObjectsAwakeSprites.Length; i++)
        {
            _childObjectsAwakeSprites[i].color = _curentColor;
        }

        if (_timer <= 0) gameObject.SetActive(false);
    }
}