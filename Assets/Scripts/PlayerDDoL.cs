using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerDDoL : MonoBehaviour
{
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }
}
