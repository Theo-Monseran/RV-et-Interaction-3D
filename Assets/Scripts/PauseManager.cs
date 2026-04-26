using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

/// <summary>
/// PauseManager — Menu pause VR pour Meta Quest.
///
/// Fonctionnement :
///   - Bouton Menu (manette gauche Quest) toggle la pause
///   - Time.timeScale = 0 fige les timers du GameManager
///   - La musique est pausée / reprise proprement
///   - Le Canvas pause s'affiche devant le joueur à chaque ouverture
///   - Boutons : Reprendre / Rejouer / Quitter
///
/// Setup Unity :
///   1. Crée un GameObject "PauseManager" dans ta scène Game
///   2. Attache ce script dessus
///   3. Crée un Canvas World Space "PauseCanvas" (enfant de Main Camera ou indépendant)
///      → Layer : UI | Distance recommandée : ~1.5m devant la caméra
///   4. Dans le Canvas, crée 3 boutons XR UI : Reprendre, Rejouer, Quitter
///   5. Relie les références dans l'Inspector
///   6. Dans Project Settings > Input System > XRI, vérifie que
///      "Menu Button" est bien mappé (c'est le cas par défaut avec XRI)
/// </summary>
public class PauseManager : MonoBehaviour
{
    // ════════════════════════════════════════════════════════════════════
    // INSPECTOR
    // ════════════════════════════════════════════════════════════════════

    [Header("── Références ──────────────────────────")]
    [Tooltip("Le Canvas World Space du menu pause")]
    [SerializeField] private GameObject pauseCanvas;

    [Tooltip("Référence au GameManager de la scène")]
    [SerializeField] private GameManager gameManager;

    [Tooltip("La Main Camera VR (ou XR Origin > Camera Offset > Main Camera)")]
    [SerializeField] private Transform vrCamera;

    [Header("── Input ────────────────────────────────")]
    [Tooltip("Action Input System mappée sur le bouton Menu Quest (manette gauche)")]
    [SerializeField] private InputActionReference menuButtonAction;

    [Header("── Audio ────────────────────────────────")]
    [Tooltip("AudioSource musique du GameManager (pour la pauser)")]
    [SerializeField] private AudioSource musicSource;

    [Header("── Positionnement du Canvas ─────────────")]
    [Tooltip("Distance devant la caméra à laquelle apparaît le menu (en mètres)")]
    [SerializeField] private float distanceDevantCamera = 1.5f;

    // ════════════════════════════════════════════════════════════════════
    // ÉTAT INTERNE
    // ════════════════════════════════════════════════════════════════════

    private bool isPaused = false;

    // ════════════════════════════════════════════════════════════════════
    // INIT
    // ════════════════════════════════════════════════════════════════════

    void Awake()
    {
        // S'assure que le canvas est caché au démarrage
        if (pauseCanvas != null)
            pauseCanvas.SetActive(false);
    }

    void OnEnable()
    {
        if (menuButtonAction != null)
        {
            menuButtonAction.action.Enable();
            menuButtonAction.action.performed += OnMenuButtonPressed;
        }
    }

    void OnDisable()
    {
        if (menuButtonAction != null)
            menuButtonAction.action.performed -= OnMenuButtonPressed;
    }

    // ════════════════════════════════════════════════════════════════════
    // INPUT
    // ════════════════════════════════════════════════════════════════════

    private void OnMenuButtonPressed(InputAction.CallbackContext ctx)
    {
        // Empêche d'ouvrir la pause si le jeu est déjà terminé
        // (GameManager.isPlaying est privé → on vérifie via Time.timeScale ou un flag)
        TogglePause();
    }

    // ════════════════════════════════════════════════════════════════════
    // LOGIQUE PAUSE
    // ════════════════════════════════════════════════════════════════════

    public void TogglePause()
    {
        if (isPaused)
            Reprendre();
        else
            Pauser();
    }

    public void Pauser()
    {
        isPaused = true;

        // Fige tous les timers (Update du GameManager utilise Time.deltaTime)
        Time.timeScale = 0f;

        // Pause la musique
        if (musicSource != null && musicSource.isPlaying)
            musicSource.Pause();

        // Positionne le canvas devant le joueur
        PositionnerCanvas();

        // Affiche le menu
        if (pauseCanvas != null)
            pauseCanvas.SetActive(true);

        Debug.Log("[Pause] Jeu en pause");
    }

    public void Reprendre()
    {
        isPaused = false;

        // Reprend le temps
        Time.timeScale = 1f;

        // Reprend la musique
        if (musicSource != null)
            musicSource.UnPause();

        // Cache le menu
        if (pauseCanvas != null)
            pauseCanvas.SetActive(false);

        Debug.Log("[Pause] Jeu repris");
    }

    /// <summary>
    /// Appelé par le bouton "Rejouer" du menu pause.
    /// Remet Time.timeScale à 1 avant de recharger la scène.
    /// </summary>
    public void Rejouer()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Game");
    }

    /// <summary>
    /// Appelé par le bouton "Quitter" du menu pause.
    /// Retourne au menu principal.
    /// </summary>
    public void Quitter()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu"); // adapte le nom si différent
    }

    // ════════════════════════════════════════════════════════════════════
    // POSITIONNEMENT DU CANVAS
    // ════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Place le menu pause devant la caméra VR à chaque ouverture.
    /// Le menu fait toujours face au joueur.
    /// </summary>
    private void PositionnerCanvas()
    {
        if (vrCamera == null || pauseCanvas == null) return;

        // Position : devant la caméra, à hauteur des yeux, sans tenir compte du tilt vertical
        Vector3 direction = vrCamera.forward;
        direction.y = 0f;
        if (direction == Vector3.zero) direction = Vector3.forward;
        direction.Normalize();

        Vector3 position = vrCamera.position + direction * distanceDevantCamera;
        pauseCanvas.transform.position = position;

        // Rotation : le canvas regarde vers le joueur
        pauseCanvas.transform.rotation = Quaternion.LookRotation(direction);
    }
}
