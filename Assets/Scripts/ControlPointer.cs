using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ControlPointer : MonoBehaviour
{
    public TextMeshProUGUI detectedID;
    public TextMeshProUGUI valueText;
    public Slider valueSlider;
    public RectTransform handle;

    public static int value = 64;
    public static int control = 0;
    public static int changeset = 0;
    int syncset = 0;

    public static void SubmitControl(int control, int value)
    {
        ControlPointer.control = control;
        ControlPointer.value = value;
        changeset++;
    }

    void Update()
    {
        if(changeset != syncset)
        {
            syncset = changeset;
            detectedID.text = "Detected ID: " + control;
            valueText.text = value.ToString();
            valueSlider.value = (float)value / 127f;
        }
        transform.position = new Vector3(handle.position.x, transform.position.y, transform.position.z);
    }
}
