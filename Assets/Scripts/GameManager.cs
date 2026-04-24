using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// GameManager — Jeu de réflexe VR complet.
///
/// Fonctionnalités :
///   - 3 vies affichées avec sprites coeur (plein / vide)
///   - Timer global paramétrable avec icône montre (rouge en urgence)
///   - Timer par bouton avec difficulté progressive
///   - Boutons Normal (jaune/bleu), Extra (vert +3pts +5s), Piège (rouge)
///   - Piège esquivable : ne pas appuyer = +1 pt, appuyer = -1 vie
///   - Pénalité temps global à chaque raté
///   - 3 sons sur les boutons : sonEnCours / sonSucces / sonEchec
///   - Son Game Over séparé selon la cause : vies = 0 ou chrono = 0
///   - Musique de fond en boucle
///   - Messages feedback : "Bien !", "Piège esquivé !", etc.
/// </summary>
public class GameManager : MonoBehaviour
{
    // ════════════════════════════════════════════════════════════════════
    // INSPECTOR
    // ════════════════════════════════════════════════════════════════════

    [Header("── Boutons de jeu ──────────────────────")]
    [SerializeField] private TargetButton[] buttons;

    [Header("── UI Texte ─────────────────────────────")]
    [Tooltip("Affiche le score")]
    [SerializeField] private TextMeshProUGUI scoreText;

    [Tooltip("Affiche le timer du bouton actif (ex: 2.4s)")]
    [SerializeField] private TextMeshProUGUI timerBoutonText;

    [Tooltip("Affiche le temps global restant (ex: 98s)")]
    [SerializeField] private TextMeshProUGUI timerGlobalText;

    [Tooltip("Affiche les messages feedback")]
    [SerializeField] private TextMeshProUGUI messageText;

    [Header("── UI Vies — Sprites Coeur ─────────────")]
    [Tooltip("Un composant Image par coeur dans l'ordre (coeur 1, 2, 3)")]
    [SerializeField] private Image[] heartImages;

    [Tooltip("Sprite coeur plein — vie active")]
    [SerializeField] private Sprite heartFull;

    [Tooltip("Sprite coeur vide — vie perdue")]
    [SerializeField] private Sprite heartEmpty;

    [Header("── UI Montre — Icône Timer Global ───────")]
    [Tooltip("Image de l'icône montre à côté du timer global")]
    [SerializeField] private Image clockIcon;

    [Tooltip("Couleur normale de l'icône montre")]
    [SerializeField] private Color clockColorNormal  = Color.white;

    [Tooltip("Couleur urgence quand il reste ≤ seuilUrgence secondes")]
    [SerializeField] private Color clockColorUrgence = new Color(1f, 0.3f, 0.3f);

    [Tooltip("Seuil en secondes pour passer en mode urgence")]
    [SerializeField] private float seuilUrgence = 20f;

    [Header("── Audio Global ────────────────────────")]
    [Tooltip("AudioSource pour la musique de fond")]
    [SerializeField] private AudioSource musicSource;

    [Tooltip("Clip musique de fond (joué en boucle)")]
    [SerializeField] private AudioClip musique;

    [Tooltip("AudioSource séparée pour les SFX globaux")]
    [SerializeField] private AudioSource sfxSource;

    [Tooltip("Son joué quand le joueur perd toutes ses vies")]
    [SerializeField] private AudioClip sonGameOver;

    [Tooltip("Son joué quand le chrono global arrive à zéro")]
    [SerializeField] private AudioClip sonChronoFini;

    [Header("── Difficulté ──────────────────────────")]
    [Tooltip("Durée totale du défi en secondes")]
    [SerializeField] private float tempsGlobalMax = 120f;

    [Tooltip("Temps initial accordé par bouton")]
    [SerializeField] private float tempsParBoutonInit = 3f;

    [Tooltip("Réduction du temps par bouton à chaque succès")]
    [SerializeField] private float reductionSucces = 0.05f;

    [Tooltip("Temps minimum par bouton (plancher)")]
    [SerializeField] private float tempsMin = 0.6f;

    [Tooltip("Pénalité en secondes sur le timer global à chaque raté")]
    [SerializeField] private float penaliteRate = 3f;

    [Tooltip("Délai entre l'extinction d'un bouton et l'allumage du suivant")]
    [SerializeField] private float delaiEntreBtn = 0.4f;

    [Tooltip("Nombre de vies au départ")]
    [SerializeField] private int viesDepart = 3;

    [Header("── Probabilités couleurs spéciales ─────")]
    [Tooltip("Probabilité qu'un bouton soit un Extra (vert, +3pts +5s)")]
    [Range(0f, 0.5f)]
    [SerializeField] private float probaExtra = 0.15f;

    [Tooltip("Probabilité qu'un bouton soit un Piège (rouge, ne pas appuyer)")]
    [Range(0f, 0.5f)]
    [SerializeField] private float probaPiege = 0.10f;

    // ════════════════════════════════════════════════════════════════════
    // ÉTAT INTERNE
    // ════════════════════════════════════════════════════════════════════

    private int          score;
    private int          vies;
    private float        tempsGlobalRestant;
    private float        tempsBoutonRestant;
    private float        tempsActuelParBouton;
    private bool         isPlaying;
    private bool         isWaiting;
    private TargetButton currentButton;
    private bool         urgenceActive;

    // ════════════════════════════════════════════════════════════════════
    // INITIALISATION
    // ════════════════════════════════════════════════════════════════════

    void Start()
    {
        foreach (var btn in buttons)
        {
            var interactable = btn.GetComponent<XRSimpleInteractable>();
            if (interactable == null)
            {
                Debug.LogWarning($"[GM] Pas de XRSimpleInteractable sur {btn.name}");
                continue;
            }
            TargetButton b = btn;
            interactable.selectEntered.AddListener((SelectEnterEventArgs _) => OnButtonPressed(b));
        }

        StartGame();
    }

    // ════════════════════════════════════════════════════════════════════
    // BOUCLE PRINCIPALE
    // ════════════════════════════════════════════════════════════════════

    void Update()
    {
        if (!isPlaying) return;

        // ── Timer global ──────────────────────────────────────────────
        tempsGlobalRestant -= Time.deltaTime;

        if (timerGlobalText != null)
            timerGlobalText.text = $"{Mathf.Max(0f, tempsGlobalRestant):F0}s";

        // Icône montre : bascule en urgence sous le seuil
        if (clockIcon != null)
        {
            bool urgence = tempsGlobalRestant <= seuilUrgence;
            if (urgence != urgenceActive)
            {
                urgenceActive   = urgence;
                clockIcon.color = urgence ? clockColorUrgence : clockColorNormal;
            }
        }

        if (tempsGlobalRestant <= 0f)
        {
            EndGame(causeVies: false);  // fin par chrono
            return;
        }

        if (isWaiting) return;

        // ── Timer du bouton actif ─────────────────────────────────────
        tempsBoutonRestant -= Time.deltaTime;

        if (timerBoutonText != null)
            timerBoutonText.text = $"{Mathf.Max(0f, tempsBoutonRestant):F1}s";

        if (tempsBoutonRestant <= 0f)
            OnBoutonExpire();
    }

    // ════════════════════════════════════════════════════════════════════
    // LOGIQUE DE JEU
    // ════════════════════════════════════════════════════════════════════

    public void StartGame()
    {
        score                = 0;
        vies                 = viesDepart;
        tempsGlobalRestant   = tempsGlobalMax;
        tempsActuelParBouton = tempsParBoutonInit;
        isPlaying            = true;
        isWaiting            = false;
        urgenceActive        = false;

        UpdateScoreUI();
        UpdateHeartsUI();
        ShowMessage("C'est parti !");

        if (clockIcon != null)
            clockIcon.color = clockColorNormal;

        if (musicSource != null && musique != null)
        {
            musicSource.clip = musique;
            musicSource.loop = true;
            musicSource.Play();
        }

        StartCoroutine(DemarrerAvecDelai());
    }

    private IEnumerator DemarrerAvecDelai()
    {
        yield return new WaitForSeconds(0.5f);
        ShowMessage("");
        ActivateRandomButton();
    }

    // ── Activation d'un bouton ────────────────────────────────────────
    private void ActivateRandomButton()
    {
        if (currentButton != null) currentButton.TurnOff();

        float r = Random.value;
        TargetButton.ButtonType type;
        if      (r < probaPiege)              type = TargetButton.ButtonType.Piege;
        else if (r < probaPiege + probaExtra) type = TargetButton.ButtonType.Extra;
        else                                  type = TargetButton.ButtonType.Normal;

        TargetButton next;
        int securite = 0;
        do
        {
            next = buttons[Random.Range(0, buttons.Length)];
            securite++;
        }
        while (next == currentButton && buttons.Length > 1 && securite < 20);

        currentButton      = next;
        tempsBoutonRestant = tempsActuelParBouton;
        currentButton.TurnOn(type);
    }

    // ── Appui sur un bouton ───────────────────────────────────────────
    private void OnButtonPressed(TargetButton button)
    {
        if (!isPlaying || isWaiting) return;

        if (button != currentButton || !button.IsActive)
        {
            button.PlayResultSound(false);
            PerdreUneVie("Mauvais bouton !");
            return;
        }

        switch (button.Type)
        {
            case TargetButton.ButtonType.Piege:
                button.PlayResultSound(false);
                PerdreUneVie("Piège ! Ne pas appuyer sur le rouge !");
                break;

            case TargetButton.ButtonType.Extra:
                button.PlayResultSound(true);
                score += 3;
                tempsGlobalRestant = Mathf.Min(tempsGlobalRestant + 5f, tempsGlobalMax);
                ShowMessage("+3 pts ! +5s bonus !");
                StartCoroutine(SuiteAvecDelai());
                break;

            default:
                button.PlayResultSound(true);
                score++;
                tempsActuelParBouton = Mathf.Max(tempsMin, tempsActuelParBouton - reductionSucces);
                ShowMessage("Bien !");
                StartCoroutine(SuiteAvecDelai());
                break;
        }

        UpdateScoreUI();
    }

    // ── Expiration du timer du bouton ─────────────────────────────────
    private void OnBoutonExpire()
    {
        if (currentButton == null) return;

        if (currentButton.Type == TargetButton.ButtonType.Piege)
        {
            currentButton.PlayResultSound(true);
            score++;
            ShowMessage("Piège esquivé ! +1");
            StartCoroutine(SuiteAvecDelai());
        }
        else
        {
            currentButton.PlayResultSound(false);
            PerdreUneVie("Trop lent !");
        }

        UpdateScoreUI();
    }

    // ── Perte de vie ──────────────────────────────────────────────────
    private void PerdreUneVie(string message)
    {
        vies--;
        UpdateHeartsUI();
        ShowMessage(message);

        tempsGlobalRestant = Mathf.Max(0f, tempsGlobalRestant - penaliteRate);

        if (vies <= 0)
        {
            EndGame(causeVies: true);  // fin par vies épuisées
            return;
        }

        StartCoroutine(SuiteAvecDelai());
    }

    // ── Délai entre deux boutons ──────────────────────────────────────
    private IEnumerator SuiteAvecDelai()
    {
        isWaiting = true;
        currentButton?.TurnOff();
        yield return new WaitForSeconds(delaiEntreBtn);
        isWaiting = false;
        ShowMessage("");
        ActivateRandomButton();
    }

    // ── Fin de partie ─────────────────────────────────────────────────
    /// <param name="causeVies">true = plus de vies, false = chrono épuisé</param>
    private void EndGame(bool causeVies)
    {
        isPlaying = false;
        currentButton?.TurnOff();
        currentButton = null;

        if (musicSource != null) musicSource.Stop();

        if (causeVies)
        {
            // Fin par vies épuisées
            PlaySFX(sonGameOver);
            ShowMessage($"Plus de vies !  Score final : {score}");
        }
        else
        {
            // Fin par chrono global à zéro
            PlaySFX(sonChronoFini);
            ShowMessage($"Temps écoulé !  Score final : {score}");
        }

        Debug.Log($"[GM] Game Over — Cause : {(causeVies ? "vies" : "chrono")} | Score : {score}");

        // Sauvegarde le score pour l'afficher dans la scène GameOver
        PlayerPrefs.SetInt("LastScore", score);
        PlayerPrefs.Save();

        SceneManager.LoadScene("GameOver");
    }

    // ════════════════════════════════════════════════════════════════════
    // MISE À JOUR UI
    // ════════════════════════════════════════════════════════════════════

    private void UpdateScoreUI()
    {
        if (scoreText != null) scoreText.text = $"Score : {score}";
    }

    private void UpdateHeartsUI()
    {
        if (heartImages == null || heartFull == null || heartEmpty == null) return;

        for (int i = 0; i < heartImages.Length; i++)
        {
            if (heartImages[i] == null) continue;

            bool vivant = i < vies;
            heartImages[i].sprite = vivant ? heartFull : heartEmpty;

            Color c = heartImages[i].color;
            c.a = vivant ? 1f : 0.3f;
            heartImages[i].color = c;
        }
    }

    private void ShowMessage(string msg)
    {
        if (messageText != null) messageText.text = msg;
    }

    private void PlaySFX(AudioClip clip)
    {
        if (clip != null && sfxSource != null)
            sfxSource.PlayOneShot(clip);
    }

    // ════════════════════════════════════════════════════════════════════
    // API PUBLIQUE
    // ════════════════════════════════════════════════════════════════════

    public void Rejouer() => StartGame();
}
