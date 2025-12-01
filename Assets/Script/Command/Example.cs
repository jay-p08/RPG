using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TMPro;

public class Example : MonoBehaviour
{
    public TMP_InputField mainInputField;

    // Activate the main input field when the Scene starts.
    void Start()
    {
        mainInputField.ActivateInputField();
    }
}