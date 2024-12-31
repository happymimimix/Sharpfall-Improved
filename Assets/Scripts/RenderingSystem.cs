using System.Diagnostics;
using System.IO;
using System.Collections;
using UnityEngine;
using Unity.Burst;

[BurstCompile]
public class RenderingSystem : MonoBehaviour
{
    static string dir = Directory.GetCurrentDirectory();
    private static Texture2D tex;

    static Process FFmpegProc = null;

    public static IEnumerator RenderFFmpeg()
    {
        while (true)
        {
            yield return new WaitForEndOfFrame();
            if (FFmpegProc == null)
            {
                UnityEngine.Debug.Log("ffmpeg.exe is not running, starting a new process. ");
                if (!File.Exists(Application.streamingAssetsPath + "/ffmpeg.exe"))
                {
                    UnityEngine.Debug.LogError("Cannot find ffmpeg.exe! ");
                    Application.Quit();
                    break;
                }
                else
                {
                    FFmpegProc = new Process();
                    FFmpegProc.StartInfo.FileName = Application.streamingAssetsPath + "/ffmpeg.exe";
                    string[] spl = GameManager.instance.selectedMidiPath.Split("\\");
                    string use = spl[spl.Length - 1];
                    FFmpegProc.StartInfo.Arguments = $"-y -r {GameManager.instance.renderFPS} -f rawvideo -s {Screen.width}x{Screen.height} -pix_fmt bgra -i pipe:0 -c:v h264 -qp 1 -pix_fmt yuva420p -vf vflip \"{use}.mkv";
                    FFmpegProc.StartInfo.UseShellExecute = false;
                    FFmpegProc.StartInfo.RedirectStandardInput = true;
                    FFmpegProc.StartInfo.RedirectStandardOutput = true;
                    FFmpegProc.StartInfo.RedirectStandardError = true;
                    FFmpegProc.StartInfo.CreateNoWindow = true;
                    FFmpegProc.OutputDataReceived += OnOutputDataReceived;
                    FFmpegProc.ErrorDataReceived += OnErrorDataReceived;
                    FFmpegProc.Start();
                    FFmpegProc.BeginOutputReadLine();
                    FFmpegProc.BeginErrorReadLine();
                }
            }
            else
            {
                if (tex == null)
                {
                    UnityEngine.Debug.Log("Begin sending data to ffmpeg.exe... ");
                    tex = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
                }
                tex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
                tex.Apply();
                Color32[] pixels = tex.GetPixels32();
                byte[] bytes = new byte[pixels.Length * 4];
                for (int i = 0; i < pixels.Length; i++)
                {
                    Color32 pix = pixels[i];
                    bytes[i * 4] = pix.b;
                    bytes[i * 4 + 1] = pix.g;
                    bytes[i * 4 + 2] = pix.r;
                    bytes[i * 4 + 3] = pix.a;
                }
                if (FFmpegProc.HasExited)
                {
                    UnityEngine.Debug.LogError("ffmpeg.exe has been terminated unexpectedly. ");
                    Application.Quit();
                    break;
                }
                else
                {
                    FFmpegProc.StandardInput.BaseStream.Write(bytes, 0, bytes.Length);
                }
                yield return null;

                if (!GameManager.instance.playingMidi)
                {
                    FFmpegProc.StandardInput.BaseStream.Flush();
                    UnityEngine.Debug.Log("Rendering complete. ");
                    FinalizeFFmpeg();
                    yield break;
                }
            }
        }
    }

    private static void FinalizeFFmpeg()
    {
        if (FFmpegProc != null && !FFmpegProc.HasExited)
        {
            UnityEngine.Debug.Log("Finalizing video... ");
            try
            {
                FFmpegProc.StandardInput.Close();
                FFmpegProc.WaitForExit();
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError("Error while finalizing video: " + ex.Message);
            }
            finally
            {
                FFmpegProc.Dispose();
                FFmpegProc = null;
                tex = null;
            }
        }
    }

    public static void OnOutputDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (!string.IsNullOrEmpty(e.Data))
        {
            UnityEngine.Debug.Log("[FFmpeg Console Output] " + e.Data);
        }
    }

    public static void OnErrorDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (!string.IsNullOrEmpty(e.Data))
        {
            UnityEngine.Debug.Log("[FFmpeg Console Output] " + e.Data);
        }
    }

    public void OnApplicationQuit()
    {
        UnityEngine.Debug.Log("Application quitting. Ensuring FFmpeg finalizes the video...");
        FinalizeFFmpeg();
    }

}
