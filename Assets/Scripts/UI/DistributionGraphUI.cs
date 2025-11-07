using System;
using TMPro;
using UnityEngine;

public class DistributionGraphUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI statsText;

    void Start()
    {
        statsText.text =
                $"Average Score: 0.00 \n" +
                $"Rounds: 0/0 \n" +
                $"Progress: 0.0% \n" +
                $"Winrate: 0.0%";

        SimulationManager.Instance.OnSimulationComplete += SimulationCompleteHandler;
    }

    void Update()
    {
        if (SimulationManager.Instance.IsRunning())
        {
            UpdateStatsText();
        }
    }

    private void SimulationCompleteHandler()
    {
        UpdateStatsText();
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
