using System;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    // Events
    public event Action<char> OnLetterPressed;
    public event Action OnEnterPressed;
    public event Action OnDeletePressed;

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        // Check for letter keys (A-Z)
        if (Input.anyKeyDown)
        {
            foreach (KeyCode keyCode in Enum.GetValues(typeof(KeyCode)))
            {
                // Check if it's a letter key (A-Z)
                if (Input.GetKeyDown(keyCode) && keyCode >= KeyCode.A && keyCode <= KeyCode.Z)
                {
                    char letter = keyCode.ToString()[0];
                    OnLetterPressed?.Invoke(letter);
                    return;
                }
            }
        }

        // Check for Enter key
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            OnEnterPressed?.Invoke();
        }

        // Check for Delete/Backspace key
        if (Input.GetKeyDown(KeyCode.Backspace) || Input.GetKeyDown(KeyCode.Delete))
        {
            OnDeletePressed?.Invoke();
        }
    }
}
