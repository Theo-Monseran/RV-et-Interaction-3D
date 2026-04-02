using UnityEngine;

public class TargetButton : MonoBehaviour
{
    [Header("Visuals")]
    [SerializeField] private Color activeColor = Color.yellow;
    [SerializeField] private Color inactiveColor = Color.gray;

    private AudioSource audioSource;
    private Renderer buttonRenderer;
    private Material buttonMaterial;
    private bool isActive = false;

    public bool IsActive => isActive;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        // Le Renderer est sur un objet enfant (le mesh), pas sur le parent
        buttonRenderer = GetComponentInChildren<Renderer>();

        if (buttonRenderer != null)
        {
            // On clone le material pour ne pas affecter les autres boutons
            buttonMaterial = buttonRenderer.material;
        }
        else
        {
            Debug.LogWarning($"[TargetButton] Aucun Renderer trouvé sur {gameObject.name} ou ses enfants.");
        }

        TurnOff();
    }

    // Active le bouton : change la couleur et joue le son spatialisé.
    public void TurnOn()
    {
        isActive = true;

        if (buttonMaterial != null)
        {
            buttonMaterial.color = activeColor;
            buttonMaterial.EnableKeyword("_EMISSION");
            buttonMaterial.SetColor("_EmissionColor", activeColor * 2f);
        }

        audioSource.Play();
        //audioSource.PlayOneShot(audioSource.clip);
    }

    // Désactive le bouton visuellement.
    public void TurnOff()
    {
        isActive = false;

        if (buttonMaterial != null)
        {
            buttonMaterial.color = inactiveColor;
            buttonMaterial.DisableKeyword("_EMISSION");
            buttonMaterial.SetColor("_EmissionColor", Color.black);
        }
    }
}
