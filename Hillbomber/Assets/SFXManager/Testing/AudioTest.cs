using UnityEngine;
using UnityEngine.InputSystem;

public class AudioTest : MonoBehaviour
{
    [Tooltip("a test sound effect played when Space is pressed")]
    public AudioClip _audioClip;

    void Update()
    {
        if(Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            SFXManager.Instance.PlaySFX(_audioClip, transform, 1f);
        }
    }
}
