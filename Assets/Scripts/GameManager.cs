using UnityEngine;
using System.Collections;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class GameManager : MonoBehaviour
{
    [Header("References")]

    [SerializeField] private TargetButton[] buttons;


    [SerializeField] private float maxTime = 3f;

    [SerializeField] private float timeReductionPerPoint = 0.05f;


    [SerializeField] private float minTime = 0.8f;

    [SerializeField] private float delayBetweenButtons = 0.5f;

    [Header("Game State")]
    [SerializeField] private int score = 0;

    private TargetButton currentButton;
    private float remainingTime;
    private bool isPlaying = false;
    private bool isWaiting = false;

    private void Start()
    {
         //Détecte quand le joueur appuie dessus
        foreach (var button in buttons)
        {
            var interactable = button.GetComponent<XRSimpleInteractable>();
            if (interactable != null)
            {
                TargetButton b = button;
                interactable.selectEntered.AddListener((SelectEnterEventArgs args) =>
                {
                    OnButtonPressed(b);
                });
            }
        }

        StartGame();
    }

    // Démarre ou redémarre une partie.
    public void StartGame()
    {
        score = 0;
        isPlaying = true;
        ActivateRandomButton();
        Debug.Log("Partie lancée !");
    }

    private void Update()
    {
        if (!isPlaying || isWaiting) return;

        remainingTime -= Time.deltaTime;

        if (remainingTime <= 0f)
        {
            GameOver();
        }
    }

    // Éteint le bouton actuel et en allume un nouveau au hasard.
    private void ActivateRandomButton()
    {
        if (currentButton != null)
        {
            currentButton.TurnOff();
        }

        // Choisir un nouveau bouton différent du précédent
        TargetButton next;
        do
        {
            int index = Random.Range(0, buttons.Length);
            next = buttons[index];
        } while (next == currentButton && buttons.Length > 1);

        currentButton = next;
        currentButton.TurnOn();

        // Reset du timer (de plus en plus court avec le score)
        remainingTime = Mathf.Max(minTime, maxTime - (score * timeReductionPerPoint));

        Debug.Log($"Button activated: {currentButton.name} | Time: {remainingTime:F2}s | Score: {score}");
    }

    // Appelé quand le joueur appuie sur un bouton.
    private void OnButtonPressed(TargetButton button)
    {
        if (!isPlaying || isWaiting) return;

        if (button == currentButton && button.IsActive)
        {
            score++;
            Debug.Log($"Correct! Score: {score}");
            StartCoroutine(ActivateNextWithDelay());
        }
        else
        {
            Debug.Log("Wrong button!");
            GameOver();
        }
    }

    // Éteint le bouton actuel, attend un délai, puis active le suivant.
    private IEnumerator ActivateNextWithDelay()
    {
        isWaiting = true;
        currentButton.TurnOff();

        yield return new WaitForSeconds(delayBetweenButtons);

        isWaiting = false;
        ActivateRandomButton();
    }

    private void GameOver()
    {
        isPlaying = false;

        if (currentButton != null)
        {
            currentButton.TurnOff();
            currentButton = null;
        }

        Debug.Log($"Game over! Final score: {score}");
    }
}
