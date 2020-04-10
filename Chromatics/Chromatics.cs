﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using Chromatics.ACTInterfaces;
using Chromatics.Controllers;
using Chromatics.Datastore;
using Chromatics.DeviceInterfaces;
using Chromatics.LCDInterfaces;
using CSharpAnalytics;
using CSharpAnalytics.Activities;
using Gma.System.MouseKeyHook;
using Microsoft.VisualBasic.FileIO;
using Microsoft.Win32;

namespace Chromatics
{
    public partial class Chromatics : Form, ILogWrite
    {
        //Setup Threading/Tasks
        private CancellationTokenSource _memoryTask = new CancellationTokenSource();
        
        private ILogitechArx _arx;
        private ILogitechLcd _lcd;
        private Task _call;
        public Task MemoryTask;
        public Thread FfxivThread;

        private string DebugMapID = "";
        private string DebugJobClass = "";
        private string DebugInstanceID = "";

        private bool _allowClose;
        private bool _allowVisible = true;
        private bool _exit;

        public bool ArxSdk;
        public int ArxSdkCalled;
        public int ArxState;
        public bool ArxToggle = true;

        public bool LcdSdk;
        public int LcdSdkCalled;

        private CancellationTokenSource _attachcts = new CancellationTokenSource();

        //Setup Variables
        public int Attatched;

        public ChromaticsSettings ChromaticsSettings = new ChromaticsSettings();

        public FfxivColorMappings ColorMappings = new FfxivColorMappings();
         
        private bool _coolermasterDeviceKeyboard = true;
        private bool _coolermasterDeviceMouse = true;
        public bool CoolermasterSdk = false;
        public int CoolermasterSdkCalled = 0;

        private bool _corsairDeviceHeadset = true;
        private bool _corsairDeviceKeyboard = true;
        private bool _corsairDeviceKeypad = true;
        private bool _corsairDeviceMouse = true;
        private bool _corsairDeviceMousepad = true;
        private bool _corsairDeviceOther = true;

        public bool CorsairRescan = false;
        public bool CorsairSdk = false;
        public int CorsairSdkCalled = 0;
        private readonly string _currentVersionX = "2.5.6";
        private readonly bool _debugmode = false;
        public bool DeviceGridStartup = false;
        private bool blockACTVersion = false;
        private const int ACTVersionMatch = 1;

        private bool threshTrigger = false;
        private bool threshTimer = false;
        private bool threshFlash = false;

        private bool ChatInit = false;
        private int ChatpreviousArrayIndex = 0;
        private int ChatpreviousOffset = 0;
        private bool pullCountdownRun = false;
        private int pullCountMax = 0;
        private int pullCountInterval = 0;
        private int statEffectcount = 0;
        private bool statEffectActive = false;

        public bool EffectRunning = false;
        private CancellationTokenSource _ffxiVcts = new CancellationTokenSource();

        private bool _gameNotify;
        //private Timer _gameResetCatch;
        public bool HoldReader = false;

        public bool _SDKRazer = true;
        public bool _SDKLogitech = true;
        public bool _SDKCorsair = true;
        public bool _SDKCooler = true;
        public bool _SDKSteelSeries = true;
        public bool _SDKWooting = true;
        public bool _SDKAsus = true;
        public bool _SDKMystic = true;
        public bool _SDKLifx = true;

        public int _ChromaLinkLEDCountZ1 = 10;
        public int _ChromaLinkLEDCountZ2 = 10;
        public int _ChromaLinkLEDCountZ3 = 10;
        public int _ChromaLinkLEDCountZ4 = 10;
        public int _ChromaLinkLEDCountZ5 = 10;
        public int _ChromaLinkLEDCountZ6 = 10;
        public bool _OtherInterpolateEffects = false;
        public bool _ReverseInterpolateEffects = false;

        public int HueSdkCalled = 0;
        public bool Init = false;
        public bool IsDx11 = false;
        private bool _keyAlt;
        private bool _gameStop = true;

        private bool _keyCtrl;
        private bool _keyShift;
        private string _lgsInstall = "";
        public bool LifxRescan = false;
        public bool LifxSdk = false;
        public int LifxSdkCalled = 0;
        private List<string> Lifxduplicates = new List<string>();

        private bool _logitechDeviceKeyboard = true;
        public bool LogitechRescan = false;
        public bool LogitechSdk = false;
        public int LogitechSdkCalled = 0;
        private IKeyboardMouseEvents _mGlobalHook;
        public bool MappingGridStartup = false;
        public int PaletteMappingCurrentSelect = 0;
        public bool LCCStatus = false;

        public List<string> Plugs = new List<string>();
        private bool _razerDeviceHeadset = true;

        private bool _razerDeviceKeyboard = true;
        private bool _razerDeviceKeypad = true;
        private bool _razerDeviceMouse = true;
        private bool _razerDeviceMousepad = true;
        private bool _razerDeviceChromaLink = true;
        public bool RazerRescan = false;
        public bool RazerSdk = false;
        public int RazerSdkCalled = 0;

        private bool _steelDeviceKeyboard = true;
        private bool _steelDeviceKeypad = true;
        private bool _steelDeviceMouse = true;
        private bool _steelDeviceMousepad = true;
        private bool _steelDeviceHeadset = true;
        public bool SteelSdk = false;
        public int SteelSdkCalled = 0;

        private bool _wootingDeviceKeyboard = true;
        public bool WootingSdk = false;
        public int WootingSdkCalled = 0;

        private bool _asusDeviceKeyboard = true;
        private bool _asusDeviceMouse = true;
        private bool _asusDeviceHeadset = true;
        private bool _asusDeviceOther = true;
        public bool AsusSdk = false;
        public int AsusSdkCalled = 0;

        private bool _mysticDeviceOther = true;
        private bool _mysticDeviceKeyboard = true;
        public bool MysticSdk = false;
        public int MysticSdkCalled = 0;
        public bool MysticFirstSet = false;

        public bool _EnableKeypadBinds = false;
        public bool _KZ1Enabled = true;
        public bool _KZ2Enabled = true;
        public bool _KZ3Enabled = true;
        public bool _KZ4Enabled = true;
        public bool _KZ5Enabled = true;
        public bool _KZ6Enabled = true;
        public bool _KZ7Enabled = true;
        public bool _KZ8Enabled = true;
        public bool _KZ9Enabled = true;
        public bool _KZ10Enabled = true;
        public bool _KZ11Enabled = true;
        public bool _KZ12Enabled = true;
        public bool _KZ13Enabled = true;
        public bool _KZ14Enabled = true;
        public bool _KZ15Enabled = true;
        public bool _KZ16Enabled = true;
        public bool _KZ17Enabled = true;
        public bool _KZ18Enabled = true;
        public bool _KZ19Enabled = true;
        public bool _KZ20Enabled = true;

        public Dictionary<int, string> _KeyBindMap = new Dictionary<int, string>();
        public Dictionary<int, Modifiers> _KeyBindModMap = new Dictionary<int, Modifiers>();

        private readonly RegistryKey _rkApp =
            Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

        private bool _roccatDeviceKeyboard = true;
        private bool _roccatDeviceMouse = true;
        public bool RoccatSdk = false;
        public int RoccatSdkCalled = 0;
        public bool SetKeysbase;
        public bool SetMousebase;
        public bool SetPadbase;
        public bool SetHeadsetbase;
        public bool SetKeypadbase;
        public bool SetCLbase;
        public bool Startup;
        public int State = 0;

        private bool razerFirstSet = false;
        private bool logitechFirstSet = false;
        private bool corsairFirstSet = false;
        private bool coolermasterFirstSet = false;
        private bool steelFirstSet = false;
        private bool wootingFirstSet = false;
        private bool lifxFirstSet = false;
        private bool AsusFirstSet = false;

        private bool _deviceKeyboard = true;
        private bool _deviceMouse = true;
        private bool _deviceMousepad = true;
        private bool _deviceHeadset = true;
        private bool _deviceKeypad = true;
        private bool _deviceCL = true;
        private bool _KeysSingleKeyModeEnabled;
        private bool _KeysMultiKeyModeEnabled;
        private DevModeTypes _KeysSingleKeyMode = DevModeTypes.Disabled;
        private DevMultiModeTypes _KeysMultiKeyMode = DevMultiModeTypes.Disabled;
        private int _KeyMultiLEDCount = 5;
        private bool _KeyMultiReverse = false;

        public DevModeTypes _MouseZone1Mode = DevModeTypes.BaseMode;
        public DevModeTypes _MouseZone2Mode = DevModeTypes.EnmityTracker;
        public DevModeTypes _MouseZone3Mode = DevModeTypes.BaseMode;
        public DevModeTypes _MouseStrip1Mode = DevModeTypes.HpTracker; 
        public DevModeTypes _MouseStrip2Mode = DevModeTypes.MpTracker;

        public DevModeTypes _PadZone1Mode = DevModeTypes.HpTracker;
        public DevModeTypes _PadZone2Mode = DevModeTypes.Castbar;
        public DevModeTypes _PadZone3Mode = DevModeTypes.MpTracker;

        public DevModeTypes _HeadsetZone1Mode = DevModeTypes.BaseMode;
        public DevModeTypes _HeadsetZone2Mode = DevModeTypes.HighlightColor;
        public DevMultiModeTypes _KeypadZone1Mode = DevMultiModeTypes.BaseMode;

        public DevModeTypes _CLZone1Mode = DevModeTypes.BaseMode;
        public DevModeTypes _CLZone2Mode = DevModeTypes.BaseMode;
        public DevModeTypes _CLZone3Mode = DevModeTypes.BaseMode;
        public DevModeTypes _CLZone4Mode = DevModeTypes.BaseMode;
        public DevModeTypes _CLZone5Mode = DevModeTypes.BaseMode;
        public DevModeTypes _CLZone6Mode = DevModeTypes.BaseMode;

        public LightbarMode _LightbarMode = LightbarMode.TargetHp;
        public FKeyMode _FKeyMode = FKeyMode.HpJobMp;
        public ACTMode _ACTMode = ACTMode.DPS;

        //Main Thread
        public Chromatics()
        {
            InitializeComponent();
        }

        public void WriteConsole(ConsoleTypes type, string line)
        {
            try
            {
                if (_exit) return;

                if (InvokeRequired)
                {
                    if (type == ConsoleTypes.System)
                        rtb_debug.Invoke((Action) delegate { rtb_debug.SelectionColor = Color.Black; });
                    else if (type == ConsoleTypes.Ffxiv)
                        rtb_debug.Invoke((Action) delegate { rtb_debug.SelectionColor = Color.DarkCyan; });
                    else if (type == ConsoleTypes.Razer)
                        rtb_debug.Invoke((Action) delegate { rtb_debug.SelectionColor = Color.LimeGreen; });
                    else if (type == ConsoleTypes.Corsair)
                        rtb_debug.Invoke((Action) delegate { rtb_debug.SelectionColor = Color.MediumVioletRed; });
                    else if (type == ConsoleTypes.Logitech)
                        rtb_debug.Invoke((Action) delegate { rtb_debug.SelectionColor = Color.DodgerBlue; });
                    else if (type == ConsoleTypes.Lifx)
                        rtb_debug.Invoke((Action) delegate { rtb_debug.SelectionColor = Color.BlueViolet; });
                    else if (type == ConsoleTypes.Arx)
                        rtb_debug.Invoke((Action) delegate { rtb_debug.SelectionColor = Color.Aqua; });
                    else if (type == ConsoleTypes.Steel)
                        rtb_debug.Invoke((Action) delegate { rtb_debug.SelectionColor = Color.HotPink; });
                    else if (type == ConsoleTypes.Coolermaster)
                        rtb_debug.Invoke((Action) delegate { rtb_debug.SelectionColor = Color.DarkBlue; });
                    else if (type == ConsoleTypes.Roccat)
                        rtb_debug.Invoke((Action) delegate { rtb_debug.SelectionColor = Color.RosyBrown; });
                    else if (type == ConsoleTypes.Wooting)
                        rtb_debug.Invoke((Action)delegate { rtb_debug.SelectionColor = Color.Green; });
                    else if (type == ConsoleTypes.Asus)
                        rtb_debug.Invoke((Action) delegate { rtb_debug.SelectionColor = Color.OrangeRed; });
                    else if (type == ConsoleTypes.Mystic)
                        rtb_debug.Invoke((Action) delegate { rtb_debug.SelectionColor = Color.SteelBlue; });
                    else if (type == ConsoleTypes.Error)
                        rtb_debug.Invoke((Action) delegate { rtb_debug.SelectionColor = Color.Red; });
                    else rtb_debug.Invoke((Action) delegate { rtb_debug.SelectionColor = Color.Black; });

                    rtb_debug.Invoke((Action) delegate { rtb_debug.AppendText(line + Environment.NewLine); });
                }
                else
                {
                    if (type == ConsoleTypes.System) rtb_debug.SelectionColor = Color.Black;
                    else if (type == ConsoleTypes.Ffxiv) rtb_debug.SelectionColor = Color.DarkCyan;
                    else if (type == ConsoleTypes.Razer) rtb_debug.SelectionColor = Color.LimeGreen;
                    else if (type == ConsoleTypes.Corsair) rtb_debug.SelectionColor = Color.MediumVioletRed;
                    else if (type == ConsoleTypes.Logitech) rtb_debug.SelectionColor = Color.DodgerBlue;
                    else if (type == ConsoleTypes.Lifx) rtb_debug.SelectionColor = Color.BlueViolet;
                    else if (type == ConsoleTypes.Arx) rtb_debug.SelectionColor = Color.Aqua;
                    else if (type == ConsoleTypes.Steel) rtb_debug.SelectionColor = Color.HotPink;
                    else if (type == ConsoleTypes.Coolermaster) rtb_debug.SelectionColor = Color.DarkBlue;
                    else if (type == ConsoleTypes.Roccat) rtb_debug.SelectionColor = Color.RosyBrown;
                    else if (type == ConsoleTypes.Wooting) rtb_debug.SelectionColor = Color.Green;
                    else if (type == ConsoleTypes.Asus) rtb_debug.SelectionColor = Color.OrangeRed;
                    else if (type == ConsoleTypes.Mystic) rtb_debug.SelectionColor = Color.SteelBlue;
                    else if (type == ConsoleTypes.Error) rtb_debug.SelectionColor = Color.Red;
                    else rtb_debug.SelectionColor = Color.Black;

                    rtb_debug.AppendText(line + Environment.NewLine);
                    rtb_debug.SelectionStart = rtb_debug.Text.Length;
                    rtb_debug.ScrollToCaret();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            MainThread();
        }

        public static bool IsAdministrator()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private void MainThread()
        {
            //Setup References
            //Watchdog.WatchdogStartup();
            if (!_debugmode)
            {
                Text = @"Chromatics " + _currentVersionX;
                lbl_about_ver.Text = @"Version " + _currentVersionX;
            }
            else
            {
                Text = @"Chromatics " + _currentVersionX + @" (BETA)";
                lbl_about_ver.Text = @"Version " + _currentVersionX + @" (BETA)";
            }

            SetupAboutText();
            Thread.CurrentThread.Priority = ThreadPriority.Lowest;
            
            //Setup Event Listeners
            FormClosing += OnFormClosing;
            Resize += ChromaticsForm_Resize;
            Application.ApplicationExit += OnApplicationExit;
            exitToolStripMenuItem.Click += exitToolStripMenuItem_Click;
            mi_effectsenable.Click += enableeffects_Click;
            mi_arxenable.Click += enablearx_Click;
            mi_showwindow.Click += showwindow_Click;
            mi_winstart.Click += mi_winstart_Click;
            mi_updatecheck.Click += mi_updatecheck_Click;

            _mGlobalHook = Hook.GlobalEvents();
            _mGlobalHook.KeyDown += Kh_KeyDown;
            _mGlobalHook.KeyUp += Kh_KeyUp;
            
            try
            {
                var lgsApp = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Logitech\\Logitech Gaming Software", false);
                if (lgsApp != null) _lgsInstall = lgsApp.GetValue("InstallDir").ToString();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            //Bind
            if (!_debugmode)
            {
                WriteConsole(ConsoleTypes.System, @"Starting Chromatics Version " + _currentVersionX);
            }
            else
            {
                WriteConsole(ConsoleTypes.System, @"Starting Chromatics Version " + _currentVersionX + " (BETA)");
            }


            //Load Functions
            LoadDevices();
            LoadChromaticsSettings();
            LoadColorMappings();


            //Check Administrator permissions
            if (!IsAdministrator())
            {
                WriteConsole(ConsoleTypes.Error,
                    @"Chromatics is not running as Administrator. Please restart with administrative privileges.");
                
                if (chk_lccauto.Checked)
                    tooltip_main.SetToolTip(gB_lcc,
                        @"Logitech Conflict Mode requires Chromatics to be run with Administrative privileges. Please restart with administrative privileges.");

                gB_lcc.Enabled = false;
                btn_ffxivcachereset.Enabled = false;
            }
            else
            {
                var enviroment = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;

                if (!ChromaticsSettings.ChromaticsSettingsMemoryCache)
                {
                    if (File.Exists(enviroment + @"/signatures-x64.json"))
                    {
                        FileSystem.DeleteFile(enviroment + @"/signatures-x64.json");
                    }

                    if (File.Exists(enviroment + @"/structures-x64.json"))
                    {
                        FileSystem.DeleteFile(enviroment + @"/structures-x64.json");
                    }
                    
                }

                if (ChromaticsSettings.ChromaticsSettingsLanguage != ChromaticsSettings.ChromaticsSettingsPreviousLanguage)
                {
                    if (File.Exists(enviroment + @"/signatures-x64.json"))
                    {
                        FileSystem.DeleteFile(enviroment + @"/signatures-x64.json");
                    }

                    if (File.Exists(enviroment + @"/structures-x64.json"))
                    {
                        FileSystem.DeleteFile(enviroment + @"/structures-x64.json");
                    }

                    if (File.Exists(enviroment + @"/actions.json"))
                    {
                        FileSystem.DeleteFile(enviroment + @"/actions.json");
                    }

                    if (File.Exists(enviroment + @"/statuses.json"))
                    {
                        FileSystem.DeleteFile(enviroment + @"/statuses.json");
                    }

                    if (File.Exists(enviroment + @"/zones.json"))
                    {
                        FileSystem.DeleteFile(enviroment + @"/zones.json");
                    }

                    WriteConsole(ConsoleTypes.Ffxiv, @"Language change detected. Clearing Cache..");
                    ChromaticsSettings.ChromaticsSettingsPreviousLanguage = ChromaticsSettings.ChromaticsSettingsLanguage;
                }

                if (!ChromaticsSettings.FirstRun)
                {
                    if (File.Exists(enviroment + @"/signatures-x64.json"))
                    {
                        FileSystem.DeleteFile(enviroment + @"/signatures-x64.json");
                    }

                    if (File.Exists(enviroment + @"/structures-x64.json"))
                    {
                        FileSystem.DeleteFile(enviroment + @"/structures-x64.json");
                    }

                    if (File.Exists(enviroment + @"/actions.json"))
                    {
                        FileSystem.DeleteFile(enviroment + @"/actions.json");
                    }

                    if (File.Exists(enviroment + @"/statuses.json"))
                    {
                        FileSystem.DeleteFile(enviroment + @"/statuses.json");
                    }

                    if (File.Exists(enviroment + @"/zones.json"))
                    {
                        FileSystem.DeleteFile(enviroment + @"/zones.json");
                    }

                    ChromaticsSettings.FirstRun = true;
                    SaveChromaticsSettings(0);
                }
            }
            
            //Check Updater
            try
            {
                string enviroment = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
                if (!File.Exists(enviroment + @"/updater.exe"))
                {
                    if (File.Exists(enviroment + @"/_updater.exe"))
                    {
                        FileSystem.RenameFile(enviroment + @"/_updater.exe", "updater.exe");
                        WriteConsole(ConsoleTypes.System, @"Updated Chromatics Updater to latest version.");
                    }
                }
                else
                {
                    if (File.Exists(enviroment + @"/_updater.exe"))
                    {
                        File.Delete(enviroment + @"/updater.exe");
                        FileSystem.RenameFile(enviroment + @"/_updater.exe", "updater.exe");
                        WriteConsole(ConsoleTypes.System, @"Updated Chromatics Updater to latest version.");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            //Setup GUI
            InitDeviceDataGrid();
            InitColorMappingGrid();
            InitSettingsGui();
            SetupTooltips();
            CenterPictureBox(pB_logo1, pB_logo1.Image);
            notify_master.ContextMenuStrip = contextMenuStrip1;
            
            Localization.SetKeyRegion(ChromaticsSettings.ChromaticsSettingsQwertyMode);
            
            try
            {
                if (ChromaticsSettings.ChromaticsSettingsDebugOpt)
                {
                    AutoMeasurement.Instance = new WinFormAutoMeasurement();
                    AutoMeasurement.Start(new MeasurementConfiguration("UA-92921709-2", "Chromatics", _currentVersionX));
                }
            }
            catch (Exception ex)
            {
                WriteConsole(ConsoleTypes.Error, @"Unable to hook debug reporting. Ex: " + ex.Message);
            }

            if (ChromaticsSettings.ChromaticsSettingsDesktopNotifications)
            {
                notify_master.BalloonTipText = @"Chromatics will automatically attach to Final Fantasy XIV";
                notify_master.ShowBalloonTip(2000);
            }

            new Task(() => { CheckUpdates(0); }).Start();

            //Setup Device Interfaces
            InitializeSdk();
            InitDevicesGui();
            SaveChromaticsSettings(0);

            if (LogitechSdkCalled == 1 && Process.GetProcessesByName("LCore").Length > 0)
            {
                if (gB_lcc.Enabled)
                {
                    //Check Logitech Enviroment
                    try
                    {
                        if (File.Exists(_lgsInstall + @"\SDK\LED\x64\LogitechLed.dll"))
                        {
                            if (chk_lccauto.Checked)
                            {
                                ToggleLccMode(true);

                                chk_lccenable.CheckedChanged -= chk_lccenable_CheckedChanged;
                                chk_lccenable.Checked = true;
                                chk_lccenable.CheckedChanged += chk_lccenable_CheckedChanged;
                            }
                            else
                            {
                                WriteConsole(ConsoleTypes.Error,
                                    @"Logitech: Chromatics has detected that the LGS internal SDK library is causing a conflict between FFXIV and Chromatics. Please make sure to enable 'Logitech Conflict Mode' under the settings tab and check that 'LED Illumination' is disabled for 'ffxiv_dx11' within LGS.");
                            }
                        }
                        else
                        {
                            WriteConsole(ConsoleTypes.Logitech, @"Logitech Conflict Mode is already enabled.");

                            chk_lccenable.CheckedChanged -= chk_lccenable_CheckedChanged;
                            chk_lccenable.Checked = true;
                            chk_lccenable.CheckedChanged += chk_lccenable_CheckedChanged;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);

                        if (chk_lccauto.Checked)
                        {
                            WriteConsole(ConsoleTypes.Error,
                                @"Logitech Conflict Mode failed to automatically start. Error: " + ex.Message);

                            chk_lccenable.CheckedChanged -= chk_lccenable_CheckedChanged;
                            chk_lccenable.Checked = false;
                            chk_lccenable.CheckedChanged += chk_lccenable_CheckedChanged;
                        }
                    }
                }
                else
                {
                    if (File.Exists(_lgsInstall + @"\SDK\LED\x64\LogitechLed.dll"))
                        WriteConsole(ConsoleTypes.Error,
                            "Logitech: Chromatics has detected that the LGS internal SDK library is causing a conflict between FFXIV and Chromatics. Please make sure to enable 'Logitech Conflict Mode' under the Devices > Advanced tab and check that 'LED Illumination' is disabled for 'ffxiv_dx11' within LGS.");
                }
            }
            else
            {
                if (Process.GetProcessesByName("lghub").Length == 0)
                {
                    tooltip_main.SetToolTip(gB_lcc,
                        "Logitech SDK not loaded. Please open LGS Software and restart Chromatics as Administrator.");
                }

                gB_lcc.Enabled = false;
            }


            //Setup LCD Interfaces

            if (ChromaticsSettings.ChromaticsSettingsLcdEnabled)
            {
                _lcd = LogitechLcdInterface.InitializeLcdSdk();

                if (_lcd != null)
                {
                    LcdSdk = true;
                    LcdSdkCalled = 1;
                    WriteConsole(ConsoleTypes.Logitech, @"LCD SDK Loaded");
                }
            }


            if (chk_arxtoggle.Checked)
            {
                _arx = LogitechArxInterface.InitializeArxSdk();

                if (_arx != null)
                {
                    ArxSdk = true;
                    ArxState = 0;
                    ArxSdkCalled = 1;
                    WriteConsole(ConsoleTypes.Arx, @"ARX SDK Loaded");

                    //Load Plugins
                    LoadArxPlugins();
                }
            }

            
            //Finish GUI Setup
            InitSettingsArxGui();
            Startup = true;

            //Ambience
            /*
            if (IsAdministrator())
                AmbienceInterface.StartAmbience();
            */
            
            var gameLanguage = "English";

            switch (ChromaticsSettings.ChromaticsSettingsLanguage)
            {
                case 0:
                    //English
                    gameLanguage = "English";
                    break;
                case 1:
                    //Chinese
                    gameLanguage = "Chinese";
                    break;
                case 2:
                    //Japanese
                    gameLanguage = "Japanese";
                    break;
                case 3:
                    //French
                    gameLanguage = "French";
                    break;
                case 4:
                    //German
                    gameLanguage = "German";
                    break;
                case 5:
                    //Korean
                    gameLanguage = "Korean";
                    break;
            }

            WriteConsole(ConsoleTypes.Ffxiv, @"Game Language set to " + gameLanguage + @".");

            //Split off MemoryReader to a Task 
            
            MemoryTask = new Task(() =>
            {
                _call = CallFfxivAttach(_attachcts.Token);
            }, _memoryTask.Token);

            MemoryTasks.Add(MemoryTask);
            MemoryTasks.Run(MemoryTask);


            /*
            var ffxivThread = new Thread(new ThreadStart(CallThreadFFXIVAttach));
            ffxivThread.Start();
            ffxivThread.Join();
            */
        }

        private void LoadArxPlugins()
        {
            if (ArxSdk && ArxSdkCalled == 1)
            {
                //Load Plugins
                if (Plugs.Count > 0)
                {
                    foreach (var plugin in Plugs)
                        if (cb_arx_mode.Items.Contains(plugin))
                            cb_arx_mode.Items.Remove(plugin);

                    Plugs.Clear();
                }

                Plugs = _arx.LoadPlugins();
                if (Plugs.Count > 0)
                    foreach (var plug in Plugs)
                    {
                        cb_arx_mode.Items.Add(plug);
                        WriteConsole(ConsoleTypes.Arx, plug + " Plugin Loaded.");
                    }
                else
                    WriteConsole(ConsoleTypes.Arx, @"No Plugins Found.");
            }
        }

        private async Task CallFfxivAttach(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                if (_gameStop)
                {
                    await Task.Delay(2500, ct);
                    AttachFfxiv();
                }
                else
                {
                    await Task.Delay(5000, ct);
                }
            }
        }

        private void AttachFfxiv()
        {
            //Debug.WriteLine("Debug trip B");

            if (InitiateMemory())
            {
                //Console.WriteLine("Attached");
                //rtb_debug.Invoke((Action)delegate { rtb_debug.AppendText("Attached" + Environment.NewLine); });
                WriteConsole(ConsoleTypes.Ffxiv, "Attached to FFXIV");

                if (ChromaticsSettings.ChromaticsSettingsReleaseDevices)
                {
                    ReInitializeSdk();
                }
                
                _gameNotify = false;
                notify_master.Text = @"Attached to FFXIV";
                _gameStop = false;

                if (ChromaticsSettings.ChromaticsSettingsDesktopNotifications)
                {
                    notify_master.BalloonTipText = @"Attached to FFXIV";
                    notify_master.ShowBalloonTip(1500);
                }

                if (ArxSdkCalled == 1 && ArxState == 0)
                    _arx.ArxUpdateInfo("Attached to FFXIV");

                if (LcdSdkCalled == 1)
                {
                    _lcd.StatusLCDInfo(@"Attached to FFXIV");
                }

                Attatched = 1;
                SetFormName(@"Chromatics " + _currentVersionX + @" (Attached)");

                //_attachcts.Cancel();
                
                MemoryTasks.Remove(MemoryTask);
                MemoryTask = null;
                _memoryTask = new CancellationTokenSource();
                _ffxiVcts = new CancellationTokenSource();

                //_call = CallFfxivMemory(_ffxiVcts.Token);

                MemoryTask = new Task(() =>
                {
                    _call = CallFfxivMemory(_ffxiVcts);
                }, _memoryTask.Token, TaskCreationOptions.LongRunning);

                MemoryTasks.Add(MemoryTask);
                MemoryTasks.Run(MemoryTask);
                
                
            }
            else
            {
                if (!_gameNotify)
                {
                    WriteConsole(ConsoleTypes.Ffxiv, "Waiting for Game Launch..");
                    notify_master.Text = @"Waiting for Game Launch..";

                    if (LcdSdkCalled == 1)
                    {
                        _lcd.StatusLCDInfo(@"Waiting for Game Launch..");
                    }

                    if (ArxSdkCalled == 1 && ArxState == 0)
                        _arx.ArxUpdateInfo("Waiting for Game Launch..");

                    SetFormName(@"Chromatics " + _currentVersionX);

                    _gameNotify = true;
                }
            }
        }

        public void RestartServices()
        {
            if (InvokeRequired)
            {
                BlinkDelegate del = RestartServices;
                Invoke(del);
            }
            else
            {
                //Watchdog.WatchdogGo();
                _call = CallFfxivMemory(_ffxiVcts);
            }
        }

        private void CheckUpdates(int notify)
        {
            try
            {
                var currentVersion = _currentVersionX.Replace(".", string.Empty);

                if (_debugmode)
                {
                    currentVersion = (Convert.ToInt32(currentVersion) - 1).ToString();
                }

                string newVersion;
                var webRequest = WebRequest.Create(@"https://chromaticsffxiv.com/chromatics2/update/version.txt");

                using (var response = webRequest.GetResponse())
                using (var content = response.GetResponseStream())
                using (var reader = new StreamReader(content))
                {
                    var newVersionA = reader.ReadToEnd();
                    newVersion = newVersionA.Replace(".", string.Empty);
                }

                var cV = int.Parse(currentVersion);
                var nV = int.Parse(newVersion);

                if (nV > cV)
                {
                    WriteConsole(ConsoleTypes.System, @"There is an updated version of Chromatics available.");
                    var result = MessageBox.Show(@"There is an updated version of Chromatics. Update it now?",
                        @"New Version", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (result == DialogResult.Yes)
                    {
                        var updatedFile = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
                        var startInfo = new ProcessStartInfo
                        {
                            FileName = updatedFile + @"\updater.exe",
                            Arguments = "\"" + updatedFile + "\""
                        };

                        if (LogitechSdkCalled == 1)
                            ToggleLccMode(false, true);

                        Process.Start(startInfo);
                        Process.GetCurrentProcess().Kill();
                    }
                }
                else
                {
                    WriteConsole(ConsoleTypes.System, @"No new updates currently available.");

                    if (notify != 1) return;
                    if (ChromaticsSettings.ChromaticsSettingsDesktopNotifications)
                    {
                        notify_master.BalloonTipText = @"No new updates currently available.";
                        notify_master.ShowBalloonTip(2000);
                    }
                }
            }
            catch (Exception ex)
            {
                WriteConsole(ConsoleTypes.Error, @"Unable to check for updates (Error: " + ex.Message + @").");
                if (notify == 1)
                {
                    if (ChromaticsSettings.ChromaticsSettingsDesktopNotifications)
                    {
                        notify_master.BalloonTipText = @"Unable to check for updates (Error: " + ex.Message + @").";
                        notify_master.ShowBalloonTip(2000);
                    }
                }
            }
        }

        private delegate void BlinkDelegate();

        
    }

    public static class ExceptionExtensions
    {
        public static Exception GetOriginalException(this Exception ex)
        {
            if (ex.InnerException == null) return ex;

            return ex.InnerException.GetOriginalException();
        }
    }

    /// <summary>
    ///     Interface used for writing to the Console or debug output in application.
    ///     Additionally accesses GUI functions for LIFX methods.
    /// </summary>
    internal interface ILogWrite
    {
        /// <summary>
        ///     Writes to the console a line of the given type.
        /// </summary>
        void WriteConsole(ConsoleTypes type, string line);

        /// <summary>
        ///     Access the Save Devices method for LIFX functions.
        /// </summary>
        void SaveDevices();

        /// <summary>
        ///     Refresh the Device tab for LIFX functions.
        /// </summary>
        void ResetDeviceDataGrid();

        void FfxivGameStop();
    }
}