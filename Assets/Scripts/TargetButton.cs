using UnityEngine;

/// <summary>
/// TargetButton — Bouton de jeu VR.
/// Gère les couleurs (Normal/Extra/Piège), l'émission lumineuse et les 3 sons.
/// </summary>
public class TargetButton : MonoBehaviour
{
    // ── Types ─────────────────────────────────────────────────────────
    public enum ButtonType { Normal, Extra, Piege }

    [Header("Couleurs")]
    public Color colorJaune    = Color.yellow;
    public Color colorBleu     = new Color(0.2f, 0.5f, 1f);
    public Color colorVert     = new Color(0.1f, 0.9f, 0.3f);
    public Color colorRouge    = new Color(1f, 0.15f, 0.15f);
    public Color inactiveColor = new Color(0.25f, 0.25f, 0.25f);

    [Header("Tes 3 sons")]
    [Tooltip("Joué quand le bouton s'allume")]
    public AudioClip sonEnCours;
    [Tooltip("Joué quand l'appui est correct ou piège esquivé")]
    public AudioClip sonSucces;
    [Tooltip("Joué quand raté, mauvais bouton ou piège appuyé")]
    public AudioClip sonEchec;

    // ── Privé ─────────────────────────────────────────────────────────
    private AudioSource  audioSource;
    private Renderer     buttonRenderer;
    private Material     buttonMaterial;
    private ButtonType   currentType;
    private bool         isActive;

    public bool       IsActive => isActive;
    public ButtonType Type     => currentType;

    // ── Init ──────────────────────────────────────────────────────────
    void Awake()
    {
        audioSource    = GetComponent<AudioSource>();
        buttonRenderer = GetComponentInChildren<Renderer>();
        if (buttonRenderer != null)
            buttonMaterial = buttonRenderer.material;
        TurnOff();
    }

    // ── API publique ──────────────────────────────────────────────────

    /// <summary>Allume le bouton avec le type donné et joue sonEnCours.</summary>
    public void TurnOn(ButtonType type)
    {
        isActive    = true;
        currentType = type;

        Color c = type switch
        {
            ButtonType.Extra => colorVert,
            ButtonType.Piege => colorRouge,
            _                => Random.value > 0.5f ? colorJaune : colorBleu
        };

        ApplyColor(c);
        PlayClip(sonEnCours);
    }

    /// <summary>Éteint le bouton.</summary>
    public void TurnOff()
    {
        isActive = false;
        ApplyColor(inactiveColor);
    }

    /// <summary>Joue sonSucces ou sonEchec selon le résultat.</summary>
    public void PlayResultSound(bool success)
    {
        PlayClip(success ? sonSucces : sonEchec);
    }

    // ── Helpers ───────────────────────────────────────────────────────
    private void ApplyColor(Color c)
    {
        if (buttonMaterial == null) return;
        buttonMaterial.color = c;
        if (c == inactiveColor)
        {
            buttonMaterial.DisableKeyword("_EMISSION");
            buttonMaterial.SetColor("_EmissionColor", Color.black);
        }
        else
        {
            buttonMaterial.EnableKeyword("_EMISSION");
            buttonMaterial.SetColor("_EmissionColor", c * 2f);
        }
    }

    private void PlayClip(AudioClip clip)
    {
        if (clip != null && audioSource != null)
            audioSource.PlayOneShot(clip);
    }
}
