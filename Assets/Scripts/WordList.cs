using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class WordList : MonoBehaviour
{
    public static WordList Instance { get; private set; }

    [Header("Word Lists")]
    [SerializeField] private List<string> allValidWords = new List<string>();
    [SerializeField] private List<string> allValidWordsSorted = new List<string>();
    [SerializeField] private List<string> possibleAnswers = new List<string>();


    private WordDataLoader dataLoader;
    private HashSet<string> validWordSet; // For fast lookup

    private void Awake()
    {
        Instance = this;

        dataLoader = GetComponent<WordDataLoader>();

        LoadWords();
    }

    /// <summary>
    /// Loads both word lists and prepares them for use
    /// </summary>
    private void LoadWords()
    {
        // Load the word lists
        allValidWords = dataLoader.LoadAllWords();
        allValidWordsSorted = dataLoader.LoadAllWordsSorted();
        possibleAnswers = dataLoader.LoadPossibleWords();

        // Create HashSets for O(1) lookup performance
        validWordSet = new HashSet<string>(allValidWords);

        int totalValidWords = allValidWords.Count;
        int totalValidWordsSorted = allValidWordsSorted.Count;
        int totalPossibleAnswers = possibleAnswers.Count;

        if (totalValidWords == 0)
        {
            Debug.LogError("No valid words loaded!");
        }
        if (totalValidWordsSorted == 0)
        {
            Debug.LogError("No valid words sorted loaded!");
        }
        if (totalPossibleAnswers == 0)
        {
            Debug.LogError("No possible answers loaded!");
        }

        Debug.Log($"WordList initialized: {totalValidWords} valid words, {totalValidWordsSorted} valid sorted words, {totalPossibleAnswers} possible answers");
    }

    /// <summary>
    /// Checks if a word is a valid guess
    /// </summary>
    public bool IsValidWord(string word)
    {
        if (string.IsNullOrEmpty(word)) return false;
        return validWordSet.Contains(word);
    }

    /// <summary>
    /// Gets all valid words (for bot calculations)
    /// </summary>
    public List<string> GetAllValidWords()
    {
        return new List<string>(allValidWords);
    }

    /// <summary>
    /// Gets all valid words sorted (for bot calculations)
    /// </summary>
    public List<string> GetAllValidWordsSorted()
    {
        return new List<string>(allValidWordsSorted);
    }

    /// <summary>
    /// Gets all possible answers (for bot calculations)
    /// </summary>
    public List<string> GetPossibleAnswers()
    {
        return new List<string>(possibleAnswers);
    }

    /// <summary>
    /// Gets a random word from possible answers
    /// </summary>
    public string GetRandomAnswer()
    {
        if (possibleAnswers.Count == 0)
        {
            Debug.LogError("No possible answers available!");
            return "ERROR";
        }

        int randomIndex = Random.Range(0, possibleAnswers.Count);
        return possibleAnswers[randomIndex];
    }

}