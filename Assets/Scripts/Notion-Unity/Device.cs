using UnityEngine;

namespace Notion.Unity
{
    [CreateAssetMenu]
    public class Device : ScriptableObject
    {
        [SerializeField]
        private string _email;

        [SerializeField]
        private string _password;

        [SerializeField]
        private string _deviceId;

        [SerializeField]
        private bool _isRemembered;

        public string Email { get { return _email; } set { _email = value; } }
        public string Password { get { return _password; } set { _password = value; } }
        public string DeviceId { get { return _deviceId; } set { _deviceId = value; } }
        public bool IsRemembered { get { return _isRemembered; } set { _isRemembered = value; } }

        public bool IsValid => 
            !string.IsNullOrEmpty(_email) && 
            !string.IsNullOrEmpty(_password) && 
            !string.IsNullOrEmpty(DeviceId);
    }
}