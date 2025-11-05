using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class WordleBot
{
    private List<string> remainingPossibleWords;
    private Dictionary<string, float> wordEntropies;
    private PatternCache patternCache;


    public WordleBot(List<string> possibleWords, PatternCache cache = null)
    {
        this.remainingPossibleWords = possibleWords;
        this.wordEntropies = new Dictionary<string, float>();
        this.patternCache = cache;
    }

    public string GetBestGuess()
    {
        string bestGuess = "";
        float maxEntropy = 0;

        foreach (string guess in remainingPossibleWords)
        {
            float entropy = CalculateExpectedEntropy(guess);
            wordEntropies.Add(guess, entropy);

            if (entropy > maxEntropy)
            {
                maxEntropy = entropy;
                bestGuess = guess;
            }
        }

        return bestGuess;
    }

    public float CalculateExpectedEntropy(string guess)
    {
        // Use cache if available, otherwise fall back to direct calculation
        Dictionary<int, List<string>> patterns;

        if (patternCache != null && patternCache.IsInitialized())
        {
            patterns = patternCache.GetPatternDistribution(guess, remainingPossibleWords);
        }
        else
        {
            patterns = PatternMatcher.GetPatternDistribution(guess, remainingPossibleWords);
        }

        float sum = 0;
        foreach (var pattern in patterns)
        {
            int patternId = pattern.Key;
            List<string> possibleAnswers = pattern.Value;

            float p = (float)possibleAnswers.Count / remainingPossibleWords.Count;

            sum += p * Mathf.Log(1 / p, 2);
        }

        return sum;
    }

    public void ProcessFeedback(string guess, GuessResult result)
    {
        // Filter remaining words based on pattern
        remainingPossibleWords = PatternMatcher.FilterByPattern(
            remainingPossibleWords, guess, result
        );

        // Clear entropy cache since possibilities changed
        wordEntropies.Clear();
    }

    public Dictionary<string, float> GetWordEntropies() { return wordEntropies; }
}
