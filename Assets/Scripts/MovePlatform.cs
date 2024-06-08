using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovePlatform : MonoBehaviour
{
    void Update()
    {
        transform.position = new Vector3(ControlHandler.floorPosX,-5f,0f);
    }
}
