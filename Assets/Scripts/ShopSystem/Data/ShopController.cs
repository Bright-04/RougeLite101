using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class ShopController : MonoBehaviour
{

    [Header("UI References")]
    [SerializeField]
    public ShopUI shopUI;

    private void Awake()
    {
        shopUI.OnClosingShop += CloseShop;
    }

    private void OnDestroy()
    {
        shopUI.OnClosingShop -= CloseShop;
    }

    void Start()
    {
        // Đợi đến Start để đảm bảo InputManager đã Awake
        if (InputManager.Instance == null)
        {
            Debug.LogError("InputManager.Instance is null! Make sure InputManager prefab exists in the scene.");
            return;
        }
        //playerControls = InputManager.Instance.Controls;
    }

    public void OpenShop(ShopInventorySO shopData,GameObject interactor)
    {
        if (!InputManager.Instance.IsUIActive())
        {
            shopUI.ShowShop();
            // Disable gameplay inputs
            InputManager.Instance.EnableUIMap();
            Debug.Log("OPEN Shop");
        }
    }

    public void CloseShop()
    {
        if (InputManager.Instance.IsUIActive())
        {
            shopUI.HideShop();
            // Enable gameplay inputs
            InputManager.Instance.DisableUIMap();
            Debug.Log("CLOSE Shop");
        }
    }
}
