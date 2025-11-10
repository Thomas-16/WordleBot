using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages word probability distribution based on frequency data.
/// Implements 3Blue1Brown's sigmoid-based probability assignment approach.
/// </summary>
public class WordFrequencyModel
{
    private Dictionary<string, float> wordProbabilities;

    private const float SIGMOID_MIDPOINT = 0.5f;  // Point where probability = 0.5
    private const float SIGMOID_STEEPNESS = 10f;  // Controls transition sharpness

    /// <summary>
    /// Creates a frequency model from a frequency-sorted word list.
    /// Words should be sorted from most to least frequent.
    /// </summary>
    public WordFrequencyModel(List<string> frequencySortedWords)
    {
        wordProbabilities = new Dictionary<string, float>();
        AssignProbabilities(frequencySortedWords);
    }

    /// <summary>
    /// Applies sigmoid function to assign probabilities based on word rank.
    /// Uses the formula: P(x) = 1 / (1 + e^(-steepness * (x - midpoint)))
    /// where x is normalized position in sorted list (0 to 1).
    /// </summary>
    private void AssignProbabilities(List<string> sortedWords)
    {
        int totalWords = sortedWords.Count;
        float rawProbabilitySum = 0f;

        // First pass: Calculate raw sigmoid probabilities
        Dictionary<string, float> rawProbabilities = new Dictionary<string, float>();

        for (int i = 0; i < totalWords; i++)
        {
            string word = sortedWords[i];

            // Normalize position to [0, 1] range (0 = most frequent)
            float normalizedPosition = (float)i / (totalWords - 1);

            // Apply sigmoid function
            // High frequency words (position near 0) get high probability
            // Low frequency words (position near 1) get low probability
            float rawProb = SigmoidFunction(normalizedPosition);

            rawProbabilities[word] = rawProb;
            rawProbabilitySum += rawProb;
        }

        // Second pass: Normalize so probabilities sum to 1.0
        foreach (var kvp in rawProbabilities)
        {
            wordProbabilities[kvp.Key] = kvp.Value / rawProbabilitySum;
        }

        Debug.Log($"WordFrequencyModel initialized with {totalWords} words. " +
                  $"Normalized probability sum: {GetTotalProbability():F6}");
    }

    /// <summary>
    /// Sigmoid function: 1 / (1 + e^(-steepness * (x - midpoint)))
    /// Inverted because position 0 = most frequent (should have high probability)
    /// </summary>
    private float SigmoidFunction(float normalizedPosition)
    {
        // Invert the position so high frequency (low position) = high probability
        float invertedPosition = 1f - normalizedPosition;

        // Shift and scale around midpoint
        float x = invertedPosition - SIGMOID_MIDPOINT;

        // Apply sigmoid
        float result = 1f / (1f + Mathf.Exp(-SIGMOID_STEEPNESS * x));

        return result;
    }

    /// <summary>
    /// Gets the probability of a specific word.
    /// Returns 0 if word is not in the model.
    /// </summary>
    public float GetProbability(string word)
    {
        return wordProbabilities.TryGetValue(word, out float prob) ? prob : 0f;
    }

    /// <summary>
    /// Gets probabilities for all words.
    /// </summary>
    public Dictionary<string, float> GetAllProbabilities()
    {
        return new Dictionary<string, float>(wordProbabilities);
    }

    /// <summary>
    /// Gets probabilities only for specified words (useful for filtered lists).
    /// </summary>
    public Dictionary<string, float> GetProbabilities(List<string> words)
    {
        Dictionary<string, float> result = new Dictionary<string, float>();

        foreach (string word in words)
        {
            if (wordProbabilities.TryGetValue(word, out float prob))
            {
                result[word] = prob;
            }
        }

        return result;
    }

    /// <summary>
    /// Renormalizes probabilities for a subset of words.
    /// Used after filtering to ensure probabilities sum to 1.0 over remaining words.
    /// </summary>
    public Dictionary<string, float> GetNormalizedProbabilities(List<string> words)
    {
        Dictionary<string, float> result = new Dictionary<string, float>();
        float sum = 0f;

        // Get probabilities for subset
        foreach (string word in words)
        {
            if (wordProbabilities.TryGetValue(word, out float prob))
            {
                result[word] = prob;
                sum += prob;
            }
        }

        // Renormalize
        if (sum > 0f)
        {
            List<string> keys = new List<string>(result.Keys);
            foreach (string key in keys)
            {
                result[key] /= sum;
            }
        }

        return result;
    }

    /// <summary>
    /// Gets total probability sum (should be 1.0 for full word list).
    /// Useful for debugging.
    /// </summary>
    private float GetTotalProbability()
    {
        float sum = 0f;
        foreach (float prob in wordProbabilities.Values)
        {
            sum += prob;
        }
        return sum;
    }

    /// <summary>
    /// Gets diagnostic information about the probability distribution.
    /// </summary>
    public string GetDiagnostics()
    {
        float totalProb = GetTotalProbability();
        int wordCount = wordProbabilities.Count;

        // Find min/max probabilities
        float minProb = float.MaxValue;
        float maxProb = float.MinValue;

        foreach (float prob in wordProbabilities.Values)
        {
            if (prob < minProb) minProb = prob;
            if (prob > maxProb) maxProb = prob;
        }

        return $"WordFrequencyModel Diagnostics:\n" +
               $"  Total words: {wordCount}\n" +
               $"  Probability sum: {totalProb:F6}\n" +
               $"  Min probability: {minProb:E4}\n" +
               $"  Max probability: {maxProb:E4}\n" +
               $"  Sigmoid midpoint: {SIGMOID_MIDPOINT}\n" +
               $"  Sigmoid steepness: {SIGMOID_STEEPNESS}";
    }
}
