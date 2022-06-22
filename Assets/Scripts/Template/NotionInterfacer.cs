using Newtonsoft.Json;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// This code is based on Ryan Turney's Notion SDK for Unity (https://github.com/ryanturney/notion-unity).
/// As he puts it, the architecture may not be sound, so be careful when Building for mobile or when updating dependencies.
/// This script is conceived to provide Unity with data from the Neurosity Crown or Notion so that you can just worry about designing a game.
/// Enjoy! - Diego Saldivar
/// </summary>

namespace Notion.Unity
{
    public class NotionInterfacer : MonoBehaviour
    {

        // This one is to check for other instances and kill them to ensure this script is a singleton.
        public static NotionInterfacer Instance { get; private set; }

        // You'll have to drag and drop a Device instance in the Inspector.
        // If the user wants the game to remember the login info, the data is saved in an encrypted .dat file in order to render the information illegible to prying human eyes.
        // The key is hardcoded though, so you may want to fumble around with it or use some serious encryption when distributing your game.
        // To create a Device instance you can drag and drop from the assets, you just click "Assets -> Create -> Device" and a new Device object will be created in your Assets folder.
        [SerializeField]
        private Device device;

        // If we have some values already in our device we can check this box to force the Notion Interfacer to login without any prompts or values from the GUI.
        // We can click this bool during Play to log into the Device instance without having to go through the Title Screen level to log in.
        [SerializeField]
        private bool loginFromInspector = false;


        // In case we want to manually override the data from the device above from the Inspector.
        // Works during Play as well.
        [Header("Override Values")]

        [SerializeField]
        private string overrideEmail;
        [SerializeField]
        private string overridePassword;
        [SerializeField]
        private string overrideDeviceId;
        [SerializeField]
        private bool overrideIsRemembered;
        [SerializeField]
        private bool loginWithOverrideData = false;

        // If we're logging in from the inspector, Login(...) will be called from the FixedUpdate() method. 
        // This boolean blocks any extra calls if we're already awaiting the Login process.
        bool IsLoggingIn = false;

        // This dependency is the backbone for logging in.
        [HideInInspector]
        public FirebaseController controller;

        // This is Ryan Turney's SDK backbone to communicate with the Device.
        [HideInInspector]
        public Notion notion;

        // Accessible values to communicate sanity checks to other classes.
        [HideInInspector]
        public bool IsLoggedIn = false;             // If the Device instance is already logged in.
        [HideInInspector]
        public bool IsSubscribed = false;           // If the Device instance is subscribed to values like focus or calm.
        [HideInInspector]
        public bool IsRemembered = false;           // If the Device instance is marked as remembered.
        [HideInInspector]
        public DeviceStatus currentStatus;          // A list of possible statuseseseses in a handy struct by Ryan Turney.

        // The very basics we'll need for a neurogame.
        [Header("Device Information")]

        public string selectedDeviceId;
        public string deviceStatus;
        public float deviceBattery;

        // In case we only want to subscribe to a few channels, we can check or uncheck the relevant booleans.
        // Feel free to add more stuff from other Handlers. in the Assets > Scripts >Notion-Unity > Handlers folder.
        [Header("Subscriptions")]

        public bool subscribeToCalm;
        public float calmScore;

        public bool subscribeToFocus;
        public float focusScore;

        public bool subscribeToAccelerometer;
        public float accelerometerAcceleration;
        public float accelerometerInclination;
        public float accelerometerOrientation;
        public float accelerometerPitch;
        public float accelerometerRoll;
        public Vector3 accelerometerVector;


        ////////////////////////

        //// PRIVATE METHODS ////

        ////////////////////////

        private void Awake()
        {
            // Let's kill all other instances to make sure the script is a singleton.
            if (Instance != null && Instance != this)
            {
                Destroy(this.gameObject);
                return;
            }

            // And make sure this singleton is persistent because we don't want to relog every time we change scenes.
            Instance = this;
            DontDestroyOnLoad(this.gameObject);

            // This is if we're feeding a test Device instance which we want to login from the Inspector or if we want to override the values in the Inspector before hitting the Play button.
            // Basically, it lets us test levels without having to login from the Settings panel in the Title Screen level.
            // We can still login from the Inspector during Play, as seen in the FixedUpdate().
            if (loginFromInspector || loginWithOverrideData) return;

            // Otherwise, load data from memory.
            LoadDeviceData();
        }

        
        private void OnEnable()
        {
            // Let's initialize some values.
            ClearStatusValues();
            ClearSubscriptionValues();

            // I tried writing code that didn't need a Device instance but turns out this SDK needs it to pass information between classes.
            // Also, creating an instance during runtime leads to all kinds of pointer headaches.
            if (device == null)
            {
                Debug.LogError( "Provide a device device instance. Assets -> Create -> Device", this );
                return;
            }
            
        }

        // This method encrypts and saves the Device instance data in a .dat file.
        // The encryption uses a hardcoded key, which isn't secure, but at least it scrambles the characters in the .dat file so that prying human eyes can't read your password in the .dat file when opening it in a text editor.
        // This is a "spit-and-prayer" approach. Be sure to either change the key or properly implement encryption in your Build.
        //
        // !! DO NOT RELY ON THIS SOLUTION FOR YOUR FINAL BUILD !!
        //
        // For more information about PROPERLY saving encrypted data, consult Dan Cox's approach where this code was inspired from.
        // https://videlais.com/2021/02/28/encrypting-game-data-with-unity/

        void SaveDeviceData()
        {
            // Turns out we can't serialize a Scriptable Object, id est, the Device class.
            // So, let's save our data in a Serializable class (SaveData) declared aaaall the way below.
            SaveData data = new SaveData();
            data.savedEmail = device.Email;
            data.savedPassword = device.Password;
            data.savedDeviceId = device.DeviceId;
            data.savedIsRemembered = device.IsRemembered;

            // Key for reading and writing encrypted data.
            // (This is a hardcoded "secret" key. )
            //
            // !! THIS IS THE WEAKEST LINK !!
            //
            byte[] hardcodedKey = { 0x16, 0x15, 0x16, 0x15, 0x16, 0x15, 0x16, 0x15, 0x16, 0x15, 0x16, 0x15, 0x16, 0x15, 0x16, 0x15 };

            // Create new AES instance.
            Aes iAes = Aes.Create();

            // Create a FileStream for creating files.
            FileStream dataStream = new FileStream(Application.persistentDataPath + "/DeviceData.dat", FileMode.Create);

            // Save the new generated IV.
            byte[] inputIV = iAes.IV;

            // Write the IV to the FileStream unencrypted.
            dataStream.Write(inputIV, 0, inputIV.Length);

            // Create CryptoStream, wrapping FileStream.
            CryptoStream iStream = new CryptoStream(
                    dataStream,
                    iAes.CreateEncryptor(hardcodedKey, iAes.IV),
                    CryptoStreamMode.Write);

            // Create StreamWriter, wrapping CryptoStream.
            StreamWriter sWriter = new StreamWriter(iStream);

            // Serialize the object into JSON and save string.
            string jsonString = JsonUtility.ToJson(data);

            // Write to the innermost stream (which will encrypt).
            sWriter.Write(jsonString);

            // Close StreamWriter.
            sWriter.Close();

            // Close CryptoStream.
            iStream.Close();

            // Close FileStream.
            dataStream.Close();

        }

        // This method decrypts the data in our .dat file and then feeds it to the NotionInterfacer.
        // If you've muddled around with the .dat file, you may get an error retrieving data. Delete the .dat file and it will be rewritten next time you Play.
        // Remember: the decryption uses the exact same hardcoded key as above. Again, not the safest way if anyone is reading this code on GitHub.
        // This is a "spit-and-prayer" approach. Be sure to either change the key or properly implement encryption in your Build.
        //
        // !! DO NOT RELY ON THIS SOLUTION FOR YOUR FINAL BUILD !!
        //
        // For more information about PROPERLY saving encrypted data, consult Dan Cox's approach where this code was inspired from.
        // https://videlais.com/2021/02/28/encrypting-game-data-with-unity/
        void LoadDeviceData()
        {

            if (File.Exists(Application.persistentDataPath + "/DeviceData.dat"))
            {
                // Key for reading and writing encrypted data.
                // (This is a hardcoded "secret" key. )
                //
                // !! THIS IS THE WEAKEST LINK !!
                //
                byte[] savedKey = { 0x16, 0x15, 0x16, 0x15, 0x16, 0x15, 0x16, 0x15, 0x16, 0x15, 0x16, 0x15, 0x16, 0x15, 0x16, 0x15 };

                // Create FileStream for opening files.
                FileStream dataStream = new FileStream(Application.persistentDataPath + "/DeviceData.dat", FileMode.Open);

                // Create new AES instance.
                Aes oAes = Aes.Create();

                // Create an array of correct size based on AES IV.
                byte[] outputIV = new byte[oAes.IV.Length];

                // Read the IV from the file.
                dataStream.Read(outputIV, 0, outputIV.Length);

                // Create CryptoStream, wrapping FileStream
                CryptoStream oStream = new CryptoStream(
                       dataStream,
                       oAes.CreateDecryptor(savedKey, outputIV),
                       CryptoStreamMode.Read);

                // Create a StreamReader, wrapping CryptoStream
                StreamReader reader = new StreamReader(oStream);

                // Read the entire file into a String value.
                string text = reader.ReadToEnd();

                SaveData data = new SaveData();

                // Deserialize the JSON data 
                //  into a pattern matching the SaveData class.
                data = JsonUtility.FromJson<SaveData>(text);

                // Let's feed all relevant info to the Notion Interfacer and the Device instance it is holding.
                device.Email = data.savedEmail;
                device.Password = data.savedPassword;
                device.DeviceId = data.savedDeviceId;
                device.IsRemembered = data.savedIsRemembered;
                IsRemembered = device.IsRemembered;

            }
        }

        // This method simply delete the .dat file from inside the Notion Interfacer.
        void DeleteDeviceData()
        {
            if (File.Exists(Application.persistentDataPath + "/DeviceData.dat"))
            {
                File.Delete(Application.persistentDataPath + "/DeviceData.dat");
            }
        }

        // This mehtods clears the Device instance info and deletes the .dat file for good measure.
        // By the way...
        //
        // !! BEFORE YOU BUILD OR SHARE !!
        //
        // Uncheck IsRemembered during Play to clear all data in the Device instance or in the .dat file.
        // You don't want to give away your personal info to the world, do you?
        private void ClearDeviceInfo()
        {
            if (device.IsRemembered) return; // If the device is remembered, data will later be saved as-is. 

            device.Email = "";
            device.Password = "";
            device.DeviceId = "";
            device.IsRemembered = false;

            DeleteDeviceData();
        }

        // Clear up and/or initialize Status values
        private void ClearStatusValues()
        {
            selectedDeviceId = "Not selected";
            deviceStatus = "";
            deviceBattery = 0;
        }

        // Clear up and/or initialize Subscription values
        // Sometimes we need to ensure we can read a bunch of zeroes.
        private void ClearSubscriptionValues()
        {
            calmScore = 0;
            focusScore = 0;
            accelerometerAcceleration = 0;
            accelerometerInclination = 0;
            accelerometerOrientation = 0;
            accelerometerPitch = 0;
            accelerometerRoll = 0;
            accelerometerVector = new Vector3 { x = 0, y = 0, z = 0 };
        }


        // This is where we get the Device instance status and battery info every time we call this method on the FixedUpdate().
        // Since this method is being called every so often, there is a myriad of sanity checks before pulling the data from the Firebase database.
        // In Ryan Turney's SDK, one does not simply subscribe to this info, but rather fetches a snapshot with no live updates.
        // This is why some warnings arise upon Logout(), since interrupting some of these processes mid-execution may throw some warnings and errors.
        // The usual warning is:
        /// <summary>
        /// Future with handle 1 still exists though its backing API 0xD8E76DB0 is being deleted. Please call Future::Release() before deleting the backing API.
        /// UnityEngine.Debug:LogWarning(object)
        /// Firebase.Platform.FirebaseLogger:LogMessage(Firebase.Platform.PlatformLogLevel, string)
        /// Firebase.LogUtil:LogMessage(Firebase.LogLevel, string)
        /// Firebase.LogUtil:LogMessageFromCallback(Firebase.LogLevel, string)
        /// Firebase.AppUtil:PollCallbacks()
        /// Firebase.Platform.FirebaseAppUtils:PollCallbacks()
        /// Firebase.Platform.FirebaseHandler:Update()
        /// Firebase.Platform.FirebaseEditorDispatcher:Update()
        /// UnityEditor.EditorApplication:Internal_CallUpdateFunctions()
        /// </summary>
        private async Task UpdateStatus()
        {
            // NOTION SANITY CHECKS //
            if (notion == null) return;                         // If we're logged out, stop!
            if (!notion.IsLoggedIn || IsLoggingIn) return;      // If we're not yet logged in, stop!
            if (!device) return;                                // If the device isn't valid, id est, if it doesn't have an e-mail, a password AND a device ID.

            // Let's now check whether the device ID is valid or whether it has a placeholder string.
            selectedDeviceId = device.DeviceId;                 
            if (selectedDeviceId.Length < 30) return;           // If we have a placeholder string (usually shorter than a Device ID), stop!

            // FIREBASE SANITY CHECK //
            if (controller.NotionDatabase == null) return;      // If we're logging out or not yet connected to the device, stop!

            // This SDK doesn't subscribe to the status as much as it gives us a snapshot of the status, thus the need to Update() the info.
            var statusSnapshot = await controller.NotionDatabase.GetReference($"devices/{selectedDeviceId}/status").GetValueAsync();

            // ANOTHER FIREBASE SANITY CHECK //
            if (statusSnapshot == null) return;                 // We can sometimes be given a snapshot with nothing on it. Usually happens during Login() and Logout();

            string json = statusSnapshot.GetRawJsonValue();     // Parse the data sent by the snapshot.

            // JSON SANITY CHECK //
            if (json == null) return;                           // Don't ask me why, but sometimes null values slip through the cracks in the frames.


            currentStatus = JsonConvert.DeserializeObject<DeviceStatus>(json);      // Consult class DeviceStatus to find out exactly what info is parsed. 

            
            if (currentStatus.SleepMode == true)                // If the device is in Sleep Mode, let's clear old subscription values and showcase info on why it is sleeping (Update or Charging).                      
            {
                ClearSubscriptionValues();
                deviceStatus = currentStatus.SleepModeReason.ToString();
            }
            else
            {
                deviceStatus = currentStatus.State.ToString();  // Let's get a string giving us a description of the device status. Exempli gratia: Charging, Updating, Online, Offline, et cetera.
            }

            // There is no point in displaying battery information from a device that is not transmitting data. We'll just get outdated data.
            if (deviceStatus == "Offline" || deviceStatus == "Booting" || deviceStatus == "ShuttingOff") return;

            deviceBattery = currentStatus.Battery;              // Batery percentage from 0 to 100.   

            // Aaaand that's it! Everything else can be subscribed to and doesn't need to be constantly fetched per Update.
            return;
        }


        /// <summary>
        /// The subscribers are mostly self explanatory.
        /// I had to modify the handlers to send info to another class instead of merely to the console.
        /// That is usually the main hitch with Unity: COMMUNICATION.
        /// </summary>

        // Subscribe to Calm.
        private void SubscribeCalm()
        {
            if (!notion.IsLoggedIn) return;

            notion.Subscribe(new CalmHandler
            {
                OnCalmUpdated = (probability) =>
                {
                    calmScore = probability;
                }
            });

            //Debug.Log( "Subscribed to calm" );

        }

        // Subscribe to Focus.
        private void SubscribeFocus()
        {
            if (!notion.IsLoggedIn) return;

            notion.Subscribe(new FocusHandler
            {
                OnFocusUpdated = (probability) =>
                {
                    focusScore = probability;
                }
            });

            //Debug.Log( "Subscribed to focus" );
        }

        // Subscribe to the accelerometer's direction and acceleration.
        // Remember:
        //              x = roll
        //              y = pitch
        //              z = yaw (not provided by handler)
        private void SubscribeAccelerometer()
        {
            if (!notion.IsLoggedIn) return;
            notion.Subscribe(new AccelerometerHandler
            {
                OnAccelerometerUpdated = (accelerometer) =>
                {
                    accelerometerAcceleration = accelerometer.Acceleration;
                    accelerometerInclination = accelerometer.Inclination;
                    accelerometerOrientation = accelerometer.Orientation;

                    accelerometerPitch = accelerometer.Pitch;
                    accelerometerRoll = accelerometer.Roll;

                    accelerometerVector.x = accelerometer.X;
                    accelerometerVector.y = accelerometer.Y;
                    accelerometerVector.z = accelerometer.Z;
                }
            });

            //Debug.Log( "Subscribed to accelerometer" );
        }


        // This one is legacy but may still be used.
        // I'll leave it here since it's syntax is slightly different from the handlers above.
        // Legacy summary below:

        /// <summary>
        /// Add kinesisLabel based on the thought you're training.
        /// For instance: leftArm, rightArm, leftIndexFinger, etc
        /// </summary>
        /// <param name="kinesisLabel"></param>
        private void SubscribeKinesis(string kinesisLabel)
        {
            if (!notion.IsLoggedIn) return;

            notion.Subscribe(new KinesisHandler
            {
                Label = kinesisLabel,
                OnKinesisUpdated = (probability) =>
                {
                    //_textKinesisProbability.text = $"{kinesisLabel} : {probability}";
                }
            });
        }

        // I am using a FixedUpdate method to lower the warnings that come from interrupting the UpdateStatus() method when logging out.
        // You'll find that UpdateStatus() will be awaiting a thread to finish to get a Status snapshot. 
        // This thread may not be done waiting when Logout is called.
        // The usual warning is:
        /// <summary>
        /// Future with handle 1 still exists though its backing API 0xD8E76DB0 is being deleted. Please call Future::Release() before deleting the backing API.
        /// UnityEngine.Debug:LogWarning(object)
        /// Firebase.Platform.FirebaseLogger:LogMessage(Firebase.Platform.PlatformLogLevel, string)
        /// Firebase.LogUtil:LogMessage(Firebase.LogLevel, string)
        /// Firebase.LogUtil:LogMessageFromCallback(Firebase.LogLevel, string)
        /// Firebase.AppUtil:PollCallbacks()
        /// Firebase.Platform.FirebaseAppUtils:PollCallbacks()
        /// Firebase.Platform.FirebaseHandler:Update()
        /// Firebase.Platform.FirebaseEditorDispatcher:Update()
        /// UnityEditor.EditorApplication:Internal_CallUpdateFunctions()
        /// </summary>
        private async void FixedUpdate()
        {
            // This is in case we click the loginFromInspector box during Play.
            if (loginFromInspector && !IsLoggingIn) // We only want to call this once and wait until we're done logging in before relogging.
            {
                IsLoggingIn = true;

                // If the Device instance is already logged in, log it out to relog with new values later.
                if (notion != null && notion.IsLoggedIn) 
                {
                    try
                    {
                        await Logout();
                    }
                    catch (Exception e)
                    {
                        loginFromInspector = false;
                        IsLoggingIn = false;
                        Debug.Log(e);
                        return;
                    }
                }

                // Try to log in with the Device instance we have in the Inspector.
                // We may have swapped it during Play, who knows?
                try
                {
                    await Login(device.Email, device.Password);

                    Subscribe();
                    loginFromInspector = false;
                    IsLoggingIn = false;
                    return;
                }
                catch (Exception e)
                {
                    loginFromInspector = false;
                    IsLoggingIn = false;
                    Debug.Log(e);
                    return;
                }
            }

            // This is in case we click the loginWithOverrideData during Play.
            if (loginWithOverrideData && !IsLoggingIn) // We only want to call this once and wait until we're done logging in before relogging.
            {
                IsLoggingIn = true;

                // If the Device instance is already logged in, log it out to relog with the override values later.
                if (notion != null && notion.IsLoggedIn)
                {
                    try
                    {
                        await Logout();
                    }
                    catch (Exception e)
                    {
                        loginWithOverrideData = false;
                        IsLoggingIn = false;
                        Debug.Log(e);
                        return;
                    }
                }

                // Try to log in using the override values we wrote in the Inspector.
                try
                {
                    device.Email = overrideEmail;
                    device.Password = overridePassword;
                    device.DeviceId = overrideDeviceId;
                    device.IsRemembered = overrideIsRemembered;
                    await Login(device.Email, device.Password);
                    Subscribe();
                    loginWithOverrideData = false;
                    IsLoggingIn = false;
                    return;
                }
                catch (Exception e)
                {
                    loginWithOverrideData = false;
                    IsLoggingIn = false;
                    Debug.Log(e);
                    return;
                }
            }

            // Try to Update the Status
            try
            {
                await UpdateStatus();
            }
            catch (Exception e)
            {
                Debug.Log(e);
            }
        }

        // Let's logout when the script is disabled. Either by unchecking the Enable box in the Inspector or by stopping the game in the Editor.
        private async void OnDisable()
        {
            if (notion == null) return;
            if (!notion.IsLoggedIn) return;

            // Wrapping because Logout is meant to be invoked and forgotten about for use in button callbacks.
            // Also, you have to use try/catch to avoid having warnings and errors stop the game.
            // Unity may still crash if too many warnings come up after a logout.
            // I used a FixedUpdate() to reduce the number of calls that could be interrupted upon logout.
            // You could also use AfterUpdate(). Consult Unity documentation to undertsand the difference.
            try
            {
                await Task.Run(() => Logout());
            }
            catch (Exception e)
            {

                Debug.Log(e.ToString());
            }

        }

        // This saves the Device instance info when quitting the game if IsRemembered is true. 
        // The Device instance does not retain information in the Build, only in the Editor.
        // Thus the need to save and encrypt the SaveData.
        private void OnApplicationQuit()
        {
            ClearDeviceInfo();
            SaveDeviceData(); 
        }



        ////////////////////////

        //// PUBLIC METHODS ////

        ////////////////////////

        // Turns out we can login without a device ID, in which case Firabease will just log into our default or last device.
        // Be wary of the await commands, they tend to throw errors if something isn't perfect.
        // This template catches any exceptions in the TitleScreenGUI class when logging in from the GUI.
        public async Task Login(string _inputEmail, string _inputPassword)
        {
            device.Email = _inputEmail;
            device.Password = _inputPassword;

            controller = new FirebaseController();
            await controller.Initialize();

            notion = new Notion(controller);
            await notion.Login(device);

            IsLoggedIn = true;

            //Debug.Log( "Logged In" );

        }


        // This method throws some warnings when logging out.
        // Read the summary in the private method FixedUpdate().
        // I'll deal with these warnings in a future version.
        // For now, the end user isn't bothered too much.
        public async Task Logout()
        {
            if ( notion != null )
            await notion.Logout();

            if( controller.NotionDatabase != null )
            controller.Logout();

            controller = null;
            notion = null;

            ClearDeviceInfo();

            IsLoggedIn = false;
            IsSubscribed = false;

            ClearStatusValues();
            ClearSubscriptionValues();

            //Debug.Log( "Logged Out" );
        }


        // Public methods to access the private data from the Device instance. Only the ones of use to me in this Template.
        // Feel free to come up with your own as the need arises.

        public void Remember(bool isRemembered)
        {
            device.IsRemembered = isRemembered;
        }

        public bool IsOnline()
        {
            if (deviceStatus == "Online") return true;
            return false;
        }

        public string GetDeviceEmail()
        {
            return device.Email;
        }

        public string GetDevicePassword()
        {
            return device.Password;
        }

        public string GetDeviceId()
        {
            return device.DeviceId;
        }


        // I conceived this method to be as user-friendly as possible.
        // This way we can send the name of the device instead of the super long Device ID.
        public async Task SelectDevice( string selectedDeviceNickname )
        {
            selectedDeviceId = "Fetching device...";

            var devicesInfo = await notion.GetDevices();

            foreach ( DeviceInfo device in devicesInfo )
            {
                if ( device.DeviceNickname == selectedDeviceNickname )
                {
                    this.device.DeviceId = device.DeviceId;
                    
                    controller = new FirebaseController();
                    await controller.Initialize();

                    notion = new Notion( controller );
                    await notion.Login( this.device );

                    Subscribe();

                    //Debug.Log( selectedDeviceNickname + "  is streaming." );
                }
                return;
            }
        }


        // And the fun begins!
        // If you're adding subscriptions to other Handlers, be sure to update this method.
        public void Subscribe()
        {
            if ( IsSubscribed ) return;

            if ( subscribeToCalm ) { SubscribeCalm(); }
            if ( subscribeToFocus ) { SubscribeFocus(); }
            if ( subscribeToAccelerometer ) { SubscribeAccelerometer(); }

            IsSubscribed = true;
        }
        
    }

    /// <summary>
    /// This is the serializable class to be able to parse the data from the Device instance into a .dat file.
    /// The Device class is a Scriptable Object, so it's not serializable; thus it can't be saved.
    /// It's a fundamental part of Ryan Turney's SDK architecture, so I have been careful not to break the Device class.
    /// I have seriously considered making it serializaable, but I decided to not open that can of worms for the time being.
    /// My hypothesis is that Firebase needs a Scriptable Object to properly function.
    /// I'll try mucking around with it at a later date. For now, "if it is not broke, don't fix it!"
    /// </summary>
    [Serializable]
    class SaveData
    {
        public string savedEmail;
        public string savedPassword;
        public string savedDeviceId;
        public bool savedIsRemembered;
    }
}
