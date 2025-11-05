using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private TopGuessesUI textGuessesUI;

    [SerializeField] private string answer;

    private WordleBot wordleBot;
    private PatternCache patternCache;

    void Start()
    {
        answer = WordList.Instance.GetRandomAnswer();

        // Initialize pattern cache (loads from file if available)
        patternCache = new PatternCache();
        bool cacheLoaded = patternCache.LoadFromFile();

        if (!cacheLoaded)
        {
            Debug.LogWarning("Pattern cache not found. Bot will run slower without precomputed patterns.");
            Debug.LogWarning("Run the CachePrecomputer script to generate the cache file.");
        }

        // Create bot with cache - use all 13k valid words
        wordleBot = new WordleBot(WordList.Instance.GetAllValidWords(), patternCache);

        System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
        Debug.Log($"Best guess: {wordleBot.GetBestGuess()}, time took: {sw.ElapsedMilliseconds} ms");
        sw.Stop();

        Dictionary<string, float> wordEntropies = wordleBot.GetWordEntropies();
        Debug.Log($"Possibilities: {wordEntropies.Count}");
        List<KeyValuePair<string, float>> topGuesses = wordEntropies.OrderByDescending(x => x.Value).Take(14).ToList();
        textGuessesUI.DisplayGuesses(topGuesses);

    }

    void Update()
    {
        
    }

}
