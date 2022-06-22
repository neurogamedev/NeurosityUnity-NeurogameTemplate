using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// This is code for a no-frills Heads-Up Display (HUD) that quickly informs the player of the device status and their Focus and Calm scores. 
/// You may want to modify the HUD to better suit your gameplay or artistic direction, if you have any such HUD at all during gameplay.
/// 
/// This HUD makes use of Text Mesh Pro for future proofing.
/// If you want to use legacy UI components, pay attention to anything related to TMP_text.
/// </summary>

namespace Notion.Unity
{

    public class NeuroHUD : MonoBehaviour
    {
        [Header("HUD Elements")]
        public Image statusIcon;
        public TMP_Text batteryPercentage;
        public TMP_Text scoreCalm;
        public TMP_Text scoreFocus;

        [Header("HUD Sprites")]
        public Sprite spriteOnOff;
        public Sprite spriteError;
        public Sprite spriteHighVoltage;
        public Sprite spriteSleep;
        public Sprite spriteUpdate;

        private NotionInterfacer deviceInterface;

        void OnEnable()
        {
            deviceInterface = FindObjectOfType<NotionInterfacer>();
        }

        void HUDUpdate()
        {
            // If you couldn't find the Notion Interfacer in the scene. It's a persistent singleton, so no worries about finding more than one.
            if(deviceInterface == null)
            {
                statusIcon.color = Color.red;
                statusIcon.sprite = spriteError;
                batteryPercentage.text = "N/A";
                batteryPercentage.color = Color.white;
                scoreCalm.text = "0.00";
                scoreFocus.text = "0.00";
                return;
            }

            // If the device isn't logged in.
            if (deviceInterface.deviceStatus == "")
            {
                statusIcon.color = Color.red;
                statusIcon.sprite = spriteError;
                batteryPercentage.text = "N/A";
                batteryPercentage.color = Color.white;
                scoreCalm.text = "0.00";
                scoreFocus.text = "0.00";
                return;
            }

            // If the device is currently receiving an OS update over the WiFi.
            if (deviceInterface.deviceStatus == "Updating")
            {
                statusIcon.color = Color.yellow;
                statusIcon.sprite = spriteUpdate;
                batteryPercentage.text = deviceInterface.deviceBattery + "%";
                batteryPercentage.color = BatteryColorUpdate(deviceInterface.deviceBattery);
                scoreCalm.text = "0.00";
                scoreFocus.text = "0.00";
                return;
            }

            // If the device is charging.
            // Remember: wireless EEG devices become live wires when charging, so they are disabled during charging for safety reasons.
            // You don't want to become the ground when the electricity in your home decides to trigger a freak accident.
            if (deviceInterface.deviceStatus == "Charging")
            {
                statusIcon.color = Color.white;
                statusIcon.sprite = spriteHighVoltage;
                batteryPercentage.text = deviceInterface.deviceBattery + "%";
                batteryPercentage.color = BatteryColorUpdate(deviceInterface.deviceBattery);
                scoreCalm.text = "0.00";
                scoreFocus.text = "0.00";
                return;
            }

            // If the device is online, we're gonna have sume fun!
            if (deviceInterface.deviceStatus == "Online")
            {
                statusIcon.color = Color.green;
                statusIcon.sprite = spriteOnOff;
                batteryPercentage.text = deviceInterface.deviceBattery + "%";
                batteryPercentage.color = BatteryColorUpdate(deviceInterface.deviceBattery);
                scoreCalm.text = string.Format("{0:0.00}", (deviceInterface.calmScore));       
                scoreFocus.text = string.Format("{0:0.00}", (deviceInterface.focusScore));
                return;
            }

            // If the device is offline, booting or shutting off... basically it is no longer transmitting data.
            if (deviceInterface.deviceStatus == "Offline" || deviceInterface.deviceStatus == "Booting" || deviceInterface.deviceStatus == "ShuttingOff")
            {
                statusIcon.color = Color.red;
                statusIcon.sprite = spriteOnOff;
                batteryPercentage.text = "N/A";
                batteryPercentage.color = Color.white;
                scoreCalm.text = "0.00";
                scoreFocus.text = "0.00";
                return;
            }
        }


        // Some quick way of color-coding the battery charge for your viewing pleasure.
        public Color BatteryColorUpdate(float batteryCharge)
        {
            if (batteryCharge > 75) return Color.green;
            if (batteryCharge <= 75 && batteryCharge > 50) return Color.yellow;
            if (batteryCharge <= 50 && batteryCharge > 25) return new Color(1, 0.5f, 0, 1);

            return Color.red;
        }

        // Update is called once per frame
        void Update()
        {
            HUDUpdate();
        }
    }
}
