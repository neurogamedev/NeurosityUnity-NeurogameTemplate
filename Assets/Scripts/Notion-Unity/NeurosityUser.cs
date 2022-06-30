using Firebase.Auth;
using Firebase.Database;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Notion.Unity
{
    public class NeurosityUser
    {
        public bool IsLoggedIn { get; private set; }
        public string UserId { get; private set; }
        public List<DeviceInfo> deviceInfoCache { get; set; }

        private readonly FirebaseController _firebase;
        private readonly FirebaseUser _firebaseUsers;
        private readonly DatabaseReference _devicesReference;

        private DatabaseReference _deviceRef;

        public NeurosityUser(FirebaseUser firebaseUser, FirebaseController firebase)
        {
            _firebase = firebase;
            _firebaseUsers = firebaseUser;
            UserId = _firebaseUsers.UserId;
            _devicesReference = _firebase.NotionDatabase.GetReference($"users/{UserId}/devices");
        }

        public async Task<IEnumerable<DeviceInfo>> GetDevices()
        {
            var devicesSnapshot = await _devicesReference.GetValueAsync();

            Dictionary<string, object> registeredDevices = devicesSnapshot.Value as Dictionary<string, object>;
            if (registeredDevices == null) return null;

            var deviceKeys = registeredDevices.Keys;
            List<DeviceInfo> devicesInfo = new List<DeviceInfo>(deviceKeys.Count);

            foreach (string deviceId in deviceKeys)
            {
                var infoSnapshot = await _firebase.NotionDatabase.
                    GetReference($"devices/{deviceId}/info").GetValueAsync();

                string json = infoSnapshot.GetRawJsonValue();
                DeviceInfo info = JsonConvert.DeserializeObject<DeviceInfo>(json);
                devicesInfo.Add(info);
            }

            return devicesInfo;
        }

        /// <summary>
        /// Notes from Diego Saldivar:
        /// Firebase and Unity do not play nice when it comes to awaiting a GetValueAsync() which does not complete its task.
        /// The will be no Fault, Cancelation or Exception of any sort. 
        /// Unity will simply continue with the Main thread to the detriment of anything that is awaiting.
        /// There have been numerous reports on this misbehavior with no fix as of mid-2022.
        /// Unity devs must thus hard-code a delay where an await would usually suffice.
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<DeviceInfo>> GetDevicesSlow()
        {
            // Modification from original SDK by Ryan Turney. I saw no point in parsing the DataBase over and over when you can do it once and save the list of Devices in working memory.
            if (deviceInfoCache != null) return deviceInfoCache;

            // This snapshot may include invalid Device IDs. May have to check why on Neurosity's side.
            var devicesSnapshot = await _devicesReference.GetValueAsync();
            Dictionary<string, object> registeredDevices = devicesSnapshot.Value as Dictionary<string, object>;

            // In case there are no registered devices on your neurosity account.
            if (registeredDevices == null) return null;

            // Let's make a list of all DDevice IDs in youa account... including the non-valid ones.
            var deviceKeys = registeredDevices.Keys;
            List<DeviceInfo> devicesInfo = new List<DeviceInfo>(deviceKeys.Count);

            // This is the maxumum ammount in second we'll wait for a response from Firebase. This is where we come up with our own custom 'await'.
            float maxWaitSeconds = 1f;

            // Let's try to get all the data from Firebase, for each Device IDs we got from before.
            foreach (string deviceId in deviceKeys)
            {
                // This is the boolean which stop the 'while(true) Delay' at the end of this loop.
                bool waiting = true;

                // This should be awaited but an invalid Device ID in the DataBase will gut the waiting functionality and Unity will just move on with the rest of the thread.
                // Ignore warning CS4014: 'Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the 'await' operator to the result of the call.'
                _firebase.NotionDatabase.
                    GetReference($"devices/{deviceId}/info").GetValueAsync().ContinueWith(task => {
                        if (task.IsFaulted || task.IsCanceled)          // In theory, this should save us from waiting forever or allow us to move on with the async thread... but, in practice, it doesn't.
                        {
                            waiting = false;
                        }
                        else if (task.IsCompleted)                      // We found data on the server related to the deviceId.
                        {
                            var infoSnapshot = task.Result;
                            string json = infoSnapshot.GetRawJsonValue();
                            DeviceInfo info = JsonConvert.DeserializeObject<DeviceInfo>(json);
                            devicesInfo.Add(info);

                            waiting = false;
                        }

                    });

                // And now we start our stopwatch.
                float waitCount = 0f;
                float waitIncrement = 0.1f;

                // This while loop will Delay up to 'maxWaitSeconds' or until the task above is finished. Whichever is first.
                while (waiting && waitCount < maxWaitSeconds)
                {
                    waitCount += waitIncrement;
                    await Task.Delay(TimeSpan.FromSeconds(waitIncrement));
                }

            }

            // We save the info in our local cache and ALWAYS return the cache.
            deviceInfoCache = devicesInfo;
            return deviceInfoCache;
        }

        public async Task<DeviceInfo> GetSelectedDevice()
        {
            var devices = await GetDevicesSlow();                   // Modified from Ryan Turney's SDK. See sumamry in NeurosityUser.cs
            DeviceInfo selectedDevice = devices.FirstOrDefault();
            _deviceRef = _firebase.NotionDatabase.GetReference($"devices/{selectedDevice.DeviceId}");
            return selectedDevice;
        }

        public async Task<DeviceStatus> GetSelectedDeviceStatus()
        {
            var selectedDevice = await GetSelectedDevice();

            var statusSnapshot = await _firebase.NotionDatabase.
                GetReference($"devices/{selectedDevice.DeviceId}/status").GetValueAsync();
            string json = statusSnapshot.GetRawJsonValue();

            return JsonConvert.DeserializeObject<DeviceStatus>(json);
        }

        public async Task<DeviceStatus> GetDeviceStatus(string DeviceId)
        {
            var selectedDevice = await GetSelectedDevice();

            var statusSnapshot = await _firebase.NotionDatabase.
                GetReference($"devices/{DeviceId}/status").GetValueAsync();
            string json = statusSnapshot.GetRawJsonValue();

            return JsonConvert.DeserializeObject<DeviceStatus>(json);
        }

        public async Task UpdateSettings(Settings settings)
        {
            if (_deviceRef == null) return;
            await _deviceRef.Child("settings").SetValueAsync(settings.ToDictionary());
        }

        public async Task RemoveDevice(string deviceId)
        {
            string claimedByPath = $"devices/{deviceId}/status/claimedBy";
            string userDevicePath = $"users/{UserId}/devices/{deviceId}";
            var claimedByRef = _firebase.NotionDatabase.GetReference(claimedByPath);
            var userDeviceRef = _firebase.NotionDatabase.GetReference(userDevicePath);

            await claimedByRef.RemoveValueAsync();
            await userDeviceRef.RemoveValueAsync();
        }
    }
}