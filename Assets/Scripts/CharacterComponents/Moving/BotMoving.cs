using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class BotMoving : MonoBehaviour
{
    [SerializeField] private float _rotationSpeed = 1;
    private Rigidbody2D rb2d;
    private void Awake()
    {
        rb2d = GetComponent<Rigidbody2D>();
    }
    private void FixedUpdate()
    {
        Vector3 difference = Vector3.up;
        difference.Normalize();
        float rotZ = Mathf.Atan2(difference.y, difference.x) * Mathf.Rad2Deg;
        Quaternion targetRotation = Quaternion.Euler(0f, 0f, rotZ - 90);

        rb2d.MoveRotation(Quaternion.Lerp(transform.rotation, targetRotation, _rotationSpeed * Time.fixedDeltaTime));
    }
}
