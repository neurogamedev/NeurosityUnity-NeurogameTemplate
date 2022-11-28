using UnityEngine;

// A quick experiment to showcase how to use information from the NotionInterfacer into any objects in-game.
// In this example, the more focused the player, the higher the sphere floats on the Y axis.
namespace Notion.Unity
{
    public class FocusSphere : MonoBehaviour
    {
        public float maxHeight = 1; //Meters over initial position.
        //[SerializeField, Range(0, 1)] public float testNumber = 0;

        private NotionInterfacer deviceInterface;
        private Vector3 initialPosition;

        void OnEnable()
        {
            deviceInterface = FindObjectOfType<NotionInterfacer>();
            initialPosition = this.transform.position;
        }

        void Bobble()
        {
            if (deviceInterface == null || deviceInterface.IsOnline() == false)
            {
                this.transform.position = initialPosition;
                return;
            }

            var step = 0.125f * Time.deltaTime;
            Vector3 target = new Vector3(initialPosition.x, (initialPosition.y + (maxHeight * deviceInterface.focusScore)), initialPosition.z);
            this.transform.position = Vector3.MoveTowards(transform.position, target, step);
        }

        // Update is called once per frame
        void FixedUpdate()
        {
            Bobble();

        }
    }
}
