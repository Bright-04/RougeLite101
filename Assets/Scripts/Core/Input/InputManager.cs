using UnityEngine;

using UnityEngine.SceneManagement;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }
    public PlayerControls Controls { get; private set; }

    private bool uiActive = false;
    public bool IsUIActive() => uiActive;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Scene activeScene = SceneManager.GetActiveScene();
            bool thisIsInActiveScene = gameObject.scene == activeScene;
            bool instanceIsInActiveScene = Instance.gameObject.scene == activeScene;

            if (thisIsInActiveScene && !instanceIsInActiveScene)
            {
                Destroy(Instance.gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        Instance = this;

        if (Controls == null)
        {
            Controls = new PlayerControls();
        }

        EnableGameplayMaps();
    }
    private void OnDestroy()
    {
        if (Instance == this)
        {
            // Properly disable all input actions before destroying
            if (Controls != null)
            {
                Controls.Movement.Disable();
                Controls.Combat.Disable();
                Controls.NavigateUI.Disable();
                Controls.UI.Disable();
                Controls.Dispose();
            }
            
            Instance = null;
        }
    }
    public void EnableUIMap()
    {
        uiActive = true;

        Controls.Movement.Disable();
        Controls.Combat.Disable();
        Controls.NavigateUI.Disable();

        Controls.UI.Enable();
    }

    public void DisableUIMap()
    {
        uiActive = false;    

        EnableGameplayMaps();
    }

    private void EnableGameplayMaps()
    {
        Controls.Movement.Enable();
        Controls.Combat.Enable();
        Controls.NavigateUI.Enable();

        Controls.UI.Disable();
    }

}
