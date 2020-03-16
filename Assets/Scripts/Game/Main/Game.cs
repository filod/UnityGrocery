#define DEBUG_LOGGING
using UnityEngine;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine.Rendering;
using System;
using System.Globalization;
using System.Linq;
using Unity.DebugDisplay;
using Unity.Sample.Core;
using UnityEngine.Profiling;
#if UNITY_EDITOR
using UnityEditorInternal;
using UnityEditor;
using Unity.NetCode.Editor;
#endif





public class EnumeratedArrayAttribute : PropertyAttribute
{
    public readonly string[] names;
    public EnumeratedArrayAttribute(Type enumtype)
    {
        names = Enum.GetNames(enumtype);
    }
}



[ExecuteAlways]
[DisableAutoCreation]
public class ControlledEntityCameraUpdate : ManualComponentSystemGroup
{
    protected override void OnUpdate()
    {
        Profiler.BeginSample("ControlledEntityCameraUpdate");
        base.OnUpdate();
        Profiler.EndSample();
    }
}



[DefaultExecutionOrder(-1000)]
public class Game : MonoBehaviour
{
    public delegate void UpdateDelegate();

    public WeakAssetReference dotsNetCodePrefabs;

    public static Game game;
    public event UpdateDelegate endUpdateEvent;

    // Vars owned by server and replicated to clients
    [ConfigVar(Name = "server.tickrate", DefaultValue = "60", Description = "Tickrate for server", Flags = ConfigVar.Flags.ServerInfo)]
    public static ConfigVar serverTickRate;

//    [ConfigVar(Name = "config.fov", DefaultValue = "60", Description = "Field of view", Flags = ConfigVar.Flags.Save)]
//    public static ConfigVar configFov;

    [ConfigVar(Name = "debug.catchloop", DefaultValue = "1", Description = "Catch exceptions in gameloop and pause game", Flags = ConfigVar.Flags.None)]
    public static ConfigVar debugCatchLoop;

    [ConfigVar(Name = "chartype", DefaultValue = "-1", Description = "Character to start with (-1 uses default character)")]
    public static ConfigVar characterType;

    [ConfigVar(Name = "allowcharchange", DefaultValue = "1", Description = "Is changing character allowed")]
    public static ConfigVar allowCharChange;

    [ConfigVar(Name = "debug.cpuprofile", DefaultValue = "0", Description = "Profile and dump cpu usage")]
    public static ConfigVar debugCpuProfile;

    [ConfigVar(Name = "net.dropevents", DefaultValue = "0", Description = "Drops a fraction of all packages containing events!!")]
    public static ConfigVar netDropEvents;

    [ConfigVar(Name = "show.entities", DefaultValue = "0", Description = "Entity stats")]
    public static ConfigVar showEntities;

    static readonly string k_UserConfigFilename = "user.cfg";
    public static readonly string k_BootConfigFilename = "boot.cfg";

    public Camera bootCamera;

    public static double frameTime;
    public static int frameCount;

    public static bool IsHeadless()
    {
        return game.m_isHeadless;
    }

    public static int GameLoopCount
    {
        get { return game == null ? 0 : 1; }
    }

    public static System.Diagnostics.Stopwatch Clock
    {
        get { return game.m_Clock; }
    }

    public string buildId
    {
        get { return _buildId; }
    }
    string _buildId = "NoBuild";

    public string buildUnityVersion
    {
        get { return _buildUnityVersion; }
    }
    // Start with sensible default, but we would like to have the full build version
    // which is only available in editor so we bake it into the build.
    string _buildUnityVersion = Application.unityVersion;



    // Pick argument for argument(!). Given list of args return null if option is
    // not found. Return argument following option if found or empty string if none given.
    // Options are expected to be prefixed with + or -
    public static string ArgumentForOption(List<string> args, string option)
    {
        var idx = args.IndexOf(option);
        if (idx < 0)
            return null;
        if (idx < args.Count - 1)
            return args[idx + 1];
        return "";
    }

    public void Awake()
    {
        GameDebug.Assert(game == null);
        DontDestroyOnLoad(gameObject);
        game = this;

        GameApp.IsInitialized = true;

        //PreLoadAnimationNodes();

        m_StopwatchFrequency = System.Diagnostics.Stopwatch.Frequency;
        m_Clock = new System.Diagnostics.Stopwatch();
        m_Clock.Start();

#if UNITY_EDITOR
        _buildUnityVersion = InternalEditorUtility.GetFullUnityVersion();
#endif

        var commandLineArgs = new List<string>(System.Environment.GetCommandLineArgs());

        // TODO we should only initialize this if we have a graphics device (i.e. non-headless)
        Overlay.Managed.Initialize();

#if UNITY_STANDALONE_LINUX
        m_isHeadless = true;
#else
        m_isHeadless = commandLineArgs.Contains("-batchmode");
#endif
        var noconsole = commandLineArgs.Contains("-noconsole");
        var consoleRestoreFocus = commandLineArgs.Contains("-consolerestorefocus");
        if (noconsole)
        {
            UnityEngine.Debug.Log("WARNING: starting without a console");
            var consoleUI = new ConsoleNullUI();
            Console.Init(buildId,buildUnityVersion, consoleUI);
        }else if (m_isHeadless)
        {
#if UNITY_EDITOR
            Debug.LogError("ERROR: Headless mode not supported in editor");
#endif

#if UNITY_STANDALONE_WIN
            string consoleTitle;

            var overrideTitle = ArgumentForOption(commandLineArgs, "-title");
            if (overrideTitle != null)
                consoleTitle = overrideTitle;
            else
                consoleTitle = Application.productName + " Console";

            consoleTitle += " [" + System.Diagnostics.Process.GetCurrentProcess().Id + "]";

            var consoleUI = new ConsoleTextWin(consoleTitle, consoleRestoreFocus);
#elif UNITY_STANDALONE_LINUX
            var consoleUI = new ConsoleTextLinux();
#else
            UnityEngine.Debug.Log("WARNING: starting without a console");
            var consoleUI = new ConsoleNullUI();
#endif
            Console.Init(buildId,buildUnityVersion,consoleUI);
        }
        else
        {
            var consoleUI = Instantiate(Resources.Load<ConsoleGUI>("Prefabs/ConsoleGUI"));
            DontDestroyOnLoad(consoleUI);
            Console.Init(buildId,buildUnityVersion,consoleUI);

            m_DebugOverlay = Instantiate(Resources.Load<DebugOverlay>("DebugOverlay"));
            DontDestroyOnLoad(m_DebugOverlay);
            m_DebugOverlay.Init();
        }

        // If -logfile was passed, we try to put our own logs next to the engine's logfile
        // if -logfile was set to "-" we forward our logs to Debug.Log, so that it ends up on stdout.
        var engineLogFileLocation = ".";
        var logName = m_isHeadless ? "game_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff") : "game";
        var logfileArgIdx = commandLineArgs.IndexOf("-logfile");
        var forceForwardToDebug = false;
        if (logfileArgIdx >= 0 && commandLineArgs.Count >= logfileArgIdx)
        {
            var logFile = commandLineArgs[logfileArgIdx + 1];
            if (logFile == "-")
                forceForwardToDebug = true;
            else
                engineLogFileLocation = System.IO.Path.GetDirectoryName(logFile);
        }
        GameDebug.Init(engineLogFileLocation, logName, forceForwardToDebug);

        ConfigVar.Init();

        // Support -port and -query_port as per Multiplay standard
        var serverPort = ArgumentForOption(commandLineArgs, "-port");
        if (serverPort != null)
            Console.EnqueueCommandNoHistory("server.port " + serverPort);

        var sqpPort = ArgumentForOption(commandLineArgs, "-query_port");
        if (sqpPort != null)
            Console.EnqueueCommandNoHistory("server.sqp_port " + sqpPort);

        Console.EnqueueCommandNoHistory("exec -s " + k_UserConfigFilename);

        // Default is to allow no frame cap, i.e. as fast as possible if vsync is disabled
        Application.targetFrameRate = -1;

        if (m_isHeadless)
        {
            Application.targetFrameRate = serverTickRate.IntValue;
            QualitySettings.vSyncCount = 0; // Needed to make targetFramerate work; even in headless mode

#if !UNITY_STANDALONE_LINUX
            if (!commandLineArgs.Contains("-nographics"))
                GameDebug.Log("WARNING: running -batchmod without -nographics");
#endif
        }
        else
        {
            //RenderSettings.Init();
        }

        // Out of the box game behaviour is driven by boot.cfg unless you ask it not to
        if (!commandLineArgs.Contains("-noboot"))
        {
            Console.EnqueueCommandNoHistory("exec -s " + k_BootConfigFilename);
        }

        GameDebug.Log("A2 initialized");
#if UNITY_EDITOR
        GameDebug.Log("Build type: editor");
#elif DEVELOPMENT_BUILD
        GameDebug.Log("Build type: development");
#else
        GameDebug.Log("Build type: release");
#endif
        GameDebug.Log("BuildID: " + buildId);
        GameDebug.Log("Unity: " + buildUnityVersion);
        GameDebug.Log("Cwd: " + System.IO.Directory.GetCurrentDirectory());

        GameDebug.Log("InputSystem initialized");

        Console.AddCommand("quit", CmdQuit, "Quits");
        //Console.AddCommand("screenshot", CmdScreenshot, "Capture screenshot. Optional argument is destination folder or filename.");
        Console.AddCommand("crashme", (string[] args) => { GameDebug.Assert(false); }, "Crashes the game next frame ");
        Console.AddCommand("saveconfig", CmdSaveConfig, "Save the user config variables");
        Console.AddCommand("loadconfig", CmdLoadConfig, "Load the user config variables");

        Console.SetOpen(true);
        Console.ProcessCommandLineArguments(commandLineArgs.ToArray());



        GameApp.CameraStack.OnCameraEnabledChanged += OnCameraEnabledChanged;
        GameApp.CameraStack.PushCamera(bootCamera);
    }

    void OnDisable()
    {
        GameDebug.Shutdown();
        Overlay.Managed.DoShutdown();
        Console.Shutdown();

        game = null;
        GameApp.IsInitialized = false;

        InputSystem.SetMousePointerLock(false);
        GameDebug.Log("A2 was shutdown");
    }

    public void Update()
    {

        GameApp.CameraStack.Update();


#if UNITY_EDITOR
        // Ugly hack to force focus to game view when using scriptable renderloops.
        if (Time.frameCount < 4)
        {
            try
            {
                var gameViewType = typeof(UnityEditor.EditorWindow).Assembly.GetType("UnityEditor.GameView");
                var gameView = (EditorWindow)Resources.FindObjectsOfTypeAll(gameViewType)[0];
                gameView.Focus();
            }
            catch (System.Exception) { /* too bad */ }
        }
#endif

        frameTime = (double)m_Clock.ElapsedTicks / m_StopwatchFrequency;
        frameCount = Time.frameCount;

        GameDebug.SetFrameCount(frameCount);
        Console.SetFrameTime(frameTime);;

        Console.ConsoleUpdate();

        //bool menusShowing = (clientFrontend != null && clientFrontend.menuShowing != ClientFrontend.MenuShowing.None);
        InputSystem.WindowFocusUpdate(false);

        UpdateCPUStats();
        UpdateEntityStats();

        endUpdateEvent?.Invoke();
    }
    //int numMBS = 0;
    //int numUMBS = 0;

    bool m_ErrorState;

    public void LateUpdate()
    {
        Console.ConsoleLateUpdate();
        if (m_DebugOverlay != null)
            m_DebugOverlay.TickLateUpdate();

        Unity.DebugDisplay.Overlay.Managed.instance.TickLateUpdate();
    }

    void OnCameraEnabledChanged(Camera camera, bool enabled)
    {
        var audioListener = camera.GetComponent<AudioListener>();
        if (audioListener != null)
        {
            audioListener.enabled = enabled;
        }

    }


    float m_NextCpuProfileTime = 0;
    double m_LastCpuUsage = 0;
    double m_LastCpuUsageUser = 0;
    void UpdateCPUStats()
    {
        if (debugCpuProfile.IntValue > 0)
        {
            if (Time.time > m_NextCpuProfileTime)
            {
                const float interval = 5.0f;
                m_NextCpuProfileTime = Time.time + interval;
                var process = System.Diagnostics.Process.GetCurrentProcess();
                var user = process.UserProcessorTime.TotalMilliseconds;
                var total = process.TotalProcessorTime.TotalMilliseconds;
                float userUsagePct = (float)(user - m_LastCpuUsageUser) / 10.0f / interval;
                float totalUsagePct = (float)(total - m_LastCpuUsage) / 10.0f / interval;
                m_LastCpuUsage = total;
                m_LastCpuUsageUser = user;
                GameDebug.Log(string.Format("CPU Usage {0}% (user: {1}%)", totalUsagePct, userUsagePct));
            }
        }
    }

    void UpdateEntityStats()
    {
        if (showEntities.IntValue <= 0)
            return;

        int y = 10;
        var aw = World.All;
        Overlay.Managed.Write(2, y++, "Worlds: {0}", aw.Count);
        foreach(var w in aw)
        {
            Overlay.Managed.Write(3, y++, "{0}: {1} ents  {2} sys", w.Name, w.EntityManager.UniversalQuery.CalculateEntityCountWithoutFiltering(), w.Systems.Count<ComponentSystemBase>());
        }
    }


    string FindNewFilename(string pattern)
    {
        for (var i = 0; i < 10000; i++)
        {
            var f = string.Format(pattern, i);
            if (System.IO.File.Exists(string.Format(pattern, i)))
                continue;
            return f;
        }
        return null;
    }

    void CmdQuit(string[] args)
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void CmdSaveConfig(string[] arguments)
    {
        ConfigVar.Save(k_UserConfigFilename);
    }

    void CmdLoadConfig(string[] arguments)
    {
        Console.EnqueueCommandNoHistory("exec " + k_UserConfigFilename);
    }


    DebugOverlay m_DebugOverlay;

    bool m_isHeadless;
    long m_StopwatchFrequency;
    System.Diagnostics.Stopwatch m_Clock;
}

