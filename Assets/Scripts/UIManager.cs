using UnityEngine;

public class UIManager : MonoBehaviour
{
    GameManager gameManager;

    [Header("Canvas")]
    public GameObject OutlineCanvas;
    public Canvas outlineCanvas;
    public GameObject MainCanvas;

    [Header("Panels")]
    public GameObject MainMenuPanel;
    public GameObject DisplayPanel;

    public static UIManager Instance;
    public void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    public void Start()
    {
        gameManager = GameManager.Instance;
        
        // Subscribe to game manager events
        gameManager.OnTap += OnOpenDisplayPanel;
        gameManager.OnCloseDisplay += OnCloseDisplayPanel;

        outlineCanvas.planeDistance = 100;
        MainMenuPanel.SetActive(true);
        DisplayPanel.SetActive(false);
    }

    public void OnOpenDisplayPanel() 
    {
        outlineCanvas.planeDistance = 30f;
        MainMenuPanel.SetActive(false);
        DisplayPanel.SetActive(true);
    }

    public void OnCloseDisplayPanel() 
    {
        outlineCanvas.planeDistance = 100;
        MainMenuPanel.SetActive(true);
        DisplayPanel.SetActive(false);
    }
}
