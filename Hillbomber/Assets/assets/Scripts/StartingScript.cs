using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class StartingScript : MonoBehaviour
{
    public static StartingScript Instance;

    public StartingScript()
    {
        if (Instance != null) { Destroy(this); return; };
        Instance = this;
    }

    public void Start()
    {
        Time.timeScale = 0f;
    }
    
    public void OnEnterAction(InputAction.CallbackContext ctx)
    {
        Time.timeScale = 1f;
    }

    public void OnDeath()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
