using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class PatternMatcher
{
    /// <summary>
    /// Evaluates a guess against the answer and returns the pattern
    /// </summary>
    public static GuessResult EvaluateGuess(string guess, string answer)
    {
        GuessResult result = new GuessResult(guess);

        // Track which answer letters have been "used"
        bool[] answerUsed = new bool[5];

        // First pass: Mark all correct positions (green)
        for (int i = 0; i < 5; i++)
        {
            if (guess[i] == answer[i])
            {
                result.results[i] = LetterResult.Correct;
                answerUsed[i] = true;
            }
        }

        // Second pass: Mark present letters (yellow)
        for (int i = 0; i < 5; i++)
        {
            if (result.results[i] == LetterResult.Correct)
                continue;

            // Look for this letter elsewhere in answer
            for (int j = 0; j < 5; j++)
            {
                if (!answerUsed[j] && guess[i] == answer[j])
                {
                    result.results[i] = LetterResult.Present;
                    answerUsed[j] = true;
                    break;
                }
            }

            // If still not marked, it's absent (gray)
            if (result.results[i] != LetterResult.Present)
            {
                result.results[i] = LetterResult.Absent;
            }
        }

        result.ToPatternId(); // Calculate and store pattern ID
        return result;
    }

    /// <summary>
    /// Gets pattern ID directly without creating GuessResult object
    /// More efficient for bot calculations
    /// </summary>
    public static int GetPattern(string guess, string answer)
    {
        GuessResult result = EvaluateGuess(guess, answer);
        return result.patternId;
    }

    /// <summary>
    /// Filters a list of words to only those that would give the same pattern
    /// Used by bot to narrow down possibilities
    /// </summary>
    public static List<string> FilterByPattern(List<string> possibleWords, string guess, GuessResult pattern)
    {
        return possibleWords.Where(word =>
            GetPattern(guess, word) == pattern.patternId
        ).ToList();
    }

    /// <summary>
    /// Checks if a word is consistent with a guess pattern
    /// </summary>
    public static bool IsConsistent(string possibleAnswer, string guess, GuessResult pattern)
    {
        return GetPattern(guess, possibleAnswer) == pattern.patternId;
    }

    /// <summary>
    /// Gets all possible patterns for a guess against a list of words
    /// Returns pattern distribution for entropy calculation
    /// </summary>
    public static Dictionary<int, List<string>> GetPatternDistribution(string guess, List<string> possibleAnswers)
    {
        Dictionary<int, List<string>> distribution = new Dictionary<int, List<string>>();

        foreach (string answer in possibleAnswers)
        {
            int pattern = GetPattern(guess, answer);

            if (!distribution.ContainsKey(pattern))
                distribution[pattern] = new List<string>();

            distribution[pattern].Add(answer);
        }

        return distribution;
    }

    /// <summary>
    /// Checks if the pattern is a win (all green)
    /// </summary>
    public static bool IsWinningPattern(GuessResult result)
    {
        return result.patternId == 242; // 2 + 2*3 + 2*9 + 2*27 + 2*81 = 242
    }

    /// <summary>
    /// Debug helper: converts pattern ID to readable string
    /// </summary>
    public static string PatternToString(int patternId)
    {
        GuessResult result = GuessResult.FromPatternId("", patternId);
        return result.ToString();
    }
}