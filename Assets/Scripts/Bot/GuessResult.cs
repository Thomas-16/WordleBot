using System;
using UnityEngine;

[System.Serializable]
public class GuessResult
{
    public LetterResult[] results;
    public string guess;
    public int patternId; // 0-242 for bot calculations

    public GuessResult(string guess)
    {
        this.guess = guess;
        results = new LetterResult[5];
    }

    public GuessResult(string guess, LetterResult result1, LetterResult result2, LetterResult result3, LetterResult result4, LetterResult result5)
    {
        this.guess = guess;
        this.results = new LetterResult[5] { result1, result2, result3, result4, result5 };
        this.patternId = ToPatternId();
    }

    /// <summary>
    /// Converts the result to a base-3 pattern integer (0-242)
    /// Used for bot entropy calculations
    /// </summary>
    public int ToPatternId()
    {
        int pattern = 0;
        int multiplier = 1;

        for (int i = 0; i < 5; i++)
        {
            pattern += (int)results[i] * multiplier;
            multiplier *= 3;
        }

        patternId = pattern;
        return pattern;
    }

    /// <summary>
    /// Creates a GuessResult from a pattern ID
    /// </summary>
    public static GuessResult FromPatternId(string guess, int patternId)
    {
        GuessResult result = new GuessResult(guess);
        int remaining = patternId;

        for (int i = 0; i < 5; i++)
        {
            result.results[i] = (LetterResult)(remaining % 3);
            remaining /= 3;
        }

        result.patternId = patternId;
        return result;
    }

    /// <summary>
    /// Debug string representation (e.g., "⬛🟨🟩🟩⬛")
    /// </summary>
    public override string ToString()
    {
        string output = "";
        foreach (var result in results)
        {
            switch (result)
            {
                case LetterResult.Absent: output += "⬛"; break;
                case LetterResult.Present: output += "🟨"; break;
                case LetterResult.Correct: output += "🟩"; break;
            }
        }
        return output;
    }
}
