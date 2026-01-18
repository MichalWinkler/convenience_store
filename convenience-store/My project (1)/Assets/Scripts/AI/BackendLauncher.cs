/*
 * Uruchamiacz Backendu
 * --------------------
 * Skrypt automatycznie uruchamiający serwer Python przy starcie gry w Unity.
 * Sprawdza, czy port jest zajęty, ustala ścieżkę do środowiska wirtualnego (.venv)
 * i zarządza procesem systemowym serwera.
 */
using UnityEngine;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;

public class BackendLauncher : MonoBehaviour
{
    private static Process pythonProcess;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Initialize()
    {
        // Nie uruchamiaj wielu instancji
        if (pythonProcess != null && !pythonProcess.HasExited) return;

        // Sprawdź czy port 8000 jest już zajęty (np. przez ręczne uruchomienie)
        if (IsPortOccupied(8000))
        {
            UnityEngine.Debug.Log("[BackendLauncher] Port 8000 is already in use. Assuming server is running externally.");
            return;
        }

        // Utwórz GameObject do obsługi OnApplicationQuit dla sprzątania
        GameObject go = new GameObject("BackendLauncher");
        DontDestroyOnLoad(go);
        go.AddComponent<BackendLauncher>();
        
        StartServer();
    }

    // Sprawdza czy dany port TCP jest nasłuchiwany
    static bool IsPortOccupied(int port)
    {
        try
        {
            using (var client = new TcpClient())
            {
                var result = client.BeginConnect("127.0.0.1", port, null, null);
                var success = result.AsyncWaitHandle.WaitOne(System.TimeSpan.FromMilliseconds(500));
                if (success)
                {
                    client.EndConnect(result);
                    return true;
                }
            }
        }
        catch { }
        return false;
    }

    // Uruchamia proces Pythona (uvicorn)
    static void StartServer()
    {
        // Ścieżka do głównego folderu projektu Python
#if UNITY_EDITOR
        string projectPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../../../convenience-store-python"));
#else
        // W Buildzie, Application.dataPath to "FolderGry_Data".
        // Zakładam, że build jest w folderze "_Builds" (lub podobnym) wewnątrz głównego katalogu repozytorium.
        // Wtedy ścieżka: Gry_Data -> _Builds -> RepoRoot -> convenience-store-python
        string projectPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../../convenience-store-python"));
#endif
        
        // Ścieżka do pliku wykonywalnego python w .venv
        string pythonExe = Path.Combine(projectPath, ".venv", "Scripts", "python.exe");

        if (!File.Exists(pythonExe))
        {
            UnityEngine.Debug.LogError($"[BackendLauncher] Python executable not found at: {pythonExe}\nMake sure your .venv is set up correctly.");
            return;
        }

        ProcessStartInfo startInfo = new ProcessStartInfo();
        startInfo.FileName = pythonExe;
        startInfo.Arguments = "-m uvicorn api:app --host 127.0.0.1 --port 8000";
        startInfo.WorkingDirectory = projectPath;
        
        // Ukryj okno, ale zachowaj możliwość czytania stdout (nie używane tutaj dla uproszczenia)
        startInfo.UseShellExecute = false; 
        startInfo.CreateNoWindow = true; 
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;

        try
        {
            pythonProcess = Process.Start(startInfo);
            UnityEngine.Debug.Log($"[BackendLauncher] Starting Python Server... (PID: {pythonProcess.Id})");
            
            // Loguj błędy asynchronicznie
            pythonProcess.BeginErrorReadLine();
            pythonProcess.ErrorDataReceived += (sender, args) => 
            {
                if (!string.IsNullOrEmpty(args.Data))
                    UnityEngine.Debug.LogError($"[Python Output]: {args.Data}");
            };
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError($"[BackendLauncher] Failed to start python server: {e.Message}");
        }
    }

    // Sprzątanie przy wyjściu z gry - zabijanie procesu
    void OnApplicationQuit()
    {
        if (pythonProcess != null && !pythonProcess.HasExited)
        {
            UnityEngine.Debug.Log("[BackendLauncher] Stopping python server...");
            try
            {
                pythonProcess.Kill();
                pythonProcess.Dispose();
            }
            catch(System.Exception e)
            {
                UnityEngine.Debug.LogWarning($"[BackendLauncher] Error killing process: {e.Message}");
            }
            pythonProcess = null;
        }
    }
}
