using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SimpleFileBrowser;

public class UIHandler : MonoBehaviour
{
    public bool IL2CPPBuild = false;
    public static string AudioLoadError = "Connection failed.";
    static bool selectingMidi = false;
    public GameObject[] res;
    public GameObject[] menus;
    public GameObject pauseMenu;
    public void OnClick(int id)
    {
        switch(id)
        {
            case 0:
                {
                    if (!selectingMidi)
                    {
                        FileBrowser.SetFilters(false, new FileBrowser.Filter("MIDI Files", ".mid", ".midi"));
                        FileBrowser.SetDefaultFilter(".mid");
                        selectingMidi = true;
                        StartCoroutine(ShowLoadDialogCoroutine());
                    }
                    break;
                }
            case 1:
                {
                    GameManager.instance.playingMidi = true;
                    MIDIPlayer.playing = false;
                    MIDIPlayer.loop = false;
                    MIDIClock.throttle = true;
                    MIDI.Cleanup();
                    if (Sound.engine != 0)
                    {
                        Sound.Reload();
                    }
                    NoteManager.ClearEntities();
                    MIDIClock.render = (bool)GameManager.instance.configuration["render"];
                    if (MIDIClock.render)
                    {
                        Time.maximumDeltaTime = Time.fixedDeltaTime;
                        StartCoroutine(RenderingSystem.RenderFFmpeg());
                    }
                    NoteManager.maxBlocks = (int)GameManager.instance.configuration["MaxBlocks"];
                    NoteManager.KNLPF = (int)GameManager.instance.configuration["KNLPF"];
                    GameObject CenterCam = GameManager.instance.CenterCam;
                    CenterCam.GetComponent<RotateCam>().enabled = false;
                    CenterCam.transform.rotation = Quaternion.Euler(0, 0, 0);
                    GameManager.instance.container.SetActive(false);
                    if (GameManager.instance.selectedMidiPath != "N/A")
                    {
                        MIDI.PreloadPath(GameManager.instance.selectedMidiPath);
                        MIDIPlayer.Play();
                        if ((int)GameManager.instance.configuration["colorMode"] == 2)
                        {
                            PFAColors.Init(MIDI.realTracks);
                        }
                    }
                }
                break;
            case 2:
                {
                    Sound.Close();
                    break;
                }
            case 3:
                {
                    AudioLoadError = "Unknown.";
                    int engine = res[6].GetComponent<TMP_Dropdown>().value + 1;
                    string winMMDev = "";
                    if(engine == 2)
                    {
                        TMP_Dropdown temp = res[7].GetComponent<TMP_Dropdown>();
                        winMMDev = temp.options[temp.value].text;
                    }
                    res[8].SetActive(!Sound.Init(engine, winMMDev));
                    res[8].GetComponent<TextMeshProUGUI>().text = "Connection Error: "+AudioLoadError;
                    break;
                }
            case 4:
                NoteManager.ClearEntities();
                break;
            case 5:
                {
                    int id2 = res[11].GetComponent<TMP_Dropdown>().value;
                    var result = InputManager.LoadDeviceID(id2);
                    if (result.Item1) {
                        res[12].GetComponent<TextMeshProUGUI>().text = "Listening to: " + WinMM.GetInputNameByID(id2);
                    } else
                    {
                        res[10].GetComponent<TextMeshProUGUI>().text = result.Item2;
                        res[12].GetComponent<TextMeshProUGUI>().text = "No input loaded.";
                    }
                    res[10].SetActive(!result.Item1);
                    res[13].GetComponent<Button>().interactable = result.Item1;
                    res[14].GetComponent<Button>().interactable = !result.Item1;
                    break;
                }
            case 6:
                {
                    InputManager.Disconnect();
                    res[12].GetComponent<TextMeshProUGUI>().text = "No input loaded.";
                    res[13].GetComponent<Button>().interactable = false;
                    res[14].GetComponent<Button>().interactable = true;
                    break;
                }
        }
    }
    public void ToggleMenu(string name)
    {
        foreach (GameObject i in menus)
        {
            i.SetActive(i.name == name);
        }
    }

    public void UpdateControlInput()
    {
        int id = res[15].GetComponent<TMP_Dropdown>().value;
        GameManager.instance.inputFields[17].GetComponent<TMP_InputField>().text = ControlHandler.listeners[id].ToString();
    }

    private void HandleIntegerInput(string config, string input, GameObject inputBox)
    {
        int parsed;
        if (!int.TryParse(input, out parsed))
        {
            inputBox.GetComponent<TMP_InputField>().text = GameManager.instance.configuration[config].ToString();
        }
        else
        {
            GameManager.instance.configuration[config] = parsed;
        }
    }

    private void HandleSingleInput(string config, string input, GameObject inputBox)
    {
        float parsed;
        if (!float.TryParse(input, out parsed))
        {
            inputBox.GetComponent<TMP_InputField>().text = GameManager.instance.configuration[config].ToString();
        }
        else
        {
            GameManager.instance.configuration[config] = parsed;
        }
    }

    public void WriteBlockLimit(string input) { HandleIntegerInput("MaxBlocks", input, GameManager.instance.inputFields[0]); }
    public void WriteKNLPF(string input) { HandleIntegerInput("KNLPF", input, GameManager.instance.inputFields[1]); }
    public void WriteSVX(string input) { HandleSingleInput("spawnVelX", input, GameManager.instance.inputFields[2]); }
    public void WriteSVY(string input) { HandleSingleInput("spawnVelY", input, GameManager.instance.inputFields[3]); }
    public void WriteSVZ(string input) { HandleSingleInput("spawnVelZ", input, GameManager.instance.inputFields[4]); }
    public void WriteGrav(string input) { HandleSingleInput("gravityY", input, GameManager.instance.inputFields[5]); }
    public void WriteIter(string input) { HandleIntegerInput("SolverIter", input, GameManager.instance.inputFields[6]); }
    public void WriteRender(bool input) { GameManager.instance.configuration["render"] = input;}
    public void WriteDT(bool input) { GameManager.instance.configuration["followDelta"] = GameManager.instance.inputFields[18].GetComponent<Toggle>().isOn; }
    public void WriteLoop(bool input) { GameManager.instance.configuration["colorLoop"] = input; if ((int)GameManager.instance.configuration["colorMode"] == 2) { PFAColors.Init(MIDI.realTracks); } }
    public void WriteFPS(string input) { HandleIntegerInput("renderFPS", input, GameManager.instance.inputFields[15]); }
    public void WriteControl(string input) {
        int test = -1;
        if(int.TryParse(input, out test))
        {
            ControlHandler.instance.WriteToIndex(res[15].GetComponent<TMP_Dropdown>().value, test);
        }
    }
    public void WriteColor(int input) {
        int val = GameManager.instance.inputFields[12].GetComponent<TMP_Dropdown>().value;
        GameManager.instance.configuration["colorMode"] = val;
        GameManager.instance.inputFields[13].SetActive(val == 2);
        if (val == 2)
        {
            PFAColors.Init(MIDI.realTracks);
        }
    }

    public void ReturnToMenu()
    {
        GameManager.instance.playingMidi = false;
        ClosePause();
        if (MIDIPlayer.playing)
        {
            MIDIPlayer.playing = false;
            MIDIPlayer.loop = true;
            MIDIClock.throttle = true;
            MIDI.Cleanup();
        }
        if (Sound.engine != 0)
        {
            Sound.Reload();
        }
        NoteManager.ClearEntities();
        MIDIClock.render = false;
        Time.maximumDeltaTime = (float)(1.0d/3.0d);
        NoteManager.maxBlocks = 1<<10;
        NoteManager.KNLPF = 1;
        GameObject CenterCam = GameManager.instance.CenterCam;
        CenterCam.GetComponent<RotateCam>().enabled = true;
        CenterCam.transform.rotation = Quaternion.Euler(0, 0, 0);
        GameManager.instance.container.SetActive(true);
        MIDI.PreloadPath(Application.streamingAssetsPath + "\\Menu.mid");
        MIDIPlayer.Play(4f);
        if((int)GameManager.instance.configuration["colorMode"] == 2)
        {
            PFAColors.Init(MIDI.realTracks);
        }
    }

    public void ClosePause()
    {
        pauseMenu.SetActive(false);
    }

    IEnumerator ShowLoadDialogCoroutine()
    {
        yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.Files, false, null, null, "Open MIDI file", "Open");
        res[0].SetActive(!FileBrowser.Success);
        //res[1].GetComponent<Button>().interactable = FileBrowser.Success;
        res[2].GetComponent<TextMeshProUGUI>().text = FileBrowser.Success ? "Selected: "+FileBrowser.Result[0].Replace("\\","\\\\") : "No file selected.";
        if (FileBrowser.Success)
        {
            GameManager.instance.selectedMidiPath = FileBrowser.Result[0];
        } else
        {
            GameManager.instance.selectedMidiPath = "N/A";
        }
        selectingMidi = false;
    }

    void Start()
    {
        // fill output devices
        TMP_Dropdown drop = res[7].GetComponent<TMP_Dropdown>();
        List<TMP_Dropdown.OptionData> test = drop.options;
        test.Clear();
        foreach(string i in WinMM.GetDevices())
        {
            test.Add(new TMP_Dropdown.OptionData(i));
        }
        drop.value = 0;

        // fill input devices
        drop = res[11].GetComponent<TMP_Dropdown>();
        test = drop.options;
        test.Clear();
        foreach (string i in WinMM.GetInputs())
        {
            test.Add(new TMP_Dropdown.OptionData(i));
        }
        drop.value = 0;

        if (IL2CPPBuild)
        {
            GameManager.instance.inputFields[16].GetComponent<Toggle>().interactable = false;
            TextMeshProUGUI temp = res[9].GetComponent<TextMeshProUGUI>();
            temp.color = new Color32(255,164,164,255);
            temp.text = "Rendering not supported with IL2CPP.";
        }
    }

    void Update()
    {
        bool escape = Input.GetKeyDown(KeyCode.Escape);
        if (escape && GameManager.instance.playingMidi)
        {
            pauseMenu.SetActive(!pauseMenu.activeSelf);
        }
        if (!GameManager.instance.playingMidi)
        {
            string tx = "No output loaded.";
            switch (Sound.engine)
            {
                case 1:
                    tx = "Outputting to: KDMAPI";
                    break;
                case 2:
                    tx = "Outputting to: WinMM (" + Sound.lastWinMMDevice + ")";
                    break;
            }
            res[3].GetComponent<TextMeshProUGUI>().text = tx;
            res[4].GetComponent<Button>().interactable = Sound.engine != 0;
            res[5].GetComponent<Button>().interactable = Sound.engine == 0;
            TMP_Dropdown engine = res[6].GetComponent<TMP_Dropdown>();
            engine.interactable = Sound.engine == 0;
            TMP_Dropdown device = res[7].GetComponent<TMP_Dropdown>();
            res[7].SetActive(engine.value == 1);
            device.interactable = Sound.engine == 0;
        }
    }
}
