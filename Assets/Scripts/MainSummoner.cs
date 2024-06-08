using System;
using System.IO;
using System.Xml.Linq;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.UI;

public struct Note
{
    public byte key;
    public double t;
    public ushort track;
}

public static class NoteManager
{
    public static List<Note> notes = new List<Note>();
    public static int[] keysHit = new int[128];
    public static int KNLPF = 1;
    public static bool firstrun = true;
    public static long totalSpawns = 0;
    public static int maxBlocks = 2500;
    public static long submitsThisFrame = 0;
    public static bool clearRequested = false;
    public static List<Entity> entities = new List<Entity>();
    public static Entity platform;
    public static void Submit(byte key, double t, ushort track)
    {
        if (keysHit[key] < KNLPF)
        {
            keysHit[key]++;
            if (submitsThisFrame < maxBlocks)
            {
                notes.Add(new Note
                {
                    key = key,
                    t = t,
                    track = track
                });
            }
            else
            {
                notes[(int)(submitsThisFrame % maxBlocks)] = new Note
                {
                    key = key,
                    t = t,
                    track = track
                };
            }
            submitsThisFrame++;
        }
    }
    public static void ClearEntities()
    {
        clearRequested = true;
    }
    public static void ResetKeysHit()
    {
        for(int i = 0; i < 128; i++)
        {
            keysHit[i] = 0;
        }
    }
}

public static class PFAColors
{
    public static bool Ready = false;
    public static Color[] trackColors;
    public static Color[] PFAConfig;
    public static void Init(int tracks)
    {
        PFAConfig = new Color[0];
        string path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Piano From Above\\Config.xml";
        if (File.Exists(path))
        {
            try
            {
                XDocument doc = XDocument.Parse(File.ReadAllText(path));
                var colors = doc.Element("PianoFromAbove").Element("Visual").Element("Colors");
                List<Color> colorsList = new List<Color>();
                foreach (var color in colors.Elements("Color"))
                {
                    colorsList.Add(new Color32(
                        Convert.ToByte(color.Attribute("R").Value),
                        Convert.ToByte(color.Attribute("G").Value),
                        Convert.ToByte(color.Attribute("B").Value),
                        255
                    ));
                }
                PFAConfig = colorsList.ToArray();
                colorsList.Clear();
            } catch(Exception e)
            {
                Debug.LogError("PFA Config error: " + e);
            }
        }
        Debug.Log("Loaded " + PFAConfig.Length + " colors.");
        trackColors = new Color[tracks];
        Unity.Mathematics.Random rnd = new Unity.Mathematics.Random((uint)DateTime.Now.Millisecond);
        Toggle temp = GameManager.instance.inputFields[13].GetComponent<Toggle>();
        temp.isOn = temp.isOn && PFAConfig.Length > 0;
        temp.interactable = PFAConfig.Length > 0;
        GameManager.instance.configuration["colorLoop"] = (bool)GameManager.instance.configuration["colorLoop"] && PFAConfig.Length > 0;
        GameManager.instance.inputFields[14].SetActive(PFAConfig.Length == 0);
        if (!(bool)GameManager.instance.configuration["colorLoop"] || PFAConfig.Length == 0) {
            for (int i = 0; i < tracks; i++)
            {
                if (i < PFAConfig.Length)
                {
                    trackColors[i] = PFAConfig[i];
                } else
                {
                    trackColors[i] = Color.HSVToRGB(rnd.NextFloat(0f, 1f), rnd.NextFloat(0f, 0.4f) + 0.6f, rnd.NextFloat(0f, 0.2f) + 0.8f);
                }
            }
        } else
        {
            for (int i = 0; i < tracks; i++)
            {
                trackColors[i] = PFAConfig[i % PFAConfig.Length];
            }
        }
        Ready = true;
    }
}

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
public unsafe partial struct MainSummoner : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var singleton = SystemAPI.GetSingleton<PrefabSystem.Prefabs>();
        var prefabs = singleton.Registry;
        singleton.Dependency.Complete();

        EntityManager _entitymanager = state.EntityManager;

        bool render = (bool)GameManager.instance.configuration["render"];
        bool dt = (bool)GameManager.instance.configuration["followDelta"];
        _entitymanager.World.GetExistingSystemManaged<FixedStepSimulationSystemGroup>().Timestep = render ? Time.fixedDeltaTime : (dt ? Mathf.Min(Mathf.Max(1.0f / (float)Application.targetFrameRate, Time.deltaTime), 1f/15f) : 1.0f / (float)Application.targetFrameRate);

        if (!prefabs.ContainsKey("Platform"))
        {
            //prefabs not registered yet, wait.
            return;
        }

        Entity prefab = prefabs["Note"];

        if (NoteManager.firstrun)
        {
            NoteManager.platform = _entitymanager.Instantiate(prefabs["Platform"]);
            NoteManager.firstrun = false;
        }

        //manage platform controls

        LocalTransform pt = _entitymanager.GetComponentData<LocalTransform>(NoteManager.platform);
        pt.Position = new float3(ControlHandler.floorPosX, ControlHandler.floorPosY, ControlHandler.floorPosZ);
        pt.Rotation = Quaternion.Euler(ControlHandler.floorRotX, 0f, ControlHandler.floorRotZ);
        _entitymanager.SetComponentData(NoteManager.platform, pt);

        int total = NoteManager.notes.Count - NoteManager.maxBlocks;
        float3 vel = GameManager.instance.startVelocity;
        if (NoteManager.clearRequested)
        {
            NoteManager.clearRequested = false;
            foreach (Entity i in NoteManager.entities)
            {
                _entitymanager.DestroyEntity(i);
            }
            NoteManager.entities.Clear();
            NoteManager.totalSpawns = 0;
        }
        Note[] safeCopy = new Note[NoteManager.notes.Count];
        NoteManager.notes.CopyTo(safeCopy);
        NoteManager.submitsThisFrame = 0;
        NoteManager.ResetKeysHit();
        NoteManager.notes.Clear();
        foreach (Note item in safeCopy) {
            if (total <= 0)
            {
                int calc = (int)(NoteManager.totalSpawns % NoteManager.maxBlocks);
                Entity e;
                bool append = false;
                if (calc >= NoteManager.entities.Count)
                {
                    e = _entitymanager.Instantiate(prefab);
                    _entitymanager.AddComponentData(e, new URPMaterialPropertyBaseColor { Value = new float4(0f, 0f, 0f, 1f) });
                    append = true;
                }
                else
                {
                    e = NoteManager.entities[calc];
                }
                LocalTransform t = _entitymanager.GetComponentData<LocalTransform>(e);
                PhysicsVelocity v = _entitymanager.GetComponentData<PhysicsVelocity>(e);
                /*
                pianofall-like spawning
                t.Position = new float3(key * 0.1f + (-6.5f), 7f, 0f);
                v.Linear = new float3(0f, -5f, 0f);
                */
                //then my precise spawning

                t.Position = new float3(item.key * 0.1f + (-6.5f), (float)(7d + -15d * item.t + (Physics.gravity.y / 2 * Mathf.Pow((float)item.t, 2))), 0f);
                v.Linear = new float3(vel.x, (float)(vel.y + Physics.gravity.y * item.t), vel.z);
                t.Rotation = quaternion.identity;
                v.Angular = float3.zero;

                switch (GameManager.instance.colorMode) {
                    case 0:
                        {
                            var n = item.key % 12;
                            Color nc = (n == 1 || n == 3 || n == 6 || n == 8 || n == 10) ? Color.black : Color.white;
                            _entitymanager.SetComponentData(e, new URPMaterialPropertyBaseColor { Value = new float4(nc.r, nc.g, nc.b, nc.a) });
                            break;
                        }
                    case 1:
                        {
                            Color nc = Color.HSVToRGB((float)item.key / 128f, 1f, 1f);
                            _entitymanager.SetComponentData(e, new URPMaterialPropertyBaseColor { Value = new float4(nc.r, nc.g, nc.b, nc.a) });
                            break;
                        }
                    case 2:
                        {
                            Color nc = PFAColors.trackColors[item.track];
                            _entitymanager.SetComponentData(e, new URPMaterialPropertyBaseColor { Value = new float4(nc.r, nc.g, nc.b, nc.a) });
                            break;
                        }
                }

                _entitymanager.SetComponentData(e, t);
                _entitymanager.SetComponentData(e, v);
                NoteManager.totalSpawns++;
                if (append)
                {
                    _entitymanager.SetEnabled(e, true);
                    NoteManager.entities.Add(e);
                }
            } else
            {
                total--;
            }
        }
    }
}