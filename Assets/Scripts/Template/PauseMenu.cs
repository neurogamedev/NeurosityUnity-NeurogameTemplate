using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;


/// <summary>
/// This is the code for the pause menu that you can pop-up during gameplay.
/// </summary>
namespace StarterAssets
{

    public class PauseMenu : MonoBehaviour
    {
        // This is so you can access inputs from the PlayerInput class in the Player Armature.
        // Your workflow will obviously vary depending on your game design.
        private StarterAssetsInputs starterAssetsInputs;

        // This is where I added the Pause action in the Input Actions Template.
        // Read the Starter Pack documentation in Assets > StarterAssets > Starter Assets_Documentation_v1.1.pdf
        private TemplateInputActions inputActions;

        // This action is mapped to the Esc key and to the Start button in a game controller.
        private InputAction pause;

        // The foundation of this button.
        public static bool gameIsPaused;

        // This is the Panel with the Pause menu pop-up and buttons.
        // !! This is the panel we want to activate and deactivate. Not its parent.
        public GameObject pauseMenu;

        // Let's get started!
        void Awake()
        {
            if (pauseMenu.activeSelf) pauseMenu.SetActive(false);
            inputActions = new TemplateInputActions();
            starterAssetsInputs = FindObjectOfType<StarterAssetsInputs>();
        }

        private void OnEnable()
        {
            pause = inputActions.UI.Pause;
            pause.Enable();

            pause.performed += PauseSwitch;
        }

        private void OnDisable()
        {
            pause.Disable();      
        }

        // This is the Pause button logic.
        public void PauseSwitch(InputAction.CallbackContext context)
        {

            gameIsPaused = !gameIsPaused;

            if (gameIsPaused)
            {
                Time.timeScale = 0f;                                // Time is stopped. Only unscaled animations will continue playing.
                pauseMenu.SetActive(true);                          // Now we open the Pause menu.
                starterAssetsInputs.cursorInputForLook = false;     // Tell the Player Input to not look around with the mouse.
                Cursor.lockState = CursorLockMode.None;             // Override any lock modes that prevent clicks from being registered.
            }
            else
            {
                pauseMenu.SetActive(false);                         // Close the Pause menu.
                starterAssetsInputs.cursorInputForLook = true;      // Let the camera be governed by the mouse.
                Cursor.lockState = CursorLockMode.Locked;           // Ignore any button clicks in the GUI... so that you can use them for gameplay.
                Time.timeScale = 1;                                 // Time flows for everyone now.
            }
        }

        // Method for the Main Menu button.
        public void LoadStartMenu(string sceneName)
        {
            Time.timeScale = 1;                                     // Be sure to bring back a normal Timescale when you switch scenes!


            if (SceneUtility.GetBuildIndexByScenePath(sceneName) != -1)                 // Sanity check.  
            {
                AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);      // Load the scene.
            }
            else
            {
                AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(0);              // If scene could not be found, just load the scene in BuildIndex 0. The one at the top of your Scenes in Build list.
            }
        }

        // Self evident.
        public void QuitGame()
        {
            Application.Quit();
        }

    }

}
