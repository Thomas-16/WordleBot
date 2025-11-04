using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class WordList : MonoBehaviour
{
    public static WordList Instance { get; private set; }

    [Header("Word Lists")]
    [SerializeField] private List<string> allValidWords = new List<string>();
    [SerializeField] private List<string> possibleAnswers = new List<string>();


    private WordDataLoader dataLoader;
    private HashSet<string> validWordSet; // For fast lookup
    private HashSet<string> answerWordSet; // For fast lookup

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
        possibleAnswers = dataLoader.LoadPossibleWords();

        // Create HashSets for O(1) lookup performance
        validWordSet = new HashSet<string>(allValidWords);
        answerWordSet = new HashSet<string>(possibleAnswers);

        int totalValidWords = allValidWords.Count;
        int totalPossibleAnswers = possibleAnswers.Count;

        if (totalValidWords == 0)
        {
            Debug.LogError("No valid words loaded!");
        }

        if (totalPossibleAnswers == 0)
        {
            Debug.LogError("No possible answers loaded!");
        }

        Debug.Log($"WordList initialized: {totalValidWords} valid words, {totalPossibleAnswers} possible answers");
    }

    /// <summary>
    /// Checks if a word is a valid guess
    /// </summary>
    public bool IsValidWord(string word)
    {
        if (string.IsNullOrEmpty(word)) return false;
        return validWordSet.Contains(word.ToUpper());
    }

    /// <summary>
    /// Checks if a word could be the answer
    /// </summary>
    public bool IsPossibleAnswer(string word)
    {
        if (string.IsNullOrEmpty(word)) return false;
        return answerWordSet.Contains(word.ToUpper());
    }

    /// <summary>
    /// Gets all valid words (for bot calculations)
    /// </summary>
    public List<string> GetAllValidWords()
    {
        return new List<string>(allValidWords);
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