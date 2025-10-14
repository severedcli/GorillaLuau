using BepInEx;
using UnityEngine;
using Utilla.Attributes;

using GorillaLuau.Lua;

namespace GorillaLuau
{
    [BepInPlugin("com.severedcli.gorillaluau", "GorillaLuau", "1.0.0")]
    [BepInDependency("org.legoandmars.gorillatag.utilla", "1.6.0")]
    [ModdedGamemode]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin instance;

        public bool isAllowed { get; private set; } = false;

        private bool windowOpen = true;
        private Rect windowRect = new Rect(100, 100, 600, 400);
        private string codeInput = "";
        private Vector2 scrollPos = Vector2.zero;

        private void Awake()
        {
            instance = this;
        }

        [ModdedGamemodeJoin]
        private void OnJoin()
        {
            isAllowed = true;
        }

        [ModdedGamemodeLeave]
        private void OnLeave()
        {
            isAllowed = false;
        }

        private void OnGUI()
        {
            if (isAllowed)
            {
                if (GUILayout.Button(windowOpen ? "Close" : "Open", GUILayout.Width(120)))
                {
                    windowOpen = !windowOpen;
                }

                if (windowOpen)
                {
                    windowRect = GUI.Window(0, windowRect, drawWindow, "GorillaLuau");
                }
            }
        }

        private void drawWindow(int windowID)
        {
            GUIStyle textStyle = new GUIStyle(GUI.skin.textArea);
            textStyle.wordWrap = true;
            textStyle.fontSize = 14;
            textStyle.normal.textColor = Color.white;
            textStyle.normal.background = MakeTex(2, 2, new Color(0.15f, 0.15f, 0.15f, 0.9f));

            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.fontSize = 14;

            scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.Height(300));
            codeInput = GUILayout.TextArea(codeInput, textStyle, GUILayout.ExpandHeight(true));
            GUILayout.EndScrollView();

            if (GUILayout.Button("Run Code", buttonStyle))
            {
                VM.runCode(codeInput);
            }

            GUI.DragWindow();
        }

        private Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; ++i)
                pix[i] = col;
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }
    }
}