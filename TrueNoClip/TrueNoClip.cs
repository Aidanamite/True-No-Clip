using UnityEngine;
using HarmonyLib;
using System.Collections.Generic;
using System;
using HMLLibrary;
using RaftModLoader;

public class TrueNoClip : Mod
{
    public static string prefix = "[TrueNoClip]: ";
    private static bool _nc;
    private static bool _fly;
    public static float flightSpeed = 1.5f;
    public static bool noclip {
        get { return _nc; }
        set
        {
            if (_nc == value)
                return;
            player = RAPI.GetLocalPlayer();
            if (player != null)
                moveDirection = Traverse.Create(player.PersonController).Field<Vector3>("moveDirection");
            _nc = value;
            if (player == null)
                return;
            if (_nc)
            {
                for (byte i = 0; i < 32; i++)
                    if (!Physics.GetIgnoreLayerCollision(20, i))
                    {
                        Physics.IgnoreLayerCollision(20, i);
                        ignoredLayers.Add(i);
                    }
            }
            else
            {
                foreach (byte layer in ignoredLayers)
                    Physics.IgnoreLayerCollision(20, layer, false);
                ignoredLayers.Clear();
                player.PersonController.ladder = null;
            }
        }
    }
    public static bool flight
    {
        get { return _fly; }
        set
        {
            if (_fly == value)
                return;
            player = RAPI.GetLocalPlayer();
            if (player != null)
                moveDirection = Traverse.Create(player.PersonController).Field<Vector3>("moveDirection");
            _fly = value;
            if (!_fly)
                player.PersonController.ladder = null;
        }
    }
    public static Network_Player player;
    public static Traverse<Vector3> moveDirection;
    public static List<byte> ignoredLayers;
    Harmony harmony;
    public void Start()
    {
        _nc = false;
        _fly = false;
        ignoredLayers = new List<byte>();
        harmony = new Harmony("com.aidanamite.TrueNoClip");
        harmony.PatchAll();
        Debug.Log(prefix + "Mod has been loaded!");
    }

    public void OnModUnload()
    {
        noclip = false;
        flight = false;
        harmony.UnpatchAll();
        Debug.Log(prefix + "Mod has been unloaded!");
    }

    public override void WorldEvent_WorldUnloaded()
    {
        noclip = false;
        flight = false;
    }

    [ConsoleCommand(name: "noclip2", docs: "Toggles true no clip mode")]
    public static string MyCommand(string[] args)
    {
        if (RAPI.GetLocalPlayer() == null)
            return prefix + "This command can only be used in world";
        noclip = !noclip;
        return prefix + (noclip ? "No clip is now active" : "No clip is no longer active");
    }

    [ConsoleCommand(name: "fly", docs: "Toggles flight mode")]
    public static string MyCommand2(string[] args)
    {
        if (RAPI.GetLocalPlayer() == null)
            return prefix + "This command can only be used in world";
        flight = !flight;
        return prefix + (flight ? "Flight is now active" : "Flight is no longer active");
    }

    [ConsoleCommand(name: "flySpeed", docs: "Syntax: 'flySpeed <speedMultiplier>' Changes the flight speed multiplier of the true no clip and flight modes")]
    public static string MyCommand3(string[] args)
    {
        if (args.Length < 1)
            return prefix + "Not enough arguments";
        if (args.Length > 1)
            return prefix + "Too many arguments";
        try
        {
            flightSpeed = float.Parse(args[0]);
            return prefix + "Flight speed multiplier is now " + flightSpeed.ToString();
        }
        catch
        {
            return prefix + "Failed to parse \"" + args[0] + "\" as a number";
        }
    }

    public void ExtraSettingsAPI_Load()
    {
        flightSpeed = ExtraSettingsAPI_GetSliderValue("flySpeed");
    }
    public void ExtraSettingsAPI_SettingsOpen()
    {
        ExtraSettingsAPI_SetSliderValue("flySpeed", flightSpeed);
    }
    public void ExtraSettingsAPI_SettingsClose()
    {
        flightSpeed = ExtraSettingsAPI_GetSliderValue("flySpeed");
    }


    static Traverse ExtraSettingsAPI_Traverse;
    static bool ExtraSettingsAPI_Loaded = false;
    public float ExtraSettingsAPI_GetSliderValue(string SettingName)
    {
        if (ExtraSettingsAPI_Loaded)
            return ExtraSettingsAPI_Traverse.Method("getSliderValue", new object[] { this, SettingName }).GetValue<float>();
        return 0;
    }
    public void ExtraSettingsAPI_SetSliderValue(string SettingName, float value)
    {
        if (ExtraSettingsAPI_Loaded)
            ExtraSettingsAPI_Traverse.Method("setSliderValue", new object[] { this, SettingName, value }).GetValue();
    }
}

[HarmonyPatch(typeof(PersonController), "GroundControll")]
public class Patch_Flight
{
    public static bool moveCheck = false;
    static void Prefix(ref PersonController __instance)
    {
        if ((TrueNoClip.flight || TrueNoClip.noclip) && TrueNoClip.player.PersonController == __instance)
        {
            if (__instance.ladder == null)
                __instance.ladder = TrueNoClip.player.transform;
            __instance.climbing = true;
        }
    }
    static void Postfix(ref PersonController __instance)
    {
        if ((TrueNoClip.flight || TrueNoClip.noclip) && TrueNoClip.player.PersonController == __instance)
        {
            float speed = MyInput.GetButton("Sprint") ? TrueNoClip.player.PersonController.sprintSpeed : TrueNoClip.player.PersonController.normalSpeed;
            Vector3 lookingDir = Helper.MainCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2)).direction.XZOnly().normalized;
            moveCheck = true;
            Vector3 move = lookingDir * MyInput.GetAxis("Walk") + new Vector3(lookingDir.z, 0 , -lookingDir.x) * MyInput.GetAxis("Strafe") + new Vector3(0, (MyInput.GetButton("Jump") ? 1 : 0) + (MyInput.GetButton("Crouch") ? -1 : 0) ,0);
            moveCheck = false;
            TrueNoClip.moveDirection.Value = move * speed * TrueNoClip.flightSpeed;
        }
    }
}

[HarmonyPatch(typeof(MyInput), "GetButton")]
public class Patch_Crouching
{
    static bool Prefix(ref bool __result, string identifier)
    {
        if (identifier == "Crouch" && !Patch_Flight.moveCheck && (TrueNoClip.flight || TrueNoClip.noclip) && TrueNoClip.player.PersonController.DistanceToObstructionUnderneath > 0.93)
        {
            __result = false;
            return false;
        }
        return true;
    }
}
