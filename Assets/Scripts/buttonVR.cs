using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class buttonVR : MonoBehaviour
{
    public GameObject button;
    public UnityEvent onPress;
    public UnityEvent onRelease;

    private Vector3 buttonPos;
    GameObject presser;
    bool isPressed;

    void Start()
    {
        buttonPos = button.transform.localPosition;
        isPressed = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isPressed)
        {
            //button.transform.localPosition = new Vector3(buttonPos.x, buttonPos.y - 0.03f, buttonPos.z);
            presser = other.gameObject;
            Debug.Log($"{gameObject.name} button pressed");
            onPress.Invoke(); // 호출된 메서드
            isPressed = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject == presser)
        {
            //button.transform.localPosition = buttonPos;
            //button.transform.localPosition = new Vector3(0, 0.07f, 0);
            Debug.Log($"{gameObject.name} button released");
            onRelease.Invoke(); // 호출된 메서드
            isPressed = false;
        }
    }
}
