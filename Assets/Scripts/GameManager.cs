using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameManager : MonoBehaviour
{
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

        // Create bot with cache
        wordleBot = new WordleBot(WordList.Instance.GetPossibleAnswers(), patternCache);

        Debug.Log($"Best guess: {wordleBot.GetBestGuess()}");

        Dictionary<string, float> wordEntropies = wordleBot.GetWordEntropies();
        Debug.Log(wordEntropies.Count);
        var top5Entropies = wordEntropies.OrderByDescending(x => x.Value).Take(5);
        foreach (var item in top5Entropies)
        {
            Debug.Log($"{item.Key}: {item.Value}");
        }
    }

    void Update()
    {
        
    }

}
