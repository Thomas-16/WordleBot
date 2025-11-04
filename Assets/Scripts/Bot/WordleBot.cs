using System.Collections.Generic;
using UnityEngine;

public class WordleBot
{
    private List<string> possibleWords;

    public WordleBot(List<string> possibleWords)
    {
        this.possibleWords = possibleWords;
    }

    public string GetBestGuess()
    {
        return "";
    }
}
