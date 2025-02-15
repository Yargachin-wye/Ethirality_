using System;
using System.Collections;
using System.Collections.Generic;
using DefaultNamespace;
using UnityEngine;

public class EyesAnimator : MonoBehaviour
{
    [SerializeField] private EyeAnimator eye0;
    [SerializeField] private EyeAnimator eye1;
    [SerializeField] private EyeAnimator eye2;
    [SerializeField] private EyeAnimator eye3;
    [SerializeField] private EyeAnimator eye4;
    [SerializeField] private EyeAnimator eye5;
    [SerializeField] private EyeAnimator eye6;
    [SerializeField] private EyeAnimator eye7;

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