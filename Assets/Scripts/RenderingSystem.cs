using System.IO;
using System.Collections;
using UnityEngine;
using Unity.Burst;
using System.Diagnostics;

[BurstCompile]
public class RenderingSystem : MonoBehaviour
{
    static int frames = 0;
    static string dir = Directory.GetCurrentDirectory();
    private static Texture2D tex;
    private static RenderTexture tex2;

    public static bool firstFind = true;
    public static bool useCRF = true;
    public static bool ffmpegLog = true;
    public static string ffmpegCodec = "libx264";
    private static string[] ffmpegCodecs = new string[]
    {
        "libx264",
        "libx265",
        "h264_nvenc",
        "hevc_nvenc"
    };
    public static uint ffmpegBitrateKB = 50000;
    public static string ffmpegPreset = "veryfast";
    private static string[] ffmpegPresets = new string[]
    {
        "ultrafast",
        "superfast",
        "veryfast",
        "faster",
        "fast",
        "medium",
        "slow",
        "slower",
        "veryslow",
        "placebo"
    };
    public static bool ffmpegAvailable = false;
    private static int renderMode = 0;

    public static IEnumerator RenderImages()
    {
        //wait for frame end
        yield return new WaitForEndOfFrame();
        //read display
        if (tex == null || tex.width != Screen.width || tex.height != Screen.height)
        {
            if (tex != null)
                DestroyImmediate(tex);  // Release the previous texture
            tex = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        }
        tex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        tex.Apply();
        byte[] bytes;
        if (!Directory.Exists(dir + "\\Render"))
        {
            Directory.CreateDirectory(dir + "\\Render");
        }
        if (renderMode == 1)
        {
            bytes = tex.EncodeToJPG(100);
            File.WriteAllBytes(dir + "\\Render\\frame" + frames + ".jpg", bytes);
        } else
        {
            bytes = tex.EncodeToPNG();
            File.WriteAllBytes(dir + "\\Render\\frame" + frames + ".png", bytes);
        }
        bytes = null;
        frames++;
    }

    static Process FFmpegProc = null;

    public static IEnumerator RenderFFmpeg()
    {
        ffmpegPreset = ffmpegPresets[(int)GameManager.instance.configuration["ffmpegPreset"]];
        int resX = (int)GameManager.instance.configuration["renderResX"];
        int resY = (int)GameManager.instance.configuration["renderResY"];
        tex2 = new RenderTexture(resX, resY, 24);
        tex2.antiAliasing = 8;
        GameManager.instance.renderCamera.GetComponent<Camera>().enabled = true;
        GameManager.instance.renderCamera.targetTexture = tex2;
        while (true)
        {
            yield return new WaitForEndOfFrame();
            if (FFmpegProc == null)
            {
                if (!File.Exists(Application.streamingAssetsPath + "/ffmpeg.exe"))
                {
                    Application.Quit();
                    break;
                }
                else
                {
                    string qualityOptions, dolog = "";
                    if (useCRF)
                    {
                        qualityOptions = "-crf " + (GameManager.instance.configuration["ffmpegCRF"].ToString()) + " -preset " + ffmpegPreset;
                    }
                    else
                    {
                        qualityOptions = "-b:v " + ffmpegBitrateKB + "K -b_ref_mode 0";
                    }
                    if (ffmpegLog)
                    {
                        dolog = "-report";
                    }
                    FFmpegProc = new Process();
                    FFmpegProc.StartInfo.FileName = Application.streamingAssetsPath + "/ffmpeg.exe";
                    string[] spl = GameManager.instance.selectedMidiPath.Split("\\");
                    string use = spl[spl.Length - 1];
                    use = use.Replace(".mid", "");
                    FFmpegProc.StartInfo.Arguments = $"-y {dolog} -r {GameManager.instance.renderFPS} -f rawvideo -s {resX}x{resY} -pixel_format rgb24 -i pipe:0 -c:v {ffmpegCodec} -vf vflip -pix_fmt rgb32 {qualityOptions} \"Render-{use}.mkv\"";
                    FFmpegProc.StartInfo.UseShellExecute = false;
                    FFmpegProc.StartInfo.RedirectStandardInput = true;
                    FFmpegProc.StartInfo.RedirectStandardOutput = true;
                    FFmpegProc.StartInfo.CreateNoWindow = true;
                    FFmpegProc.Start();
                }
            }
            if (FFmpegProc != null)
            {
                if (!GameManager.instance.playingMidi)
                {
                    FFmpegProc.Kill();
                    FFmpegProc = null;
                    GameManager.instance.renderCamera.targetTexture.Release();
                    GameManager.instance.renderCamera.GetComponent<Camera>().enabled = false;
                    tex = null;
                    yield break;
                }
                if (tex == null)
                {
                    tex = new Texture2D(resX, resY, TextureFormat.RGB24, false);
                }
                GameManager.instance.renderCamera.Render();
                RenderTexture.active = tex2;
                tex.ReadPixels(new Rect(0, 0, resX, resY), 0, 0);
                tex.Apply();
                RenderTexture.active = null;
                byte[] bytes = tex.GetRawTextureData();
                if (FFmpegProc.HasExited)
                {
                    Application.Quit();
                    break;
                }
                else
                {
                    FFmpegProc.StandardInput.BaseStream.Write(bytes, 0, bytes.Length);
                    FFmpegProc.StandardInput.BaseStream.Flush();
                }
                yield return null;
            }
            else
            {
                Application.Quit();
                break;
            }
        }
    }
}
