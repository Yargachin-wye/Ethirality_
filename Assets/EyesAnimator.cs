using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EyesAnimator : MonoBehaviour
{
    public const string OpenEye = "open_eye";
    public const string CloseEye = "close_eye";
    public const string BlinkEye = "blink_eye";
    
    [SerializeField] private float transitionDuration;
    
    [Space]
    [SerializeField] private Animation eyes0Animator;
    [SerializeField] private Animation eyes1Animator;
    [SerializeField] private Animation eyes2Animator;
    [SerializeField] private Animation eyes3Animator;
    [SerializeField] private Animation eyes4Animator;
    [SerializeField] private Animation eyes5Animator;
    [SerializeField] private Animation eyes6Animator;
    [SerializeField] private Animation eyes7Animator;
    [SerializeField] private Animation eyes8Animator;

    [SerializeField, HideInInspector] private Animation[] animators;
    

    private void Start()
    {
        animators = new Animation[9];
        animators[0] = eyes0Animator;
        animators[1] = eyes1Animator;
        animators[2] = eyes2Animator;
        animators[3] = eyes3Animator;
        animators[4] = eyes4Animator;
        animators[5] = eyes5Animator;
        animators[6] = eyes6Animator;
        animators[7] = eyes7Animator;
        animators[8] = eyes8Animator;
        
        PlayAnimation(eyes0Animator, BlinkEye, WrapMode.Loop);
    }

    public void PlayAnimation(Animation animatior, string animationName, WrapMode wrapMode = WrapMode.Once)
    {
        animatior[animationName].wrapMode = wrapMode;
        animatior.CrossFade(animationName, transitionDuration);
    }

    private void Update()
    {
    }
}