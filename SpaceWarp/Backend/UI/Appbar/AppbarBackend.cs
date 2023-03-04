// Attribution Notice To Lawrence/HatBat of https://github.com/Halbann/LazyOrbit
// This file is licensed under https://creativecommons.org/licenses/by-sa/4.0/

using System;
using System.Collections;
using BepInEx.Logging;
using HarmonyLib;
using I2.Loc;
using KSP.Api;
using KSP.Api.CoreTypes;
using KSP.Game;
using KSP.Iteration.UI.Binding;
using KSP.Sim.impl;
using KSP.UI;
using KSP.UI.Binding;
using KSP.UI.Flight;
using RTG;
using SpaceWarp.API.UI.Appbar;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SpaceWarp.Backend.UI.Appbar;

internal static class AppbarBackend
{
    
    private static readonly ManualLogSource _logger = BepInEx.Logging.Logger.CreateLogSource("ToolbarBackend");
    public static GameObject AddButton(string buttonText, Sprite buttonIcon, string buttonId, Action<bool> function)
    {
        // Find the resource manager button and "others" group.

        // Say the magic words...
        GameObject list = GameObject.Find("GameManager/Default Game Instance(Clone)/UI Manager(Clone)/Popup Canvas/Container/ButtonBar/BTN-App-Tray/appbar-others-group");
        GameObject resourceManger = list?.GetChild("BTN-Resource-Manager");

        if (list == null || resourceManger == null)
        {
            _logger.LogInfo("Couldn't find appbar.");
            return null;
        }

       

        // Clone the resource manager button.
        GameObject appButton = GameObject.Instantiate(resourceManger, list.transform);
        appButton.name = buttonId;

        // Change the text.
        TextMeshProUGUI text = appButton.GetChild("Content").GetChild("TXT-title").GetComponent<TextMeshProUGUI>();
        text.text = buttonText;

        Localize localizer = text.gameObject.GetComponent<Localize>();
        if (localizer)
            GameObject.Destroy(localizer);

        // Change the icon.
        GameObject icon = appButton.GetChild("Content").GetChild("GRP-icon");
        Image image = icon.GetChild("ICO-asset").GetComponent<Image>();
        image.sprite = buttonIcon;

        // Add our function call to the toggle.
        ToggleExtended utoggle = appButton.GetComponent<ToggleExtended>();
        utoggle.onValueChanged.AddListener(state => function(state));

        // Set the initial state of the button.
        UIValue_WriteBool_Toggle toggle = appButton.GetComponent<UIValue_WriteBool_Toggle>();
        toggle.BindValue(new Property<bool>(false));

        // Bind the action to close the tray after pressing the button.
        IAction action = resourceManger.GetComponent<UIAction_Void_Toggle>().Action;
        appButton.GetComponent<UIAction_Void_Toggle>().BindAction(action);

        _logger.LogInfo($"Added appbar button: {buttonId}");

        return appButton;
    }
    public static GameObject AddButtonOAB(string buttonText, Sprite buttonIcon, string buttonId, Action<bool> function)
    {
       

        //buttonId = buttonId + "OAB";
        

        // Abra Kadbra...
        GameObject oabAppBar = GameObject.Find("OAB(Clone)/HUDSpawner/HUD/widget_SideBar/widget_sidebarNav");
        if (oabAppBar == null)
        {
            return null;
        }


        GameObject _trayButton = oabAppBar.GetChild("BTN-App-Tray(Clone)");
        if (_trayButton == null)
        {
            
            _trayButton = GameManager.Instance.Game.UI.GetPopupCanvas().gameObject.GetChild("BTN-App-Tray");
            
            //_trayButton.Persist();
            //_trayButton.SetActive(true);
            
            _trayButton = UnityObject.Instantiate(_trayButton, oabAppBar.transform);
            GameObject _oldOthersGroup = _trayButton.GetChild("appbar-others-group");
            foreach (GameObject o in _oldOthersGroup.GetAllChildren())
            {
                if (o.name != "BTN-Resource-Manager")
                {
                    GameObject.Destroy(o);
                }
            }
            
            _trayButton.transform.parent = oabAppBar.transform;
            if (_trayButton == null)
            {
               
                return null;
            }
        }

        
        GameObject _othersGroup = _trayButton.GetChild("appbar-others-group");
        GameObject _resourceManager = _othersGroup.GetChild("BTN-Resource-Manager");
        //_othersGroup.GetChild("BTN-Kerbal-Manager").DestroyGameObject();
        //_othersGroup.GetChild("BTN-Action-Manager").DestroyGameObject();


        // Clone the resource manager button.
        GameObject oabTrayButton = GameObject.Instantiate(_resourceManager, _othersGroup.transform);
        oabTrayButton.name = buttonId;
        oabTrayButton.SetActive(true);
        
        // Add our function call to the toggle.
        ToggleExtended utoggle = oabTrayButton.GetComponent<ToggleExtended>();
        utoggle.onValueChanged.AddListener(state => function(state));

        // Set the initial state of the button.
        UIValue_WriteBool_Toggle toggle = oabTrayButton.GetComponent<UIValue_WriteBool_Toggle>();
        toggle.BindValue(new Property<bool>(false));

        // Change the text.
        TextMeshProUGUI text = oabTrayButton.GetChild("Content").GetChild("TXT-title").GetComponent<TextMeshProUGUI>();
        text.text = buttonText;

        Localize localizer = text.gameObject.GetComponent<Localize>();
        if (localizer)
            GameObject.Destroy(localizer);

        // Change the icon.
        GameObject icon = oabTrayButton.GetChild("Content").GetChild("GRP-icon");
        Image image = icon.GetChild("ICO-asset").GetComponent<Image>();
        image.sprite = buttonIcon;


        Debug.Log($"Added appbar button: {oabTrayButton.name}");

        return oabTrayButton;
        



   
    }


    public static UnityEvent AppBarInFlightSubscriber = new UnityEvent();
    public static UnityEvent AppBarOabSubscriber =  new UnityEvent();

    internal static void SubscriberSchedulePing()
    {
        GameObject gameObject = new GameObject();
        gameObject.AddComponent<ToolbarBackendObject>();
        gameObject.SetActive(true);
       // gameObject.hideFlags = HideFlags.HideAndDontSave;
       
    }
    
}

class ToolbarBackendObject : KerbalBehavior
{
    public new void Start()
    {
      
      StartCoroutine(awaiter());
    }

    private IEnumerator awaiter()
    {
        yield return new WaitForSeconds(1);
        if (KSP.Game.GameManager.Instance == null)
        {
           yield break;
        }
        //Ew
        if (KSP.Game.GameManager.Instance.Game.GlobalGameState.GetState() == KSP.Game.GameState.VehicleAssemblyBuilder)
        {
            AppbarBackend.AppBarOabSubscriber.Invoke();

        }
        else  { 
               
               AppbarBackend.AppBarInFlightSubscriber.Invoke();
        }
       
        
        Destroy(this);
    }
}

//TODO: Much better way of doing this
[HarmonyPatch(typeof(UIFlightHud))]
[HarmonyPatch("Start")]
class ToolbarBackendAppBarPatcher
{
    public static void Postfix(UIFlightHud __instance) => AppbarBackend.SubscriberSchedulePing();
}

[HarmonyPatch(typeof(OABSideBar))]
[HarmonyPatch("Start")]
class ToolbarBackendAppBarPatcherOAB
{
    public static void Postfix(OABSideBar __instance) => AppbarBackend.SubscriberSchedulePing();
}