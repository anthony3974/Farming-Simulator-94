using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The controls pop-up in GameScene. A real object in the scene hierarchy ("Help Menu", made by
/// Tools > Build Help Menu), so its panels and texts can be moved, resized, restyled and reworded in the editor.
/// This script only opens and closes it. Press H, or the HELP button on touch, to open and close it; a tap
/// anywhere on it also closes it. There are two panels: <see cref="keyboardPanel"/> on PC and
/// <see cref="touchPanel"/> when the touch controls are on (phones, or Show Touch Controls in the editor).
/// </summary>
public class HelpMenu : MonoBehaviour
{
    [Header("Scene objects (set by the builder; keep them set if you rearrange)")]
    [Tooltip("The part shown while open (full-screen dimmer holding both panels). Hidden when the game runs.")]
    public GameObject menuRoot;
    [Tooltip("Optional: a full-screen button on the menu; a tap on it closes the menu.")]
    public Button closeButton;
    [Tooltip("Shown on PC: the controls with their keys.")]
    public GameObject keyboardPanel;
    [Tooltip("Shown with touch controls: the controls with their buttons.")]
    public GameObject touchPanel;

    private InputManager input;

    public bool IsOpen => menuRoot != null && menuRoot.activeSelf;

    void Start()
    {
        input = FindFirstObjectByType<InputManager>();
        if (menuRoot == null || keyboardPanel == null || touchPanel == null || input == null)
        {
            string missing = (menuRoot == null ? " Menu Root" : "") + (keyboardPanel == null ? " Keyboard Panel" : "")
                + (touchPanel == null ? " Touch Panel" : "") + (input == null ? " (no InputManager in the scene)" : "");
            Debug.LogError($"Help menu turned off: '{name}' is missing{missing}. Drag the objects back into its Help Menu slots, "
                + "or run Tools > Build Help Menu again.", this);
            enabled = false;
            return;
        }

        bool touch = input.TouchControlsActive;
        keyboardPanel.SetActive(!touch);
        touchPanel.SetActive(touch);
        if (closeButton != null) closeButton.onClick.AddListener(Close);
        menuRoot.SetActive(false);
    }

    void Update()
    {
        if (input.Help) menuRoot.SetActive(!menuRoot.activeSelf);
    }

    public void Close()
    {
        menuRoot.SetActive(false);
    }
}
