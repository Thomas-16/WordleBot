using System.Collections.Generic;
using UnityEngine;

public class WordleBot
{
    private List<string> remainingPossibleWords;
    private Dictionary<string, float> wordEntropies;

    private WordList wordList;
    

    public WordleBot(List<string> possibleWords)
    {
        this.remainingPossibleWords = possibleWords;
    }

    public string GetBestGuess()
    {
        string bestGuess = "";
        float maxEntropy = 0;

        // For each valid guess, calculate expected information gain
        foreach (string guess in remainingPossibleWords)
        {
            float entropy = CalculateExpectedEntropy(guess);
            if (entropy > maxEntropy)
            {
                maxEntropy = entropy;
                bestGuess = guess;
            }
        }

        return bestGuess;
    }

    private float CalculateExpectedEntropy(string guess)
    {
        Dictionary<int, List<string>> patternDistribution = PatternMatcher.GetPatternDistribution(guess, remainingPossibleWords);

        return 0;
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
}
