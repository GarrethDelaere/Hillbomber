using UnityEngine;
using UnityEngine.SceneManagement;

public class StartingScript : MonoBehaviour
{
    public static StartingScript Instance;

    public StartingScript()
    {
        if (Instance != null) { Destroy(this); return; };
        Instance = this;
    }

    public void OnDeath()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
