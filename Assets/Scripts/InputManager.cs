using UnityEngine;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }
    public PlayerControls Controls { get; private set; }

    private bool uiActive = false;
    public bool IsUIActive() => uiActive;

    private void Awake()
    {
        //logic Singleton
        // Nếu chưa có instance → đây là instance đầu tiên
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Controls = new PlayerControls();
            EnableGameplayMaps();
        }
        else
        {
            // Đã có instance rồi → xóa cái mới
            Destroy(gameObject);
        }
    }
    private void OnDestroy()
    {
        if (Instance == this)
        {
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
