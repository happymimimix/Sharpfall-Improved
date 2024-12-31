using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Main : MonoBehaviour
{
    void Start()
    {
        if(!Sound.Init(1, null))
        {
            Sound.Init(2, "Microsoft GS Wavetable Synth");
        }
        MIDI.PreloadPath(Application.streamingAssetsPath + "\\Menu.mid");
        MIDIPlayer.Play(4f);
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
        Time.fixedDeltaTime = 1f / 60f;
    }
}
