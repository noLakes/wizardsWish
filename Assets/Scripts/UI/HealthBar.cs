using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    private Slider slider;
    public Gradient gradient;
    private Image fill;

    private void Awake() 
    {
        slider = GetComponent<Slider>();
        fill = transform.Find("Fill").gameObject.GetComponent<Image>();
    }

    public void SetMaxHealth(int health)
    {
        slider.maxValue = health;
        fill.color = gradient.Evaluate(slider.normalizedValue);
    }

    public void SetHealth(int health)
    {
        slider.value = health;
        fill.color = gradient.Evaluate(slider.normalizedValue);
    }
}
