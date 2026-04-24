using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine;

public class GameOverManager : MonoBehaviour
{

    [SerializeField] private TextMeshProUGUI scoreText;

    [SerializeField] private TextMeshProUGUI highscoreText;

    private const string HIGHSCORE_KEY = "Highscore";
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        int finalScore = PlayerPrefs.GetInt("LastScore", 0);
        int highscore = PlayerPrefs.GetInt(HIGHSCORE_KEY, 0);

        // Met à jour le highscore si le score actuel est meilleur
        if (finalScore > highscore)
        {
            highscore = finalScore;
            PlayerPrefs.SetInt(HIGHSCORE_KEY, highscore);
            PlayerPrefs.Save();
        }

        // Affiche les scores
        if (scoreText != null)
            scoreText.text = $"Score: {finalScore}";

        if (highscoreText != null)
            highscoreText.text = $"Best: {highscore}";
    }

    public void Retry()
    {
        SceneManager.LoadScene("Game");
    }

    public void Quit()
    {
        Debug.Log("Quit!");
        Application.Quit();

        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}
