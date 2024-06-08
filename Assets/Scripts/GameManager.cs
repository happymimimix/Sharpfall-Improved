using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public Entity platform;
    public GameObject CenterCam;
    public GameObject container;
    public float3 startVelocity = new float3(0f, -15f, 0f);
    public float gravityY = -9.80665f;
    public float gravityX = 0f;
    public float gravityZ = 0f;
    public int solverIterations = 4;
    public int simulationType = 1;
    public int colorMode = 1;
    public int renderFPS = 60;
    public string selectedMidiPath = "N/A";
    public bool playingMidi = false;
    public Camera renderCamera;
    public Dictionary<string, object> configuration = new Dictionary<string, object>()
    {
        ["MaxBlocks"] = 2500,
        ["KNLPF"] = 1,
        ["gravity"] = -9.80665f,
        ["gravityX"] = 0f,
        ["gravityZ"] = 0f,
        ["spawnVelX"] = 0f,
        ["spawnVelY"] = -15f,
        ["spawnVelZ"] = 0f,
        ["SolverIter"] = 4,
        ["render"] = false,
        ["followDelta"] = false,
        ["simulationType"] = 1,
        ["ffmpegPreset"] = 2,
        ["ffmpegCRF"] = 17u,
        ["renderFPS"] = 60,
        ["renderResX"] = 1920,
        ["renderResY"] = 1080,
        ["colorMode"] = 1,
        ["colorLoop"] = false
    };

    public GameObject[] inputFields;

    void Start()
    {
        if (instance != null)
        {
            Debug.LogWarning("Multiple GameManagers cannot exist!");
        }
        else
        {
            instance = this;
        }
    }

    void Update()
    {
        solverIterations = (int)configuration["SolverIter"];
        gravityX = (float)configuration["gravityX"];
        gravityZ = (float)configuration["gravityZ"];
        float get = (float)configuration["gravity"];
        if (get != gravityY)
        {
            inputFields[5].GetComponent<TMP_InputField>().text = get.ToString();
        }
        gravityY = get;
        simulationType = (int)configuration["simulationType"];
        startVelocity = new float3((float)configuration["spawnVelX"], (float)configuration["spawnVelY"], (float)configuration["spawnVelZ"]);
        colorMode = (int)configuration["colorMode"];
        int target = (int)configuration["renderFPS"];
        if(target > 5f && target != renderFPS)
        {
            renderFPS = target;
            Application.targetFrameRate = target;
            Time.fixedDeltaTime = 1f / (float)target;
        }
    }
}
