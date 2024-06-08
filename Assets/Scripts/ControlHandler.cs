using System;
using System.IO;
using TMPro;
using UnityEngine;

public class ControlHandler : MonoBehaviour
{
    public static ControlHandler instance;

    delegate void ControlFunction(int value);

    public static int[] listeners = {-1,-1,-1,-1,-1,-1,-1,-1,-1,-1};

    private ControlFunction[] functions =
    {
        OnFloorX,
        OnFloorHeight,
        OnFloorZ,
        OnFloorRotationX,
        OnFloorRotationZ,
        OnGravity,
        OnGravityX,
        OnGravityZ,
        PressAllKeys,
        RemoveAllKeys
    };

    static void SaveListeners()
    {
        byte[] write = new byte[listeners.Length];
        for(int i = 0; i < listeners.Length; i++)
        {
            write[i] = (byte)(Math.Max(Math.Min(listeners[i],254),-1) + 1);
        }
        File.WriteAllBytes("bindings.bin", write);
    }

    static void LoadListeners()
    {
        if (File.Exists("bindings.bin"))
        {
            int idx = -1;
            foreach(byte i in File.ReadAllBytes("bindings.bin"))
            {
                idx++;
                listeners[idx] = i - 1;
            }
            GameManager.instance.inputFields[17].GetComponent<TMP_InputField>().text = listeners[0] == -1 ? "" : listeners[0].ToString();
        }
    }

    public void WriteToIndex(int idx, int value)
    {
        listeners[idx] = value;
        SaveListeners();
    }

    public void SubmitControl(int id, int value)
    {
        for(int i = 0; i < listeners.Length; i++)
        {
            if (listeners[i] == id)
            {
                functions[i](value);
            }
        }
    }

    public static float floorPosX = -0.5f;
    public static float floorPosY = -5f;
    public static float floorPosZ = 0f;
    public static float floorRotX = 0f;
    public static float floorRotZ = 0f;

    static void OnFloorX(int value)
    {
        floorPosX = (value - 64) / 3f - 0.5f;
    }

    static void OnFloorHeight(int value)
    {
        floorPosY = (value - 64) / 10f - 5f;
    }

    static void OnFloorZ(int value)
    {
        floorPosZ = (value - 64) / 3f;
    }

    static void OnFloorRotationX(int value)
    {
        if (value == 64)
        {
            floorRotX = 0;
        }
        else
        {
            floorRotX = value - 64;
            floorRotX = -(floorRotX + 0.5f) / 63.5f * 90;
        }
    }

    static void OnFloorRotationZ(int value)
    {
        if (value == 64)
        {
            floorRotZ = 0;
        }
        else
        {
            floorRotZ = value - 64;
            floorRotZ = -(floorRotZ + 0.5f) / 63.5f * 90;
        }
    }

    static void OnGravity(int value)
    {
        float result = value == 64 ? 9.80665f : (value / 127f) * 9.80665f * 2f - 9.80665f;
        GameManager.instance.configuration["gravity"] = result;
    }

    static void OnGravityX(int value)
    {
        float result = value == 64 ? 0 : (value / 127f - 0.5f) * 4f * 9.80665f;
        GameManager.instance.configuration["gravityX"] = result;
    }

    static void OnGravityZ(int value)
    {
        float result = value == 64 ? 0 : (value / 127f - 0.5f) * 4f * 9.80665f;
        GameManager.instance.configuration["gravityZ"] = result;
    }

    static void PressAllKeys(int value)
    {
        if(value == 0)
        {
            for (int i = 0; i < 128; i++)
            {
                Sound.Submit((0x80 | (i << 8) | (0 << 16)));
            }
        } else if(value == 64)
        {
            for (int i = 0; i < 128; i++)
            {
                Sound.Submit((0x90 | (i << 8) | (100 << 16)));
                NoteManager.Submit((byte)i, 0, 0);
            }
        }
    }

    static void RemoveAllKeys(int value)
    {
        if (value == 64)
        {
            NoteManager.ClearEntities();
        }
    }

    void Start()
    {
        if (instance != null)
        {
            Debug.LogWarning("Multiple ControlHandlers cannot exist!");
        }
        else
        {
            instance = this;
        }
        LoadListeners();
    }

    void Update()
    {
        
    }
}
