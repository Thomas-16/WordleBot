using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class InitialGuessesCache
{
    private const string CACHE_FILENAME = "initial_guesses_cache.bytes";

    private List<KeyValuePair<string, float>> cachedGuesses;

    public InitialGuessesCache()
    {
        cachedGuesses = new List<KeyValuePair<string, float>>();
    }

    public bool LoadFromFile()
    {
        string path = Path.Combine(Application.streamingAssetsPath, CACHE_FILENAME);

        if (!File.Exists(path))
        {
            Debug.LogWarning($"Initial guesses cache not found at: {path}");
            return false;
        }

        try
        {
            using (BinaryReader reader = new BinaryReader(File.Open(path, FileMode.Open)))
            {
                int count = reader.ReadInt32();
                cachedGuesses = new List<KeyValuePair<string, float>>(count);

                for (int i = 0; i < count; i++)
                {
                    string word = reader.ReadString();
                    float entropy = reader.ReadSingle();
                    cachedGuesses.Add(new KeyValuePair<string, float>(word, entropy));
                }
            }

            Debug.Log($"Initial guesses cache loaded: {cachedGuesses.Count} entries, best guess: {cachedGuesses[0].Key}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load initial guesses cache: {e.Message}");
            return false;
        }
    }

    public void SaveToFile(List<KeyValuePair<string, float>> topGuesses)
    {
        string directory = Application.streamingAssetsPath;

        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string path = Path.Combine(directory, CACHE_FILENAME);

        try
        {
            using (BinaryWriter writer = new BinaryWriter(File.Create(path)))
            {
                writer.Write(topGuesses.Count);

                foreach (var guess in topGuesses)
                {
                    writer.Write(guess.Key);
                    writer.Write(guess.Value);
                }
            }

            Debug.Log($"Initial guesses cache saved: {topGuesses.Count} entries to {path}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save initial guesses cache: {e.Message}");
        }
    }

    public bool IsLoaded() => cachedGuesses.Count > 0;

    public string GetBestGuess() => cachedGuesses.Count > 0 ? cachedGuesses[0].Key : null;

    public Dictionary<string, float> GetAsEntropiesDictionary()
    {
        var dict = new Dictionary<string, float>();
        foreach (var kvp in cachedGuesses)
        {
            dict[kvp.Key] = kvp.Value;
        }
        return dict;
    }
}
