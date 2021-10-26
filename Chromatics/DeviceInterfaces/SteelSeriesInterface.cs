﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Chromatics.Controllers;
using Chromatics.DeviceInterfaces.EffectLibrary;
using Chromatics.DeviceInterfaces.SteelSeriesLibs;
using Chromatics.FFXIVInterfaces;
using GalaSoft.MvvmLight.Ioc;

namespace Chromatics.DeviceInterfaces
{
    public class SteelSeriesInterface
    {
        public static SteelLib InitializeSteelSdk()
        {
            SteelLib steel = null;
            if (Process.GetProcessesByName("SteelSeriesEngine3").Length > 0 || Process.GetProcessesByName("SteelSeriesEngine").Length > 0)
            {
                steel = new SteelLib();
                var result = steel.InitializeLights();

                if (!result)
                    return null;

            }

            return steel;
        }
    }

    public class SteelSeriesSdkWrapper
    {
        //

        #region Enums

        public enum SteelSeriesKeyCodes
        {
            LOGO = 0x00,
            SS_KEY = 0xEF,
            G0 = 0xE8,
            G1 = 0xE9,
            G2 = 0xEA,
            G3 = 0xEB,
            G4 = 0xEC,
            G5 = 0xED,
        };

        #endregion
    }

    public interface ISteelSdk
    {
        bool InitializeLights();
        void DeviceUpdate();
        void Shutdown();
        void ResetSteelSeriesDevices(bool deviceKeyboard, bool deviceMouse, bool deviceHeadset, Color basecol);
        void SetAllLights(Color col);
        void SetLights(Color col);
        void ApplyMapSingleLighting(Color col);
        void ApplyMapMultiLighting(Color col, string region);
        void ApplyMapMultiLightingInterpolate(Color empty, Color full, int pos, int z1, int min, int max, int current, bool reverse);
        void ApplyMapKeyLighting(string key, Color col, bool clear, [Optional] bool bypasswhitelist);
        void ApplyMapMouseLighting(string key, Color col);
        void ApplyMapHeadsetLighting(Color col);
        Task Ripple1(Color burstcol, int speed, Color baseColor);
        Task Ripple2(Color burstcol, int speed);
        Task MultiRipple1(Color burstcol, int speed);
        Task MultiRipple2(Color burstcol, int speed);
        void Flash1(Color burstcol, int speed, string[] regions);
        void Flash2(Color burstcol, int speed, CancellationToken cts, string[] regions);
        void Flash3(Color burstcol, int speed, CancellationToken cts);
        void Flash4(Color burstcol, int speed, CancellationToken cts, string[] regions);
        void SingleFlash1(Color burstcol, int speed, string[] regions);
        void SingleFlash2(Color burstcol, int speed, CancellationToken cts, string[] regions);
        void SingleFlash4(Color burstcol, int speed, CancellationToken cts, string[] regions);
        void ParticleEffect(Color[] toColor, string[] regions, uint interval, CancellationTokenSource cts, int speed = 50);
        void CycleEffect(int interval, CancellationTokenSource token);
        Color GetCurrentKeyColor(string key);
        Color GetCurrentMouseColor(string region);
    }

    public class SteelLib : ISteelSdk
    {
        private static readonly ILogWrite Write = SimpleIoc.Default.GetInstance<ILogWrite>();
        private readonly object action_lock = new object();

        private string devicename = "SteelSeries";
        private GameSenseSDK gameSenseSDK = new GameSenseSDK();
        private bool isInitialized;
        private bool keyboard_updated;
        private bool MouseUpdated;
        private bool HeadsetUpdated;

        private Dictionary<byte, Color> prevKeyboard = new Dictionary<byte, Color>();
        private List<byte> KeyboardHIDs = new List<byte>();
        private Dictionary<int, Color> KeyboardZones = new Dictionary<int, Color>();
        private Color prevMouseScroll = Color.Black;
        private Color prevMouseFront = Color.Black;
        private Color prevMouseLogo = Color.Black;
        private Color prevHeadset = Color.Black;

        private Stopwatch watch = new Stopwatch();
        private Stopwatch keepaliveTimer = new Stopwatch();
        private long lastUpdateTime = 0;

        private bool _steelKeyboard = true;
        private bool _steelMouse = true;
        private bool _steelHeadset = true;

        private static readonly object SteelRipple1 = new object();
        private static readonly object SteelRipple2 = new object();
        private static readonly object SteelFlash1 = new object();
        private static int _steelFlash2Step;
        private static bool _steelFlash2Running;
        private static Dictionary<string, Color> _flashpresets = new Dictionary<string, Color>();
        private static readonly object SteelFlash2 = new object();
        private static int _steelFlash3Step;
        private static bool _steelFlash3Running;
        private static readonly object SteelFlash3 = new object();
        private static int _steelFlash4Step;
        private static bool _steelFlash4Running;
        private static Dictionary<string, Color> _flashpresets4 = new Dictionary<string, Color>();
        private static readonly object SteelFlash4 = new object();

        private static Dictionary<int, string> _multiZones = new Dictionary<int, string>
        {
            {0, "zone1" },
            {1, "zone2" },
            {2, "zone3" },
            {3, "zone4" },
            {4, "zone5" },
        };
        
        public bool InitializeLights()
        {
            lock (action_lock)
            {
                if (!isInitialized)
                {
                    try
                    {
                        gameSenseSDK.init("CHROMATICS", "Final Fantasy XIV", 5);

                        //First Time

                        var hidsA = Helpers.EnumUtil.GetValues<USBHIDCodes>();
                        var hidsB = Helpers.EnumUtil.GetValues<SteelSeriesSdkWrapper.SteelSeriesKeyCodes>();

                        foreach (var hid in hidsA)
                        {
                            if (!prevKeyboard.ContainsKey((byte)hid))
                            {
                                prevKeyboard.Add((byte)hid, Color.Black);
                            }

                            if (!KeyboardHIDs.Contains((byte)hid))
                            {
                                KeyboardHIDs.Add((byte)hid);
                            }
                        }

                        foreach (var hid in hidsB)
                        {
                            if (!prevKeyboard.ContainsKey((byte)hid))
                            {
                                prevKeyboard.Add((byte)hid, Color.Black);
                            }

                            if (!KeyboardHIDs.Contains((byte)hid))
                            {
                                KeyboardHIDs.Add((byte)hid);
                            }
                        }

                        KeyboardZones.Add(0, Color.Black);
                        KeyboardZones.Add(1, Color.Black);
                        KeyboardZones.Add(2, Color.Black);
                        KeyboardZones.Add(3, Color.Black);
                        KeyboardZones.Add(4, Color.Black);
                        KeyboardZones.Add(5, Color.Black);

                        /*
                        if (!heartbeatThread.IsAlive)
                        {
                            heartbeatThread = new Thread(() => StartupB(port, path));
                            heartbeatThread.Start();
                        }
                        */

                        if (Process.GetProcessesByName("Discord").Length > 0)
                        {
                            Write.WriteConsole(ConsoleTypes.Steel, @"Note: Please disable the Discord profile in Engine Apps.");
                        }

                        isInitialized = true;
                        return true;
                    }
                    catch (Exception ex)
                    {
                        if (ex.Message.Contains("System.Net.WebException"))
                        {
                            Write.WriteConsole(ConsoleTypes.Steel, @"Cannot detect any SteelSeries devices connected. Please connect a device and restart Chromatics.");
                        }

                        Write.WriteConsole(ConsoleTypes.Steel, @"SteelSeries GameSense SDK failed to load. EX: " + ex);

                        isInitialized = false;
                        return false;
                    }
                }

                return isInitialized;
            }
        }

        public void Shutdown()
        {
            lock (action_lock)
            {
                try
                {
                    if (isInitialized)
                    {
                        Reset();
                        //GameSenseSDK.sendStop(); doesn't work atm so just wait for timeout=15sec
                        isInitialized = false;
                    }
                }
                catch (Exception ex)
                {
                    Write.WriteConsole(ConsoleTypes.Steel, @"There was an error shutting down SteelSeries GameSense SDK. EX: " + ex);
                    isInitialized = false;
                }

                if (keepaliveTimer.IsRunning)
                    keepaliveTimer.Stop();
            }
        }

        public void ResetSteelSeriesDevices(bool deviceKeyboard, bool deviceMouse, bool deviceHeadset, Color basecol)
        {
            _steelKeyboard = deviceKeyboard;
            _steelMouse = deviceMouse;
            _steelHeadset = deviceHeadset;
        }
        
        public void SetAllLights(Color col)
        {
            if (!isInitialized) return;
            
            keyboard_updated = false;
            MouseUpdated = false;
            HeadsetUpdated = false;

            try
            {
                SendKeepalive();

                if (_steelKeyboard)
                {
                    List<byte> hids = new List<byte>();
                    List<Tuple<byte, byte, byte>> colors = new List<Tuple<byte, byte, byte>>();

                    foreach (var hid in KeyboardHIDs)
                    {
                        if (prevKeyboard.ContainsKey(hid) && prevKeyboard[hid] != col)
                        {
                            hids.Add(hid);
                            colors.Add(Tuple.Create(col.R, col.G, col.B));
                            prevKeyboard[hid] = col;
                        }
                    }

                    gameSenseSDK.setKeyboardColors(hids, colors);
                    keyboard_updated = true;
                }

                if (_steelMouse)
                {
                    gameSenseSDK.setMouseColor(col.R, col.G, col.B);
                    gameSenseSDK.setMouseLogoColor(col.R, col.G, col.B);
                    gameSenseSDK.setMouseScrollWheelColor(col.R, col.G, col.B);
                    prevMouseScroll = col;
                    prevMouseFront = col;
                    prevMouseLogo = col;
                    MouseUpdated = true;
                }

                if (_steelHeadset)
                {
                    gameSenseSDK.setHeadsetColor(col.R, col.G, col.B);
                    prevHeadset = col;
                    HeadsetUpdated = true;
                }
            }
            catch (Exception ex)
            {
                Write.WriteConsole(ConsoleTypes.Steel, @"SteelSeries GameSense SDK, error when updating devices. EX: " + ex);
            }
        }

        public void DeviceUpdate()
        {
            if (!isInitialized) return;

            try
            {
                SendKeepalive();

                if (_steelKeyboard)
                {
                    var hids = new List<byte>();
                    var cols = new List<Tuple<byte, byte, byte>>();

                    foreach (var updates in prevKeyboard)
                    {
                        hids.Add(updates.Key);
                        cols.Add(Tuple.Create(updates.Value.R, updates.Value.G, updates.Value.B));
                    }

                    gameSenseSDK.setKeyboardColors(hids, cols);
                    keyboard_updated = true;
                }
            }
            catch (Exception ex)
            {
                Write.WriteConsole(ConsoleTypes.Steel,
                    "SteelSeries GameSense SDK, error when updating keyboard. EX: " + ex);
            }
        }

        public void SetLights(Color col)
        {
            if (!isInitialized) return;

            keyboard_updated = false;

            try
            {
                SendKeepalive();

                if (_steelKeyboard)
                {
                    List<byte> hids = new List<byte>();
                    List<Tuple<byte, byte, byte>> colors = new List<Tuple<byte, byte, byte>>();

                    foreach (var hid in KeyboardHIDs)
                    {
                        if (prevKeyboard.ContainsKey(hid) && prevKeyboard[hid] != col)
                        {
                            hids.Add(hid);
                            colors.Add(Tuple.Create(col.R, col.G, col.B));
                            prevKeyboard[hid] = col;
                        }
                    }

                    //Console.WriteLine(@"Tag");
                    //gameSenseSDK.setKeyboardColors(hids, colors);
                    keyboard_updated = true;
                }
            }
            catch (Exception ex)
            {
                Write.WriteConsole(ConsoleTypes.Steel,
                    "SteelSeries GameSense SDK, error when updating keyboard. EX: " + ex);
            }
        }

        public void ApplyMapSingleLighting(Color col)
        {
            if (!isInitialized) return;

            keyboard_updated = false;

            try
            {
                SendKeepalive();

                if (_steelKeyboard)
                {
                    if (KeyboardZones[0] == col) return;

                    gameSenseSDK.sendColor("periph", col.R, col.G, col.B);
                    keyboard_updated = true;
                    KeyboardZones[0] = col;
                }
            }
            catch (Exception ex)
            {
                Write.WriteConsole(ConsoleTypes.Steel,
                    "SteelSeries GameSense SDK, error when updating keyboard (single). EX: " + ex);
            }
        }

        public void ApplyMapMultiLightingInterpolate(Color empty, Color full, int pos, int z1, int min, int max,
            int current, bool reverse)
        {
            if (!isInitialized) return;

            keyboard_updated = false;

            try
            {
                if (!_steelKeyboard) return;
                SendKeepalive();

                var cc_empty = reverse ? full : empty;
                var cc_full = reverse ? empty : full;

                var x1 = 0;
                var Z1Interpolate1 = Helpers.FFXIVInterpolation.Interpolate_Int(current, min, max, reverse ? x1 : z1, reverse ? z1 : x1);

                if (Z1Interpolate1 <= _multiZones.Count)
                {
                    for (var a1 = x1; a1 < z1; a1++)
                    {
                        if (_multiZones.ContainsKey(a1))
                        {
                            if (a1 <= Z1Interpolate1)
                            {
                                if (KeyboardZones[a1] != cc_full)
                                {
                                    gameSenseSDK.sendColor(_multiZones[a1], cc_full.R, cc_full.G, cc_full.B);
                                    keyboard_updated = true;
                                    KeyboardZones[a1] = cc_full;
                                }
                            }
                            else
                            {
                                if (KeyboardZones[a1] != cc_empty)
                                {
                                    gameSenseSDK.sendColor(_multiZones[a1], cc_empty.R, cc_empty.G, cc_empty.B);
                                    keyboard_updated = true;
                                    KeyboardZones[a1] = cc_empty;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Write.WriteConsole(ConsoleTypes.Steel,
                    "SteelSeries GameSense SDK, error when updating keyboard (multi). EX: " + ex);
            }
        }

        public void ApplyMapMultiLighting(Color col, string region)
        {
            if (!isInitialized) return;

            keyboard_updated = false;

            try
            {
                SendKeepalive();

                if (_steelKeyboard)
                {
                    for (var i = 0; i < _multiZones.Count; i++)
                    {
                        if (KeyboardZones[i] != col)
                        {
                            gameSenseSDK.sendColor(_multiZones[i], col.R, col.G, col.B);
                            keyboard_updated = true;
                            KeyboardZones[i] = col;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Write.WriteConsole(ConsoleTypes.Steel,
                    "SteelSeries GameSense SDK, error when updating keyboard (multi). EX: " + ex);
            }
        }

        public void ApplyMapKeyLighting(string key, Color col, bool clear, bool bypasswhitelist = false)
        {
            if (!isInitialized) return;

            keyboard_updated = false;
            if (!_steelKeyboard) return;

            try
            {
                SendKeepalive();
                
                var keyid = GetHIDCode(key);
                if (!prevKeyboard.ContainsKey(keyid) || prevKeyboard[keyid] == col) return;


                var hids = new List<byte>();
                var colors = new List<Tuple<byte, byte, byte>>();
                
                hids.Add(keyid);
                colors.Add(Tuple.Create(col.R, col.G, col.B));

                if (hids.Count == 0) return;

                if (clear)
                {
                    gameSenseSDK.setKeyboardColors(hids, colors);
                    keyboard_updated = true;
                }
                
                prevKeyboard[keyid] = col;
                
            }
            catch (Exception ex)
            {
                Write.WriteConsole(ConsoleTypes.Steel, @"SteelSeries GameSense SDK, error when updating key. EX: " + ex);
            }
            
        }

        public Color GetCurrentKeyColor(string key)
        {
            if (!isInitialized) return Color.Black;
            if (!_steelKeyboard) return Color.Black;

            var keyid = GetHIDCode(key);
            if (!prevKeyboard.ContainsKey(keyid)) return Color.Black;

            return prevKeyboard[keyid];
        }

        public void ApplyMapMouseLighting(string key, Color col)
        {
            if (!isInitialized) return;

            MouseUpdated = false;
            if (!_steelMouse) return;

            try
            {
                SendKeepalive();

                switch (key)
                {
                    case "All":
                        if (prevMouseFront != col)
                        {
                            gameSenseSDK.setMouseColor(col.R, col.G, col.B);
                            MouseUpdated = true;
                            prevMouseFront = col;
                        }

                        if (prevMouseScroll != col)
                        {
                            gameSenseSDK.setMouseScrollWheelColor(col.R, col.G, col.B);
                            MouseUpdated = true;
                            prevMouseScroll = col;
                        }

                        if (prevMouseLogo != col)
                        {
                            gameSenseSDK.setMouseLogoColor(col.R, col.G, col.B);
                            MouseUpdated = true;
                            prevMouseLogo = col;
                        }

                        break;
                    case "MouseFront":
                        if (prevMouseFront != col)
                        {
                            gameSenseSDK.setMouseColor(col.R, col.G, col.B);
                            MouseUpdated = true;
                            prevMouseFront = col;
                        }
                        
                        break;
                    case "MouseScroll":
                        if (prevMouseScroll != col)
                        {
                            gameSenseSDK.setMouseScrollWheelColor(col.R, col.G, col.B);
                            MouseUpdated = true;
                            prevMouseScroll = col;
                        }
                        
                        break;
                    case "MouseLogo":
                        if (prevMouseLogo != col)
                        {
                            gameSenseSDK.setMouseLogoColor(col.R, col.G, col.B);
                            MouseUpdated = true;
                            prevMouseLogo = col;
                        }
                        
                        break;
                }
            }
            catch (Exception ex)
            {
                Write.WriteConsole(ConsoleTypes.Steel, @"SteelSeries GameSense SDK, error when updating mouse. EX: " + ex);
            }
            
        }
        public Color GetCurrentMouseColor(string region)
        {
            if (!isInitialized) return Color.Black;
            if (!_steelMouse) return Color.Black;

            switch (region)
            {
                case "MouseFront":
                    return prevMouseFront;
                case "MouseScroll":
                    return prevMouseScroll;
                case "MouseLogo":
                    return prevMouseLogo;
                default:
                    return Color.Black;
            }
        }

        public void ApplyMapHeadsetLighting(Color col)
        {
            if (!isInitialized) return;

            HeadsetUpdated = false;
            if (!_steelHeadset) return;

            try
            {
                SendKeepalive();

                if (prevHeadset == col) return;
                gameSenseSDK.setHeadsetColor(col.R, col.G, col.B);
                HeadsetUpdated = true;
                prevHeadset = col;
            }
            catch (Exception ex)
            {
                Write.WriteConsole(ConsoleTypes.Steel, @"SteelSeries GameSense SDK, error when updating headset. EX: " + ex);
            }
            
        }

        public Task Ripple1(Color burstcol, int speed, Color baseColor)
        {
            return new Task(() =>
            {
                if (!isInitialized || !_steelKeyboard)
                    return;

                var presets = new Dictionary<string, Color>();
                List<byte> hids = new List<byte>();
                List<Tuple<byte, byte, byte>> colors = new List<Tuple<byte, byte, byte>>();

                for (var i = 0; i <= 9; i++)
                {
                    if (i == 0)
                    {
                        //Setup

                        foreach (var key in DeviceEffects.GlobalKeys)
                            try
                            {
                                if (prevKeyboard.ContainsKey(GetHIDCode(key)))
                                {
                                    presets.Add(key, prevKeyboard[GetHIDCode(key)]);
                                }
                            }
                            catch (Exception ex)
                            {
                                Write.WriteConsole(ConsoleTypes.Error, @"(" + key + "): " + ex.Message);
                            }
                    }
                    else if (i == 1)
                    {
                        //Step 0
                        foreach (var key in DeviceEffects.GlobalKeys)
                        {
                            var pos = Array.IndexOf(DeviceEffects.PulseOutStep0, key);
                            //ApplyMapKeyLighting(key, pos > -1 ? burstcol : presets[key], true);
                            if (pos > -1)
                            {
                                hids.Add(GetHIDCode(key));
                                colors.Add(Tuple.Create(burstcol.R, burstcol.G, burstcol.B));
                            }
                            else
                            {
                                hids.Add(GetHIDCode(key));
                                colors.Add(Tuple.Create(presets[key].R, presets[key].G, presets[key].B));
                            }
                        }
                    }
                    else if (i == 2)
                    {
                        //Step 1
                        foreach (var key in DeviceEffects.GlobalKeys)
                        {
                            var pos = Array.IndexOf(DeviceEffects.PulseOutStep1, key);
                            //ApplyMapKeyLighting(key, pos > -1 ? burstcol : presets[key], true);
                            if (pos > -1)
                            {
                                hids.Add(GetHIDCode(key));
                                colors.Add(Tuple.Create(burstcol.R, burstcol.G, burstcol.B));
                            }
                            else
                            {
                                hids.Add(GetHIDCode(key));
                                colors.Add(Tuple.Create(presets[key].R, presets[key].G, presets[key].B));
                            }
                        }
                    }
                    else if (i == 3)
                    {
                        //Step 2
                        foreach (var key in DeviceEffects.GlobalKeys)
                        {
                            var pos = Array.IndexOf(DeviceEffects.PulseOutStep2, key);
                            //ApplyMapKeyLighting(key, pos > -1 ? burstcol : presets[key], true);
                            if (pos > -1)
                            {
                                hids.Add(GetHIDCode(key));
                                colors.Add(Tuple.Create(burstcol.R, burstcol.G, burstcol.B));
                            }
                            else
                            {
                                hids.Add(GetHIDCode(key));
                                colors.Add(Tuple.Create(presets[key].R, presets[key].G, presets[key].B));
                            }
                        }
                    }
                    else if (i == 4)
                    {
                        //Step 3
                        foreach (var key in DeviceEffects.GlobalKeys)
                        {
                            var pos = Array.IndexOf(DeviceEffects.PulseOutStep3, key);
                            //ApplyMapKeyLighting(key, pos > -1 ? burstcol : presets[key], true);
                            if (pos > -1)
                            {
                                hids.Add(GetHIDCode(key));
                                colors.Add(Tuple.Create(burstcol.R, burstcol.G, burstcol.B));
                            }
                            else
                            {
                                hids.Add(GetHIDCode(key));
                                colors.Add(Tuple.Create(presets[key].R, presets[key].G, presets[key].B));
                            }
                        }
                    }
                    else if (i == 5)
                    {
                        //Step 4
                        foreach (var key in DeviceEffects.GlobalKeys)
                        {
                            var pos = Array.IndexOf(DeviceEffects.PulseOutStep4, key);
                            //ApplyMapKeyLighting(key, pos > -1 ? burstcol : presets[key], true);
                            if (pos > -1)
                            {
                                hids.Add(GetHIDCode(key));
                                colors.Add(Tuple.Create(burstcol.R, burstcol.G, burstcol.B));
                            }
                            else
                            {
                                hids.Add(GetHIDCode(key));
                                colors.Add(Tuple.Create(presets[key].R, presets[key].G, presets[key].B));
                            }
                        }
                    }
                    else if (i == 6)
                    {
                        //Step 5
                        foreach (var key in DeviceEffects.GlobalKeys)
                        {
                            var pos = Array.IndexOf(DeviceEffects.PulseOutStep5, key);
                            //ApplyMapKeyLighting(key, pos > -1 ? burstcol : presets[key], true);
                            if (pos > -1)
                            {
                                hids.Add(GetHIDCode(key));
                                colors.Add(Tuple.Create(burstcol.R, burstcol.G, burstcol.B));
                            }
                            else
                            {
                                hids.Add(GetHIDCode(key));
                                colors.Add(Tuple.Create(presets[key].R, presets[key].G, presets[key].B));
                            }
                        }
                    }
                    else if (i == 7)
                    {
                        //Step 6
                        foreach (var key in DeviceEffects.GlobalKeys)
                        {
                            var pos = Array.IndexOf(DeviceEffects.PulseOutStep6, key);
                            //ApplyMapKeyLighting(key, pos > -1 ? burstcol : presets[key], true);
                            if (pos > -1)
                            {
                                hids.Add(GetHIDCode(key));
                                colors.Add(Tuple.Create(burstcol.R, burstcol.G, burstcol.B));
                            }
                            else
                            {
                                hids.Add(GetHIDCode(key));
                                colors.Add(Tuple.Create(presets[key].R, presets[key].G, presets[key].B));
                            }
                        }
                    }
                    else if (i == 8)
                    {
                        //Step 7
                        foreach (var key in DeviceEffects.GlobalKeys)
                        {
                            var pos = Array.IndexOf(DeviceEffects.PulseOutStep7, key);
                            //ApplyMapKeyLighting(key, pos > -1 ? burstcol : presets[key], true);
                            if (pos > -1)
                            {
                                hids.Add(GetHIDCode(key));
                                colors.Add(Tuple.Create(burstcol.R, burstcol.G, burstcol.B));
                            }
                            else
                            {
                                hids.Add(GetHIDCode(key));
                                colors.Add(Tuple.Create(presets[key].R, presets[key].G, presets[key].B));
                            }
                        }
                    }
                    else if (i == 9)
                    {
                        //Spin down

                        foreach (var key in DeviceEffects.GlobalKeys)
                        {
                            hids.Add(GetHIDCode(key));
                            colors.Add(Tuple.Create(burstcol.R, burstcol.G, burstcol.B));
                            //ApplyMapKeyLighting(key, presets[key], true);
                        }

                        hids.Add(GetHIDCode("D1"));
                        colors.Add(Tuple.Create(baseColor.R, baseColor.G, baseColor.B));
                        hids.Add(GetHIDCode("D2"));
                        colors.Add(Tuple.Create(baseColor.R, baseColor.G, baseColor.B));
                        hids.Add(GetHIDCode("D3"));
                        colors.Add(Tuple.Create(baseColor.R, baseColor.G, baseColor.B));
                        hids.Add(GetHIDCode("D4"));
                        colors.Add(Tuple.Create(baseColor.R, baseColor.G, baseColor.B));
                        hids.Add(GetHIDCode("D5"));
                        colors.Add(Tuple.Create(baseColor.R, baseColor.G, baseColor.B));
                        hids.Add(GetHIDCode("D6"));
                        colors.Add(Tuple.Create(baseColor.R, baseColor.G, baseColor.B));
                        hids.Add(GetHIDCode("D7"));
                        colors.Add(Tuple.Create(baseColor.R, baseColor.G, baseColor.B));
                        hids.Add(GetHIDCode("D8"));
                        colors.Add(Tuple.Create(baseColor.R, baseColor.G, baseColor.B));
                        hids.Add(GetHIDCode("D9"));
                        colors.Add(Tuple.Create(baseColor.R, baseColor.G, baseColor.B));
                        hids.Add(GetHIDCode("D0"));
                        colors.Add(Tuple.Create(baseColor.R, baseColor.G, baseColor.B));
                        hids.Add(GetHIDCode("OemMinus"));
                        colors.Add(Tuple.Create(baseColor.R, baseColor.G, baseColor.B));
                        hids.Add(GetHIDCode("OemEquals"));
                        colors.Add(Tuple.Create(baseColor.R, baseColor.G, baseColor.B));

                        /*
                        ApplyMapKeyLighting("D1", baseColor, true);
                        ApplyMapKeyLighting("D2", baseColor, true);
                        ApplyMapKeyLighting("D3", baseColor, true);
                        ApplyMapKeyLighting("D4", baseColor, true);
                        ApplyMapKeyLighting("D5", baseColor, true);
                        ApplyMapKeyLighting("D6", baseColor, true);
                        ApplyMapKeyLighting("D7", baseColor, true);
                        ApplyMapKeyLighting("D8", baseColor, true);
                        ApplyMapKeyLighting("D9", baseColor, true);
                        ApplyMapKeyLighting("D0", baseColor, true);
                        ApplyMapKeyLighting("OemMinus", baseColor, true);
                        ApplyMapKeyLighting("OemEquals", baseColor, true);
                        */

                        presets.Clear();
                    }

                    if (i < 9)
                    {
                        Thread.Sleep(speed);
                    }

                    EffectKeyUpdate(hids, colors);
                    hids.Clear();
                    colors.Clear();
                }
            });
        }

        public Task Ripple2(Color burstcol, int speed)
        {
            return new Task(() =>
            {
                if (!isInitialized || !_steelKeyboard)
                    return;

                var safeKeys = DeviceEffects.GlobalKeys.Except(FfxivHotbar.Keybindwhitelist);

                lock (SteelRipple2)
                {
                    var previousValues = new Dictionary<string, Color>();
                    var enumerable = safeKeys.ToList();
                    List<byte> hids = new List<byte>();
                    List<Tuple<byte, byte, byte>> colors = new List<Tuple<byte, byte, byte>>();

                    for (var i = 0; i <= 9; i++)
                    {

                        if (i == 0)
                        {
                            //Setup

                            foreach (var key in enumerable)
                                try
                                {
                                    if (prevKeyboard.ContainsKey(GetHIDCode(key)))
                                    {
                                        previousValues.Add(key, prevKeyboard[GetHIDCode(key)]);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Write.WriteConsole(ConsoleTypes.Error, @"(" + key + "): " + ex.Message);
                                }
                        }
                        else if (i == 1)
                        {
                            //Step 0
                            foreach (var key in enumerable)
                            {
                                var pos = Array.IndexOf(DeviceEffects.PulseOutStep0, key);
                                if (pos > -1)
                                {
                                    hids.Add(GetHIDCode(key));
                                    colors.Add(Tuple.Create(burstcol.R, burstcol.G, burstcol.B));
                                    //ApplyMapKeyLighting(key, burstcol, true);
                                }
                            }
                        }
                        else if (i == 2)
                        {
                            //Step 1
                            foreach (var key in enumerable)
                            {
                                var pos = Array.IndexOf(DeviceEffects.PulseOutStep1, key);
                                if (pos > -1)
                                {
                                    hids.Add(GetHIDCode(key));
                                    colors.Add(Tuple.Create(burstcol.R, burstcol.G, burstcol.B));
                                    //ApplyMapKeyLighting(key, burstcol, true);
                                }
                            }
                        }
                        else if (i == 3)
                        {
                            //Step 2
                            foreach (var key in enumerable)
                            {
                                var pos = Array.IndexOf(DeviceEffects.PulseOutStep2, key);
                                if (pos > -1)
                                {
                                    hids.Add(GetHIDCode(key));
                                    colors.Add(Tuple.Create(burstcol.R, burstcol.G, burstcol.B));
                                    //ApplyMapKeyLighting(key, burstcol, true);
                                }
                            }
                        }
                        else if (i == 4)
                        {
                            //Step 3
                            foreach (var key in enumerable)
                            {
                                var pos = Array.IndexOf(DeviceEffects.PulseOutStep3, key);
                                if (pos > -1)
                                {
                                    hids.Add(GetHIDCode(key));
                                    colors.Add(Tuple.Create(burstcol.R, burstcol.G, burstcol.B));
                                    //ApplyMapKeyLighting(key, burstcol, true);
                                }
                            }
                        }
                        else if (i == 5)
                        {
                            //Step 4
                            foreach (var key in enumerable)
                            {
                                var pos = Array.IndexOf(DeviceEffects.PulseOutStep4, key);
                                if (pos > -1)
                                {
                                    hids.Add(GetHIDCode(key));
                                    colors.Add(Tuple.Create(burstcol.R, burstcol.G, burstcol.B));
                                    //ApplyMapKeyLighting(key, burstcol, true);
                                }
                            }
                        }
                        else if (i == 6)
                        {
                            //Step 5
                            foreach (var key in enumerable)
                            {
                                var pos = Array.IndexOf(DeviceEffects.PulseOutStep5, key);
                                if (pos > -1)
                                {
                                    hids.Add(GetHIDCode(key));
                                    colors.Add(Tuple.Create(burstcol.R, burstcol.G, burstcol.B));
                                    //ApplyMapKeyLighting(key, burstcol, true);
                                }
                            }
                        }
                        else if (i == 7)
                        {
                            //Step 6
                            foreach (var key in enumerable)
                            {
                                var pos = Array.IndexOf(DeviceEffects.PulseOutStep6, key);
                                if (pos > -1)
                                {
                                    hids.Add(GetHIDCode(key));
                                    colors.Add(Tuple.Create(burstcol.R, burstcol.G, burstcol.B));
                                    //ApplyMapKeyLighting(key, burstcol, true);
                                }
                            }
                        }
                        else if (i == 8)
                        {
                            //Step 7
                            foreach (var key in enumerable)
                            {
                                var pos = Array.IndexOf(DeviceEffects.PulseOutStep7, key);
                                if (pos > -1)
                                {
                                    hids.Add(GetHIDCode(key));
                                    colors.Add(Tuple.Create(burstcol.R, burstcol.G, burstcol.B));
                                    //ApplyMapKeyLighting(key, burstcol, true);
                                }
                            }
                        }
                        else if (i == 9)
                        {
                            //Spin down

                            foreach (var key in previousValues.Keys)
                            {
                                hids.Add(GetHIDCode(key));
                                colors.Add(Tuple.Create(previousValues[key].R, previousValues[key].G, previousValues[key].B));
                                //ApplyMapKeyLighting(key, previousValues[key], true);
                            }

                            previousValues.Clear();
                        }

                        if (i < 9)
                        {
                            Thread.Sleep(speed);
                        }

                        EffectKeyUpdate(hids, colors);
                        hids.Clear();
                        colors.Clear();
                    }
                }
            });
        }

        public Task MultiRipple1(Color burstcol, int speed)
        {
            throw new NotImplementedException();
        }

        public Task MultiRipple2(Color burstcol, int speed)
        {
            throw new NotImplementedException();
        }

        public void Flash1(Color burstcol, int speed, string[] regions)
        {
            lock (SteelFlash1)
            {
                if (!isInitialized || !_steelKeyboard)
                    return;

                var previousValues = new Dictionary<string, Color>();
                List<byte> hids = new List<byte>();
                List<Tuple<byte, byte, byte>> colors = new List<Tuple<byte, byte, byte>>();

                for (var i = 0; i <= 8; i++)
                {

                    if (i == 0)
                    {
                        //Setup

                        foreach (var key in regions)
                        {
                            if (prevKeyboard.ContainsKey(GetHIDCode(key)))
                            {
                                previousValues.Add(key, prevKeyboard[GetHIDCode(key)]);
                            }
                        }
                    }
                    else if (i % 2 == 1)
                    {
                        //Step 1, 3, 5, 7
                        foreach (var key in regions)
                        {
                            hids.Add(GetHIDCode(key));
                            colors.Add(Tuple.Create(burstcol.R, burstcol.G, burstcol.B));
                            //ApplyMapKeyLighting(key, burstcol, true);
                        }
                    }
                    else if (i % 2 == 0)
                    {
                        //Step 2, 4, 6, 8
                        foreach (var key in regions)
                        {
                            hids.Add(GetHIDCode(key));
                            colors.Add(Tuple.Create(previousValues[key].R, previousValues[key].G, previousValues[key].B));
                            //ApplyMapKeyLighting(key, previousValues[key], true);
                        }
                    }

                    if (i < 8)
                    {
                        Thread.Sleep(speed);
                    }

                    EffectKeyUpdate(hids, colors);
                    hids.Clear();
                    colors.Clear();
                }
            }
        }

        public void Flash2(Color burstcol, int speed, CancellationToken cts, string[] regions)
        {
            try
            {
                if (!isInitialized || !_steelKeyboard)
                    return;

                lock (SteelFlash2)
                {
                    var previousValues = new Dictionary<string, Color>();
                    List<byte> hids = new List<byte>();
                    List<Tuple<byte, byte, byte>> colors = new List<Tuple<byte, byte, byte>>();

                    if (!_steelFlash2Running)
                    {
                        foreach (var key in regions)
                        {
                            if (prevKeyboard.ContainsKey(GetHIDCode(key)))
                            {
                                previousValues.Add(key, prevKeyboard[GetHIDCode(key)]);
                            }
                        }
                        
                        _steelFlash2Running = true;
                        _steelFlash2Step = 0;
                        _flashpresets = previousValues;
                    }

                    if (_steelFlash2Running)
                        while (_steelFlash2Running)
                        {
                            if (cts.IsCancellationRequested)
                                break;

                            if (_steelFlash2Step == 0)
                            {
                                foreach (var key in regions)
                                {
                                    hids.Add(GetHIDCode(key));
                                    colors.Add(Tuple.Create(burstcol.R, burstcol.G, burstcol.B));
                                    //ApplyMapKeyLighting(key, burstcol, true);
                                }
                                
                                _steelFlash2Step = 1;
                            }
                            else if (_steelFlash2Step == 1)
                            {
                                foreach (var key in regions)
                                {
                                    hids.Add(GetHIDCode(key));
                                    colors.Add(Tuple.Create(_flashpresets[key].R, _flashpresets[key].G, _flashpresets[key].B));
                                    //ApplyMapKeyLighting(key, _flashpresets[key], true);
                                }
                                
                                _steelFlash2Step = 0;
                            }

                            EffectKeyUpdate(hids, colors);
                            hids.Clear();
                            colors.Clear();
                            Thread.Sleep(speed);
                        }
                }
            }
            catch
            {
                //
            }
        }

        public void Flash3(Color burstcol, int speed, CancellationToken cts)
        {
            try
            {
                if (!isInitialized || !_steelKeyboard)
                    return;

                lock (SteelFlash3)
                {
                    //var previousValues = new Dictionary<string, Color>();
                    _steelFlash3Running = true;
                    _steelFlash3Step = 0;

                    List<byte> hids = new List<byte>();
                    List<Tuple<byte, byte, byte>> colors = new List<Tuple<byte, byte, byte>>();

                    if (_steelFlash3Running == false)
                    {
                        //
                    }
                    else
                    {
                        while (_steelFlash3Running)
                        {
                            if (cts.IsCancellationRequested)
                                break;

                            if (_steelFlash3Step == 0)
                            {
                                foreach (var key in DeviceEffects.NumFlash)
                                {
                                    if (prevKeyboard.ContainsKey(GetHIDCode(key)))
                                    {
                                        hids.Add(GetHIDCode(key));
                                        colors.Add(Tuple.Create(burstcol.R, burstcol.G, burstcol.B));
                                        //ApplyMapKeyLighting(key, burstcol, true);
                                    }
                                        
                                }
                                  

                                _steelFlash3Step = 1;
                            }
                            else if (_steelFlash3Step == 1)
                            {
                                foreach (var key in DeviceEffects.NumFlash)
                                {
                                    if (prevKeyboard.ContainsKey(GetHIDCode(key)))
                                    {
                                        hids.Add(GetHIDCode(key));
                                        colors.Add(Tuple.Create(Color.Black.R, Color.Black.G, Color.Black.B));
                                        //ApplyMapKeyLighting(key, Color.Black, true);
                                    }
                                }
                                
                                _steelFlash3Step = 0;
                            }

                            EffectKeyUpdate(hids, colors);
                            hids.Clear();
                            colors.Clear();
                            Thread.Sleep(speed);
                        }
                    }
                }
            }
            catch
            {
                //
            }
        }

        public void Flash4(Color burstcol, int speed, CancellationToken cts, string[] regions)
        {
            try
            {
                if (!isInitialized || !_steelKeyboard)
                    return;
                
                lock (SteelFlash4)
                {
                    var flashpresets = new Dictionary<string, Color>();
                    List<byte> hids = new List<byte>();
                    List<Tuple<byte, byte, byte>> colors = new List<Tuple<byte, byte, byte>>();

                    if (!_steelFlash4Running)
                    {
                        foreach (var key in regions)
                        {
                            if (prevKeyboard.ContainsKey(GetHIDCode(key)))
                                flashpresets.Add(key, prevKeyboard[GetHIDCode(key)]);
                        }
                            

                        _steelFlash4Running = true;
                        _steelFlash4Step = 0;
                        _flashpresets4 = flashpresets;
                    }

                    if (_steelFlash4Running)
                        while (_steelFlash4Running)
                        {
                            if (cts.IsCancellationRequested)
                                break;
                            
                            if (_steelFlash4Step == 0)
                            {
                                foreach (var key in regions)
                                {
                                    hids.Add(GetHIDCode(key));
                                    colors.Add(Tuple.Create(burstcol.R, burstcol.G, burstcol.B));
                                    //ApplyMapKeyLighting(key, burstcol, true);
                                }
                                
                                _steelFlash4Step = 1;
                            }
                            else if (_steelFlash4Step == 1)
                            {
                                foreach (var key in regions)
                                {
                                    hids.Add(GetHIDCode(key));
                                    colors.Add(Tuple.Create(_flashpresets4[key].R, _flashpresets4[key].G, _flashpresets4[key].B));
                                    //ApplyMapKeyLighting(key, _flashpresets4[key], true);
                                }
                                
                                _steelFlash4Step = 0;
                            }

                            EffectKeyUpdate(hids, colors);
                            hids.Clear();
                            colors.Clear();

                            Thread.Sleep(speed);
                        }
                }
            }
            catch
            {
                //
            }
        }

        public void SingleFlash1(Color burstcol, int speed, string[] regions)
        {
            throw new NotImplementedException();
        }

        public void SingleFlash2(Color burstcol, int speed, CancellationToken cts, string[] regions)
        {
            throw new NotImplementedException();
        }

        public void SingleFlash4(Color burstcol, int speed, CancellationToken cts, string[] regions)
        {
            throw new NotImplementedException();
        }

        public void ParticleEffect(Color[] toColor, string[] regions, uint interval, CancellationTokenSource cts, int speed = 50)
        {
            if (!isInitialized || !_steelKeyboard) return;
            if (cts.IsCancellationRequested) return;
            
            Dictionary<string, ColorFader> colorFaderDict = new Dictionary<string, ColorFader>();

            //Keyboard.SetCustomAsync(refreshKeyGrid);
            Thread.Sleep(500);

            while (!cts.IsCancellationRequested)
            {
                if (cts.IsCancellationRequested) break;

                var rnd = new Random();
                colorFaderDict.Clear();

                foreach (var key in regions)
                {
                    if (cts.IsCancellationRequested) return;

                    var rndCol = toColor[rnd.Next(toColor.Length)];

                    colorFaderDict.Add(key, new ColorFader(toColor[0], rndCol, interval));
                }

                var _regions = regions.OrderBy(x => rnd.Next()).ToArray();

                List<byte> hids = new List<byte>();
                List<Tuple<byte, byte, byte>> colors = new List<Tuple<byte, byte, byte>>();

                foreach (var key in _regions)
                {
                    if (cts.IsCancellationRequested) return;
                    if (!colorFaderDict.ContainsKey(key)) continue;

                    foreach (var color in colorFaderDict[key].Fade())
                    {
                        if (cts.IsCancellationRequested) return;

                        if (prevKeyboard.ContainsKey(GetHIDCode(key)))
                        {
                            hids.Add(GetHIDCode(key));
                            colors.Add(Tuple.Create(color.R, color.G, color.B));
                        }
                    }

                    EffectKeyUpdate(hids, colors);
                    hids.Clear();
                    colors.Clear();

                    Thread.Sleep(speed);
                }

            }
        }

        private readonly object lockObject = new object();
        public void CycleEffect(int interval, CancellationTokenSource token)
        {
            if (!isInitialized || !_steelKeyboard)
                return;

            List<byte> hids = new List<byte>();
            List<Tuple<byte, byte, byte>> colors = new List<Tuple<byte, byte, byte>>();

            while (true)
            {
                lock (lockObject)
                {
                    for (var x = 0; x <= 250; x += 5)
                    {
                        if (token.IsCancellationRequested) break;
                        Thread.Sleep(10);
                        foreach (var hid in KeyboardHIDs)
                        {
                            hids.Add(hid);
                            colors.Add(Tuple.Create((byte) Math.Ceiling((double) (250 * 100) / 255),
                                (byte) Math.Ceiling((double) (x * 100) / 255), (byte) 0));
                        }
                    }

                    for (var x = 250; x >= 5; x -= 5)
                    {
                        if (token.IsCancellationRequested) break;
                        Thread.Sleep(10);
                        foreach (var hid in KeyboardHIDs)
                        {
                            hids.Add(hid);
                            colors.Add(Tuple.Create((byte) Math.Ceiling((double) (x * 100) / 255),
                                (byte) Math.Ceiling((double) (250 * 100) / 255), (byte) 0));
                        }
                    }

                    for (var x = 0; x <= 250; x += 5)
                    {
                        if (token.IsCancellationRequested) break;
                        Thread.Sleep(10);
                        foreach (var hid in KeyboardHIDs)
                        {
                            hids.Add(hid);
                            colors.Add(Tuple.Create((byte) Math.Ceiling((double) (x * 100) / 255),
                                (byte) Math.Ceiling((double) (250 * 100) / 255), (byte) 0));
                        }
                    }

                    for (var x = 250; x >= 5; x -= 5)
                    {
                        if (token.IsCancellationRequested) break;
                        Thread.Sleep(10);
                        foreach (var hid in KeyboardHIDs)
                        {
                            hids.Add(hid);
                            colors.Add(Tuple.Create((byte) 0, (byte) Math.Ceiling((double) (x * 100) / 255),
                                (byte) Math.Ceiling((double) (250 * 100) / 255)));
                        }
                    }

                    for (var x = 0; x <= 250; x += 5)
                    {
                        if (token.IsCancellationRequested) break;
                        Thread.Sleep(10);
                        foreach (var hid in KeyboardHIDs)
                        {
                            hids.Add(hid);
                            colors.Add(Tuple.Create((byte) Math.Ceiling((double) (x * 100) / 255), (byte) 0,
                                (byte) Math.Ceiling((double) (250 * 100) / 255)));
                        }

                    }

                    for (var x = 250; x >= 5; x -= 5)
                    {
                        if (token.IsCancellationRequested) break;
                        Thread.Sleep(10);
                        foreach (var hid in KeyboardHIDs)
                        {
                            hids.Add(hid);
                            colors.Add(Tuple.Create((byte) Math.Ceiling((double) (250 * 100) / 255), (byte) 0,
                                (byte) Math.Ceiling((double) (x * 100) / 255)));
                        }

                    }

                    if (token.IsCancellationRequested) break;

                    EffectKeyUpdate(hids, colors);
                    hids.Clear();
                    colors.Clear();
                }
            }
            Thread.Sleep(interval);
        }

        private void EffectKeyUpdate(List<byte> hids, List<Tuple<byte, byte, byte>> cols)
        {
            if (!isInitialized) return;

            try
            {
                SendKeepalive();

                if (_steelKeyboard)
                {
                    gameSenseSDK.setKeyboardColors(hids, cols);
                    keyboard_updated = true;
                }
            }
            catch (Exception ex)
            {
                Write.WriteConsole(ConsoleTypes.Steel,
                    "SteelSeries GameSense SDK, error when updating keyboard. EX: " + ex);
            }
        }


        private void SendKeepalive(bool forced = false)
        {
            // workaround for heartbeat/keepalive events every 10sec
            if (!keepaliveTimer.IsRunning)
                keepaliveTimer.Start();

            if (keepaliveTimer.ElapsedMilliseconds > 10000 || forced)
            {
                gameSenseSDK.sendHeartbeat();
                keepaliveTimer.Restart();
            }
        }

        private string GetDeviceUpdatePerformance()
        {
            return (isInitialized ? lastUpdateTime + " ms" : "");
        }

        private string GetDeviceDetails()
        {
            if (isInitialized)
            {
                return devicename + ": Connected";
            }

            return devicename + ": Not initialized";
        }

        private string GetDeviceName()
        {
            return devicename;
        }

        private void Reset()
        {
            if (isInitialized && (keyboard_updated || MouseUpdated || HeadsetUpdated))
            {
                keyboard_updated = false;
                MouseUpdated = false;
                HeadsetUpdated = false;
            }
        }

        private static byte GetHIDCode(string key)
        {
            switch (key)
            {
                case "MouseLogo":
                    return (byte) SteelSeriesSdkWrapper.SteelSeriesKeyCodes.LOGO;
                case "Function":
                    return (byte) SteelSeriesSdkWrapper.SteelSeriesKeyCodes.SS_KEY;
                case "Macro1":
                    return (byte) SteelSeriesSdkWrapper.SteelSeriesKeyCodes.G0;
                case "Macro2":
                    return (byte) SteelSeriesSdkWrapper.SteelSeriesKeyCodes.G1;
                case "Macro3":
                    return (byte) SteelSeriesSdkWrapper.SteelSeriesKeyCodes.G2;
                case "Macro4":
                    return (byte) SteelSeriesSdkWrapper.SteelSeriesKeyCodes.G3;
                case "Macro5":
                    return (byte) SteelSeriesSdkWrapper.SteelSeriesKeyCodes.G4;
                case "Macro6":
                    return (byte) SteelSeriesSdkWrapper.SteelSeriesKeyCodes.G5;
                case "Escape":
                    return (byte) USBHIDCodes.ESC;
                case "F1":
                    return (byte) USBHIDCodes.F1;
                case "F2":
                    return (byte) USBHIDCodes.F2;
                case "F3":
                    return (byte) USBHIDCodes.F3;
                case "F4":
                    return (byte) USBHIDCodes.F4;
                case "F5":
                    return (byte) USBHIDCodes.F5;
                case "F6":
                    return (byte) USBHIDCodes.F6;
                case "F7":
                    return (byte) USBHIDCodes.F7;
                case "F8":
                    return (byte) USBHIDCodes.F8;
                case "F9":
                    return (byte) USBHIDCodes.F9;
                case "F10":
                    return (byte) USBHIDCodes.F10;
                case "F11":
                    return (byte) USBHIDCodes.F11;
                case "F12":
                    return (byte) USBHIDCodes.F12;
                case "PrintScreen":
                    return (byte) USBHIDCodes.PRINT_SCREEN;
                case "Scroll":
                    return (byte) USBHIDCodes.SCROLL_LOCK;
                case "Pause":
                    return (byte) USBHIDCodes.PAUSE_BREAK;
                case "OemTilde":
                    return (byte) USBHIDCodes.TILDE;
                case "D1":
                    return (byte) USBHIDCodes.ONE;
                case "D2":
                    return (byte) USBHIDCodes.TWO;
                case "D3":
                    return (byte) USBHIDCodes.THREE;
                case "D4":
                    return (byte) USBHIDCodes.FOUR;
                case "D5":
                    return (byte) USBHIDCodes.FIVE;
                case "D6":
                    return (byte) USBHIDCodes.SIX;
                case "D7":
                    return (byte) USBHIDCodes.SEVEN;
                case "D8":
                    return (byte) USBHIDCodes.EIGHT;
                case "D9":
                    return (byte) USBHIDCodes.NINE;
                case "D0":
                    return (byte) USBHIDCodes.ZERO;
                case "OemMinus":
                    return (byte) USBHIDCodes.MINUS;
                case "OemEquals":
                    return (byte) USBHIDCodes.EQUALS;
                case "Backspace":
                    return (byte) USBHIDCodes.BACKSPACE;
                case "Insert":
                    return (byte) USBHIDCodes.INSERT;
                case "Home":
                    return (byte) USBHIDCodes.HOME;
                case "PageUp":
                    return (byte) USBHIDCodes.PAGE_UP;
                case "NumLock":
                    return (byte) USBHIDCodes.NUM_LOCK;
                case "NumDivide":
                    return (byte) USBHIDCodes.NUM_SLASH;
                case "NumMultiply":
                    return (byte) USBHIDCodes.NUM_ASTERISK;
                case "NumSubtract":
                    return (byte) USBHIDCodes.NUM_MINUS;
                case "Tab":
                    return (byte) USBHIDCodes.TAB;
                case "Q":
                    return (byte) USBHIDCodes.Q;
                case "W":
                    return (byte) USBHIDCodes.W;
                case "E":
                    return (byte) USBHIDCodes.E;
                case "R":
                    return (byte) USBHIDCodes.R;
                case "T":
                    return (byte) USBHIDCodes.T;
                case "Y":
                    return (byte) USBHIDCodes.Y;
                case "U":
                    return (byte) USBHIDCodes.U;
                case "I":
                    return (byte) USBHIDCodes.I;
                case "O":
                    return (byte) USBHIDCodes.O;
                case "P":
                    return (byte) USBHIDCodes.P;
                case "OemLeftBracket":
                    return (byte) USBHIDCodes.OPEN_BRACKET;
                case "OemRightBracket":
                    return (byte) USBHIDCodes.CLOSE_BRACKET;
                case "OemBackslash":
                    return (byte) USBHIDCodes.BACKSLASH;
                case "Delete":
                    return (byte) USBHIDCodes.KEYBOARD_DELETE;
                case "End":
                    return (byte) USBHIDCodes.END;
                case "PageDown":
                    return (byte) USBHIDCodes.PAGE_DOWN;
                case "Num7":
                    return (byte) USBHIDCodes.NUM_SEVEN;
                case "Num8":
                    return (byte) USBHIDCodes.NUM_EIGHT;
                case "Num9":
                    return (byte) USBHIDCodes.NUM_NINE;
                case "NumAdd":
                    return (byte) USBHIDCodes.NUM_PLUS;
                case "CapsLock":
                    return (byte) USBHIDCodes.CAPS_LOCK;
                case "A":
                    return (byte) USBHIDCodes.A;
                case "S":
                    return (byte) USBHIDCodes.S;
                case "D":
                    return (byte) USBHIDCodes.D;
                case "F":
                    return (byte) USBHIDCodes.F;
                case "G":
                    return (byte) USBHIDCodes.G;
                case "H":
                    return (byte) USBHIDCodes.H;
                case "J":
                    return (byte) USBHIDCodes.J;
                case "K":
                    return (byte) USBHIDCodes.K;
                case "L":
                    return (byte) USBHIDCodes.L;
                case "OemSemicolon":
                    return (byte) USBHIDCodes.SEMICOLON;
                case "OemApostrophe":
                    return (byte) USBHIDCodes.APOSTROPHE;
                case "EurPound":
                    return (byte) USBHIDCodes.HASHTAG;
                case "Enter":
                    return (byte) USBHIDCodes.ENTER;
                case "Num4":
                    return (byte) USBHIDCodes.NUM_FOUR;
                case "Num5":
                    return (byte) USBHIDCodes.NUM_FIVE;
                case "Num6":
                    return (byte) USBHIDCodes.NUM_SIX;
                case "LeftShift":
                    return (byte) USBHIDCodes.LEFT_SHIFT;
                case "Z":
                    return (byte) USBHIDCodes.Z;
                case "X":
                    return (byte) USBHIDCodes.X;
                case "C":
                    return (byte) USBHIDCodes.C;
                case "V":
                    return (byte) USBHIDCodes.V;
                case "B":
                    return (byte) USBHIDCodes.B;
                case "N":
                    return (byte) USBHIDCodes.N;
                case "M":
                    return (byte) USBHIDCodes.M;
                case "OemComma":
                    return (byte) USBHIDCodes.COMMA;
                case "OemPeriod":
                    return (byte) USBHIDCodes.PERIOD;
                case "OemSlash":
                    return (byte) USBHIDCodes.FORWARD_SLASH;
                case "RightShift":
                    return (byte) USBHIDCodes.RIGHT_SHIFT;
                case "Up":
                    return (byte) USBHIDCodes.ARROW_UP;
                case "Num1":
                    return (byte) USBHIDCodes.NUM_ONE;
                case "Num2":
                    return (byte) USBHIDCodes.NUM_TWO;
                case "Num3":
                    return (byte) USBHIDCodes.NUM_THREE;
                case "NumEnter":
                    return (byte) USBHIDCodes.NUM_ENTER;
                case "LeftControl":
                    return (byte) USBHIDCodes.LEFT_CONTROL;
                case "LeftWindows":
                    return (byte) USBHIDCodes.LEFT_WINDOWS;
                case "LeftAlt":
                    return (byte) USBHIDCodes.LEFT_ALT;
                case "JpnYen":
                    return (byte) USBHIDCodes.JPN_MUHENKAN;
                case "Space":
                    return (byte) USBHIDCodes.SPACE;
                case "RightAlt":
                    return (byte) USBHIDCodes.RIGHT_ALT;
                //case ("Function"):
                //    return (byte)USBHIDCodes.RIGHT_WINDOWS;
                //case (DeviceKeys.FN_Key):
                //return (byte) USBHIDCodes.RIGHT_WINDOWS;
                case "RightMenu":
                    return (byte) USBHIDCodes.APPLICATION_SELECT;
                case "RightControl":
                    return (byte) USBHIDCodes.RIGHT_CONTROL;
                case "Left":
                    return (byte) USBHIDCodes.ARROW_LEFT;
                case "Down":
                    return (byte) USBHIDCodes.ARROW_DOWN;
                case "Right":
                    return (byte) USBHIDCodes.ARROW_RIGHT;
                case "Num0":
                    return (byte) USBHIDCodes.NUM_ZERO;
                case "NumDecimal":
                    return (byte) USBHIDCodes.NUM_PERIOD;

                default:
                    return (byte) USBHIDCodes.ERROR;
            }
        }

        
    }
}