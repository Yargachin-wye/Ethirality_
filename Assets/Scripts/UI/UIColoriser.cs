using System.Collections;
using System.Collections.Generic;
using System.Net.Mime;
using UnityEngine;
using UnityEngine.UI;

public class UIColoriser : MonoBehaviour
{
    [SerializeField] private Image image;
    [SerializeField] private Gradient gradient;
    [SerializeField] private float speed;
    private float value = 0;

    void Update()
    {
        value += Time.deltaTime * speed;
        if (value > 1) value = 0;
        image.color = gradient.Evaluate(value);
    }
}