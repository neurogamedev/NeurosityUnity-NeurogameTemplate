using TMPro;
using System;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// This class takes charge of handling the menus and buttons in the Title Screen.
/// Modify at your pleasure but be careful of any async methods being called during Update.
/// 
/// This HUD makes use of Text Mesh Pro for future proofing.
/// If you want to use legacy UI components, pay attention to anything related to TMP_text.
/// </summary>
namespace Notion.Unity
{ 
    public class TitleScreenGUI : MonoBehaviour
    {
        // Drag and drop these from the Hierarchy into the Inspector.

        [Header("Navigation")]
        public GameObject panelTitle;
        public GameObject panelSettings;
        public GameObject panelCredits;

        [Header("Login Info")]
        public GameObject panelLogin;
        public TMP_InputField inputEmail;
        public TMP_InputField inputPassword;
        public Button buttonLogin;
        public Toggle toggleRememberUser;
        public GameObject panelInfo;
        public TMP_Text infoText;

        [Header("Device Selection")]
        public GameObject panelDevices;
        public TMP_Dropdown dropdownDevices;
        public Button buttonSelect;

        [Header("Device Status")]
        public GameObject panelStatus;
        public Image statusIcon;
        public TMP_Text deviceName;
        public Sprite onOffSprite;              // This is an asset, not in the scene.
        public Sprite chargeSprite;             // This is an asset, not in the scene.
        public Sprite moonSprite;               // This is an asset, not in the scene.
        public TMP_Text statusText;
        public TMP_Text chargeText;
        public TMP_Text focusScore;
        public TMP_Text calmScore;

        // There should be an instance of this singleton in the scene to work.
        private NotionInterfacer deviceInterface;

        // A variable for when the user logs in and chooses their device by Nickname instead of by Device ID.
        private string currentDeviceNickname;

        // The panels we'll be puppeteering.
        private List<GameObject> canvasPanels;

        void OnEnable()
        {      
            // Look for the NotionInterfacer script where we'll get all of our values from. 
            deviceInterface = FindObjectOfType<NotionInterfacer>();

            if(deviceInterface == null)
            {
                Debug.LogError("No Notion Interfacer could be found in the scene." , this);
                return;
            }

            // If the Device instance is already logged in, you want to display its values in the settings.
            // This may happen when coming from another scene.
            if (deviceInterface.IsSubscribed) FoundLoggedDevice();

            // Otherwise, check if there is any saved information in the DeviceData.dat file to autolog the user.
            // This may happen if the Device instance IsRemembered and you're opening the game again.
            else CheckDeviceMemory();

            // Let's put all canvas panels in one list for easy access later on.
            canvasPanels = new List<GameObject>();
            canvasPanels.AddRange(new List<GameObject> { panelTitle, panelSettings, panelCredits });

            // Let's start our game in the Title Screen panel.
            SwitchToPanel(panelTitle);

            // It's a good practice to reset to the Login service panel in the Settings panel, just in case you were editing something before playing.
            // Even if the Settings panel is disabled when starting the game.
            CurrentService(panelLogin);

            // Let's initialize all the buttons.
            buttonLogin.onClick.AddListener(delegate { LoginButton(deviceInterface.IsLoggedIn); }); 
            buttonSelect.onClick.AddListener(delegate { SelectDevice(dropdownDevices.captionText.text); }); 
            toggleRememberUser.onValueChanged.AddListener(delegate { RememberUser(toggleRememberUser.isOn); });
        }

        ///////////////////////////////////

        ////      PUBLIC METHODS      ////
        //// FOR THE PANEL 01 BUTTONS ////

        //////////////////////////////////

        // Method for the START button in the GUI.
        public void StartGame(string sceneName)
        {

            if (deviceInterface.IsSubscribed && SceneUtility.GetBuildIndexByScenePath(sceneName) != -1)     // SANITY CHECK: There must be a valid scene and a logged device.
            {
                AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
            }
            else
            {
                SwitchToPanel(panelSettings);                                                                // Otherwise, go to the settings panel and get ourself logged in or be given information about why you can't log in.
            }
            return;
        }

        // Method to quickly move between panels. It's used for the SETTINGS and CREDITS buttons.
        // If you add or remove panels, make sure to update the canvasPanels initialization in the OnEnable() method;
        public void SwitchToPanel(GameObject nextPanel)
        {
            foreach (GameObject panel in canvasPanels)
            {
                if (panel != nextPanel) panel.SetActive(false);
                else panel.SetActive(true);
            }
        }

        // Method for the QUIT button. 
        public void QuitGame()
        {
            Application.Quit();
        }


        ///////////////////////////////////

        ////      PUBLIC METHODS      ////
        //// FOR THE PANEL 02 BUTTONS ////

        //////////////////////////////////

        // Method for the IsRemembered boolean.
        public void RememberUser(bool IsRemembered)
        {
            deviceInterface.Remember(IsRemembered);
        }

        // Method for the Login button.
        public async void LoginButton(bool isLoggedIn)
        {
            if (!isLoggedIn)
            {
                try
                {
                    await Login();
                }
                catch (Exception e)
                {
                    Debug.Log(e.InnerException.Message.ToString());
                }
            }
            else
            {
                try
                {
                    await Logout();
                }
                catch (Exception e)
                {
                    Debug.Log(e.InnerException.Message.ToString());
                }

            }

            return;
        }

        // Method for the Select Device button.
        public async void SelectDevice(string selectedDeviceNickname)
        {
            try // Let's try to stream data from the selected device.
            {
                buttonSelect.interactable = false;
                buttonSelect.GetComponentInChildren<TMP_Text>().text = "Fetching data...";
                await deviceInterface.SelectDevice(selectedDeviceNickname);
                currentDeviceNickname = selectedDeviceNickname;
                deviceInterface.Subscribe();

                deviceName.text = currentDeviceNickname;
                CurrentService(panelStatus);                    //If successfully connected to the device, let's get to the Status panel.

                // Cleanup of precedent service panel for later use.
                buttonSelect.GetComponentInChildren<TMP_Text>().text = "Select";
                buttonSelect.interactable = true;
            }
            catch (Exception e) //At this stage, it would be very difficult to fail, but you never know. Most likely we were autologged with bogus data.
            {
                buttonSelect.GetComponentInChildren<TMP_Text>().text = "Select";
                Debug.Log(e.ToString());
            }
        }

        // Method for the Logout button... which is the Login button after we are succesfully logged in.
        public async Task Logout()
        {
            CurrentService(panelLogin);

            try // Let's try to log in.
            {
                buttonLogin.interactable = false;
                buttonLogin.GetComponentInChildren<TMP_Text>().text = "Logging out...";
                await deviceInterface.Logout();

                buttonLogin.GetComponentInChildren<TMP_Text>().text = "Login";
                buttonLogin.interactable = true;

                focusScore.text = "00%";
                calmScore.text = "00%";
            }
            catch (Exception e) // Otherwise tell the user what went wrong.
            {
                infoText.text = e.InnerException.Message.ToString();
                Debug.Log(e.InnerException.Message.ToString());
                buttonLogin.GetComponentInChildren<TMP_Text>().text = "Login";
                buttonLogin.interactable = true;
            }

            buttonLogin.GetComponent<Image>().color = new Color(1f, 15f, 1f, 1f);

            return;

        }

        ////////////////////////////////

        ////     PRIVATE METHODS    ////
        //// FOR PANEL 01 PROCESSES ////

        ////////////////////////////////

        // If the player comes from a different scene with an already logged in Device instance,
        // we want to display this info in the settings panel in the background. Just incase they check.
        private async void FoundLoggedDevice()
        {
            // SANITY CHECKS by Justin Case.
            if (deviceInterface == null) return;
            if (deviceInterface.IsLoggedIn == false) return;
            if (deviceInterface.IsSubscribed == false) return;

            // Let's fill in the Login GUI with the information we already have from the logged device.
            inputEmail.text = deviceInterface.GetDeviceEmail();
            inputPassword.text = deviceInterface.GetDevicePassword();
            toggleRememberUser.isOn = deviceInterface.IsRemembered;

            // Let's make a show of accessing the devices in the account, in case the player wants to switch devices.
            CurrentService(panelDevices);
            dropdownDevices.ClearOptions();
            var placeholderOption = new List<TMP_Dropdown.OptionData>();
            placeholderOption.Add(new TMP_Dropdown.OptionData("Fetching devices..."));
            dropdownDevices.AddOptions(placeholderOption);
            dropdownDevices.RefreshShownValue();

            try
            {
                var devicesInfo = await deviceInterface.notion.GetDevices();
                var fetchedDevices = new List<TMP_Dropdown.OptionData>();

                foreach (DeviceInfo device in devicesInfo)
                {
                    fetchedDevices.Add(new TMP_Dropdown.OptionData(device.DeviceNickname));
                }

                dropdownDevices.ClearOptions();
                dropdownDevices.AddOptions(fetchedDevices);
                dropdownDevices.RefreshShownValue();

                buttonLogin.GetComponentInChildren<TMP_Text>().text = "Logout";
                buttonLogin.GetComponent<Image>().color = new Color(1f, 0.75f, 0.75f, 1f);
                infoText.text = "";
                buttonLogin.interactable = true;
            }
            catch (NullReferenceException) //If getting the devices was unsuccessful, let's inform the user.
            {
                CurrentService(panelLogin);
                infoText.text = "No devices could be fetched.";
                buttonLogin.GetComponentInChildren<TMP_Text>().text = "Login";
                buttonLogin.GetComponent<Image>().color = new Color(1f, 1f, 1f, 1f);
                buttonLogin.interactable = true;
                return;
            }

            // And let's display the Status panel with the info of the already logged-in device.
            CurrentService(panelStatus);

            return;

        }

        // Let's get the data in the Device Instance. By this moment, the Notion Interfacer muct already have decrypted the data from the DeviceData.dat file.
        private async void CheckDeviceMemory()
        {
            inputEmail.text = deviceInterface.GetDeviceEmail();
            inputPassword.text = deviceInterface.GetDevicePassword();
            toggleRememberUser.isOn = deviceInterface.IsRemembered;

            if(deviceInterface.IsRemembered)  // If IsRemembered is true, then it logically follows that the Email, Passowrd and DeviceID are not blank.                     
            {
                try 
                { 
                    await Login();            // So let's try logging in! Obviously we can even remember bogus information, in which case the player will be informed of an error in the GUI.
                    SelectDevice(dropdownDevices.captionText.text);
                }
                catch (Exception e)           // Sanity check, anyone...?
                {
                    Debug.Log(e);
                }
            }
        }


        ////////////////////////////////

        ////     PRIVATE METHODS    ////
        //// FOR PANEL 02 PROCESSES ////

        ////////////////////////////////      

        // These will be used to display GUI elements in the Settings panel.
        private void CurrentService(GameObject panel)
        {
            if (panel == panelLogin)
            {
                infoText.text = "";
                panelLogin.SetActive(true);
                panelInfo.SetActive(true);
                panelDevices.SetActive(false);
                panelStatus.SetActive(false);
                return;
            }

            if (panel == panelDevices)
            {
                panelLogin.SetActive(true);
                panelInfo.SetActive(false);
                panelDevices.SetActive(true);
                panelStatus.SetActive(false);
                return;
            }

            if (panel == panelStatus)
            {
                panelLogin.SetActive(true);
                panelInfo.SetActive(false);
                panelDevices.SetActive(true);
                panelStatus.SetActive(true);
                return;
            }
        }
     
        // The Login method to interact with the Notion Interfacer and give visual feedback to the player.
        private async Task Login()
        {
            try // Let's try to log in.
            {
                buttonLogin.interactable = false;
                buttonLogin.GetComponentInChildren<TMP_Text>().text = "Logging in...";
                await deviceInterface.Login(inputEmail.text, inputPassword.text);  
            } 
            catch (Exception e) // Otherwise tell the user what went wrong.
            {
                infoText.text = e.InnerException.Message.ToString();
                buttonLogin.GetComponentInChildren<TMP_Text>().text = "Login";
                buttonLogin.GetComponent<Image>().color = new Color(1f, 1f, 1f, 1f);
                buttonLogin.interactable = true;
                return;
            }

            // If we were sucessful logging in, we'll open Select Devices panel and give visual feedback while fetching the devices in the player's Neurosity account.
            dropdownDevices.ClearOptions();
            var placeholderOption = new List<TMP_Dropdown.OptionData>();
            placeholderOption.Add(new TMP_Dropdown.OptionData("Fetching devices..."));
            dropdownDevices.AddOptions(placeholderOption);
            dropdownDevices.RefreshShownValue();

            CurrentService(panelDevices);

            // Let's actually try fetching those devices and putting them in the Dropdown menu.
            try
            {
                var devicesInfo = await deviceInterface.notion.GetDevices();
                var fetchedDevices = new List<TMP_Dropdown.OptionData>();

                foreach (DeviceInfo device in devicesInfo)
                {
                    fetchedDevices.Add(new TMP_Dropdown.OptionData(device.DeviceNickname));
                }

                dropdownDevices.ClearOptions();
                dropdownDevices.AddOptions(fetchedDevices);
                dropdownDevices.RefreshShownValue();

                // Cleanup of precedent panel for later use.
                buttonLogin.GetComponentInChildren<TMP_Text>().text = "Logout";
                buttonLogin.GetComponent<Image>().color = new Color(1f, 0.75f, 0.75f, 1f);
                infoText.text = "";
                buttonLogin.interactable = true;
            }
            catch (NullReferenceException) //If getting the devices was unsuccessful, let's inform the user.
            {
                CurrentService(panelLogin);
                infoText.text = "No devices could be fetched.";
                buttonLogin.GetComponentInChildren<TMP_Text>().text = "Login";
                buttonLogin.interactable = true;
                return;
            }

            return;

        }


        // This is the fun part where you tell the GUI how to show the Status
        void StatusUpdate()
        {
            if (deviceInterface.deviceStatus == "") return;                                     // SANITY CHECK: Freak error, but you never know.

            if (deviceInterface.deviceStatus == "Updating")                                     // If the device is updating over the WiFi.
            {
                statusIcon.color = Color.yellow;
                statusIcon.sprite = moonSprite;
                statusText.text = "Sleeping while updating";
                chargeText.text = deviceInterface.deviceBattery + "%";
                chargeText.color = BatteryColorUpdate(deviceInterface.deviceBattery);
                calmScore.text = "00%";
                focusScore.text = "00%";
                return;
            }

            if (deviceInterface.deviceStatus == "Charging")                                     // If the device is charging its battery. Sleepmode is an electric safety feature. 
            {
                statusIcon.color = Color.white;
                statusIcon.sprite = chargeSprite;
                statusText.text = "Sleeping while charging";
                chargeText.text = deviceInterface.deviceBattery + "%";
                chargeText.color = BatteryColorUpdate(deviceInterface.deviceBattery);
                calmScore.text = "00%";
                focusScore.text = "00%";
                return;
            }

            if (deviceInterface.deviceStatus == "Online")                                       // If the device is Online, it is obviously transmitting data. Let's show it! 
            {
                statusIcon.color = Color.green;
                statusIcon.sprite = onOffSprite;
                statusText.text = "Online";
                chargeText.text = deviceInterface.deviceBattery + "%";
                chargeText.color = BatteryColorUpdate(deviceInterface.deviceBattery);
                calmScore.text = string.Format("{0:00}%", (deviceInterface.calmScore * 100));    // The scores are usually given from 0 to 1, so I formatted them to be presented as something like 23% instead of 0.2356010....
                focusScore.text = string.Format("{0:00}%", (deviceInterface.focusScore * 100));
                return;
            }

            // If the device is offline, booting or shutting off... basically it is no longer transmitting data.
            if (deviceInterface.deviceStatus == "Offline" || deviceInterface.deviceStatus == "Booting" || deviceInterface.deviceStatus == "ShuttingOff") 
            {
                statusIcon.color = Color.red;
                statusIcon.sprite = onOffSprite;
                statusText.text = deviceInterface.deviceStatus;
                chargeText.color = Color.white;
                chargeText.text = "N/A";
                calmScore.text = "00%";
                focusScore.text = "00%";
                return;
            }
        }

        // Some quick way of color-coding the battery charge for your viewing pleasure.
        Color BatteryColorUpdate(float batteryCharge) 
        {
            if (batteryCharge > 75) return Color.green;
            if (batteryCharge <= 75 && batteryCharge > 50) return Color.yellow ;
            if (batteryCharge <= 50 && batteryCharge > 25) return new Color(1, 0.5f, 0, 1);

            return Color.red;

        }

        // Update is called once per frame
        void Update()
        {
            if (!deviceInterface) return;

            if (panelStatus.activeSelf) 
            {
                StatusUpdate();
            }

        }
   
    }
}

