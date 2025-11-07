using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DistributionGraphUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI statsText;

    [SerializeField] private RectTransform[] bars;
    [SerializeField] private RectTransform[] barNumbers;

    void Start()
    {
        SimulationManager.Instance.OnSimulationComplete += SimulationCompleteHandler;

        ResetUI();
    }

    void Update()
    {
        if (SimulationManager.Instance.IsRunning())
        {
            UpdateStatsText();
            UpdateBarGraph();
            UpdateBarNumbers();
        }
    }

    private void UpdateBarNumbers()
    {
        for (int i = 0; i < 6; i++)
        {
            RectTransform numberTransform = barNumbers[i];
            TextMeshProUGUI numberText = numberTransform.GetComponent<TextMeshProUGUI>();
            int number = SimulationManager.Instance.GetSolveDistribution()[i + 1];

            // Update number and visibility
            numberText.text = number.ToString();
            numberTransform.gameObject.SetActive(number != 0);

            // Update position
            numberTransform.anchoredPosition = new Vector2(numberTransform.anchoredPosition.x, -375 + bars[i].sizeDelta.y + 25);
        }
    }

    private void UpdateBarGraph()
    {
        // Index 0 = failed, 1-6 = solved in N guesses
        int[] solveDistribution = SimulationManager.Instance.GetSolveDistribution();

        for (int i = 1; i < 7; i++)
        {
            bars[i - 1].sizeDelta = new Vector2(bars[i - 1].sizeDelta.x, solveDistribution[i] / (float)SimulationManager.Instance.GetGamesCompleted() * 750f);
        }
    }
    
    private void ResetUI ()
    {
        statsText.text =
                $"Average Score: 0.00 \n" +
                $"Rounds: 0/0 \n" +
                $"Progress: 0.0% \n" +
                $"Winrate: 0.0%";

        for (int i = 1; i < 7; i++)
        {
            bars[i - 1].sizeDelta = new Vector2(bars[i - 1].sizeDelta.x, 0);
        }
        for (int i = 0; i < bars.Length; i++)
        {
            barNumbers[i].gameObject.SetActive(false);
        }
    }

    private void SimulationCompleteHandler()
    {
        UpdateStatsText();
        UpdateBarGraph();
        UpdateBarNumbers();
    }

    private void UpdateStatsText()
    {
        statsText.text =
                $"Average Score: {Math.Round(SimulationManager.Instance.GetAverageGuesses(), 2)} \n" +
                $"Rounds: {SimulationManager.Instance.GetGamesCompleted()}/{SimulationManager.Instance.GetTotalGames()} \n" +
                $"Progress: {Math.Round(SimulationManager.Instance.GetProgress() * 100f, 1)}% \n" +
                $"Winrate: {Math.Round(SimulationManager.Instance.GetWins() / (float) SimulationManager.Instance.GetGamesCompleted() * 100f, 1)}%";
    }
}
