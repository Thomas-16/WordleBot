using System.Collections.Generic;
using UnityEngine;

public class WordDataLoader : MonoBehaviour
{
    // Paths to word list files in Resources folder
    [SerializeField] private string allWordsPath = "all_words";
    [SerializeField] private string allWordsSortedPath = "all_words_sorted";
    [SerializeField] private string possibleWordsPath = "possible_wordle_words";


    /// <summary>
    /// Loads a word list from Resources folder
    /// </summary>
    /// <param name="resourcePath">Path to the text file (without .txt extension)</param>
    /// <returns>List of words in uppercase</returns>
    public List<string> LoadWordList(string resourcePath)
    {
        List<string> words = new List<string>();

        // Load the text file from Resources
        TextAsset wordFile = Resources.Load<TextAsset>(resourcePath);

        if (wordFile == null)
        {
            Debug.LogError($"Failed to load word list at: {resourcePath}");
            return words;
        }

        // Split by newlines and process each word
        string[] lines = wordFile.text.Split('\n', '\r');

        foreach (string line in lines)
        {
            string word = line.Trim().ToUpper();

            // Only add valid 5-letter words
            if (!string.IsNullOrEmpty(word) && word.Length == 5)
            {
                words.Add(word);
            }
        }

        Debug.Log($"Loaded {words.Count} words from {resourcePath}");
        return words;
    }

    /// <summary>
    /// Loads all valid words that can be guessed
    /// </summary>
    public List<string> LoadAllWords()
    {
        return LoadWordList(allWordsPath);
    }
    /// <summary>
    /// Loads all valid words that can be guessed sorted from most frequent to least
    /// </summary>
    public List<string> LoadAllWordsSorted()
    {
        return LoadWordList(allWordsSortedPath);
    }

    /// <summary>
    /// Loads only words that can be the answer
    /// </summary>
    public List<string> LoadPossibleWords()
    {
        return LoadWordList(possibleWordsPath);
    }
}