using UnityEngine;
using System.Collections.Generic;

namespace Debugging
{
    public class DebugLogger : MonoBehaviour
    {
        private List<string> logs = new List<string>();
        private Vector2 scrollPosition;
        private bool showLogs = true;

        void Awake()
        {
            DontDestroyOnLoad(this.gameObject);
            Application.logMessageReceived += HandleLog;
        }

        void OnDestroy()
        {
            Application.logMessageReceived -= HandleLog;
        }

        void HandleLog(string logString, string stackTrace, LogType type)
        {
            string color = "white";
            if (type == LogType.Error || type == LogType.Exception) color = "red";
            else if (type == LogType.Warning) color = "yellow";

            string entry = $"<color={color}>[{type}] {logString}</color>";
            logs.Add(entry);
            
            // Keep only last 50 logs
            if (logs.Count > 50) logs.RemoveAt(0);
        }

        void OnGUI()
        {
            if (!showLogs) return;

            // Scale GUI for high DPI screens
            float scale = Screen.dpi / 160f; // Baseline 160dpi
            if (scale < 1) scale = 1;
            
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1));

            // Draw Box
            float width = Screen.width / scale;
            float height = Screen.height / scale / 3; // Bottom 1/3rd
            
            GUILayout.BeginArea(new Rect(0, Screen.height / scale - height, width, height), GUI.skin.box);
            
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            
            foreach (var log in logs)
            {
                // Strip rich text manually if needed, but GUI label supports some tags if configured.
                // Standard GUI.Label doesn't support rich text by default in older Unity versions, 
                // but let's just print simple text to be safe if tags fail.
                GUILayout.Label(log); 
            }
            
            GUILayout.EndScrollView();
            GUILayout.EndArea();

            // Toggle Button
            if (GUI.Button(new Rect(10, 10, 100, 50), "Toggle Logs"))
            {
                showLogs = !showLogs;
            }
        }
    }
}
