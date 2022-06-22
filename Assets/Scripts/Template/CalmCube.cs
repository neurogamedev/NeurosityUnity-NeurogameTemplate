using UnityEngine;

// A quick experiment to showcase how to use information from the NotionInterfacer into any objects in-game.
// In this example, the calmer the player, the slower the cube spins on its Y axis.
namespace Notion.Unity
{
    public class CalmCube : MonoBehaviour
    {
        public float spinSpeed = 10f; //Degrees per FixedUpdate.
        //[SerializeField, Range(0, 1)] public float testNumber = 0;

        private NotionInterfacer deviceInterface;
        private Quaternion initialRotation;

        void OnEnable()
        {
            deviceInterface = FindObjectOfType<NotionInterfacer>();
            initialRotation = this.transform.localRotation;
        }

        void Spin()
        {

            if (deviceInterface == null || deviceInterface.IsOnline() == false)
            {
                this.transform.localRotation = initialRotation;
                return;
            }

            this.transform.Rotate(new Vector3(0, spinSpeed - (deviceInterface.calmScore * spinSpeed), 0), Space.Self);
        }

        void FixedUpdate()
        {
            Spin();
            
        }
    }
}
