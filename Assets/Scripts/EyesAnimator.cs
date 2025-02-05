using System;
using System.Collections;
using System.Collections.Generic;
using DefaultNamespace;
using UnityEngine;

public class EyesAnimator : MonoBehaviour
{
    [SerializeField] private EyeAnimator eye0;

    private void OnValidate()
    {
        eye0.Validate();
    }

    private void Start()
    {
        eye0.Play(EyeAnimator.Animations.BlinkEye);
    }

    private void FixedUpdate()
    {
        eye0.Update(Time.fixedDeltaTime);
    }
}