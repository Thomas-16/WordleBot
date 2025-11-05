using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Precomputes and caches all pattern IDs for instant lookup
/// Saves to binary file for fast loading (~30MB, loads in <1 second)
/// </summary>
public class PatternCache
{
    private const string CACHE_FILENAME = "pattern_cache.bytes";

    // Compact storage: guess -> array of pattern bytes (indexed by answer)
    private Dictionary<string, byte[]> patternMatrix;

    // Maps answer words to their index in the byte arrays
    private Dictionary<string, int> answerToIndex;

    // Ordered lists (must match file order)
    private List<string> orderedGuesses;
    private List<string> orderedAnswers;

    public PatternCache()
    {
        patternMatrix = new Dictionary<string, byte[]>();
        answerToIndex = new Dictionary<string, int>();
        orderedGuesses = new List<string>();
        orderedAnswers = new List<string>();
    }

    /// <summary>
    /// Loads precomputed patterns from binary file
    /// Returns true if successful, false if file doesn't exist or is corrupted
    /// </summary>
    public bool LoadFromFile()
    {
        string path = Path.Combine(Application.streamingAssetsPath, CACHE_FILENAME);

        if (!File.Exists(path))
        {
            Debug.LogWarning($"Pattern cache file not found at: {path}");
            return false;
        }

        try
        {
            var startTime = DateTime.Now;

            using (BinaryReader reader = new BinaryReader(File.Open(path, FileMode.Open)))
            {
                // Read header
                int guessCount = reader.ReadInt32();
                int answerCount = reader.ReadInt32();

                // Read guess list
                orderedGuesses = new List<string>(guessCount);
                for (int i = 0; i < guessCount; i++)
                {
                    orderedGuesses.Add(reader.ReadString());
                }

                // Read answer list and build index
                orderedAnswers = new List<string>(answerCount);
                answerToIndex = new Dictionary<string, int>(answerCount);
                for (int i = 0; i < answerCount; i++)
                {
                    string answer = reader.ReadString();
                    orderedAnswers.Add(answer);
                    answerToIndex[answer] = i;
                }

                // Read pattern matrix
                patternMatrix = new Dictionary<string, byte[]>(guessCount);
                for (int i = 0; i < guessCount; i++)
                {
                    byte[] patterns = reader.ReadBytes(answerCount);
                    patternMatrix[orderedGuesses[i]] = patterns;
                }
            }

            var elapsed = DateTime.Now - startTime;
            Debug.Log($"Pattern cache loaded in {elapsed.TotalSeconds:F2} seconds");
            Debug.Log($"Loaded {orderedGuesses.Count} guesses × {orderedAnswers.Count} answers");

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load pattern cache: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// Precomputes all patterns and saves to binary file
    /// Call this once to generate the cache file
    /// </summary>
    public void PrecomputeAndSave(List<string> allValidGuesses, List<string> possibleAnswers)
    {
        orderedGuesses = new List<string>(allValidGuesses);
        orderedAnswers = new List<string>(possibleAnswers);

        // Build answer index mapping
        answerToIndex = new Dictionary<string, int>(orderedAnswers.Count);
        for (int i = 0; i < orderedAnswers.Count; i++)
        {
            answerToIndex[orderedAnswers[i]] = i;
        }

        int totalGuesses = orderedGuesses.Count;
        int answersCount = orderedAnswers.Count;

        Debug.Log($"Precomputing {totalGuesses} × {answersCount} = {totalGuesses * answersCount:N0} patterns...");

        var startTime = DateTime.Now;

        // Parallelize across guesses
        ConcurrentDictionary<string, byte[]> concurrentMatrix = new ConcurrentDictionary<string, byte[]>();

        Parallel.ForEach(orderedGuesses, guess =>
        {
            byte[] patterns = new byte[answersCount];

            for (int i = 0; i < answersCount; i++)
            {
                int patternId = PatternMatcher.GetPattern(guess, orderedAnswers[i]);
                patterns[i] = (byte)patternId;
            }

            concurrentMatrix[guess] = patterns;
        });

        patternMatrix = new Dictionary<string, byte[]>(concurrentMatrix);

        var elapsed = DateTime.Now - startTime;
        Debug.Log($"Pattern cache computed in {elapsed.TotalSeconds:F2} seconds");

        // Save to file
        SaveToFile();
    }

    /// <summary>
    /// Saves the pattern matrix to a binary file
    /// </summary>
    private void SaveToFile()
    {
        string directory = Application.streamingAssetsPath;

        // Create StreamingAssets directory if it doesn't exist
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string path = Path.Combine(directory, CACHE_FILENAME);

        try
        {
            var startTime = DateTime.Now;

            using (BinaryWriter writer = new BinaryWriter(File.Create(path)))
            {
                // Write header
                writer.Write(orderedGuesses.Count);
                writer.Write(orderedAnswers.Count);

                // Write guess list
                foreach (string guess in orderedGuesses)
                {
                    writer.Write(guess);
                }

                // Write answer list
                foreach (string answer in orderedAnswers)
                {
                    writer.Write(answer);
                }

                // Write pattern matrix
                foreach (string guess in orderedGuesses)
                {
                    writer.Write(patternMatrix[guess]);
                }
            }

            var elapsed = DateTime.Now - startTime;
            FileInfo fileInfo = new FileInfo(path);
            Debug.Log($"Pattern cache saved in {elapsed.TotalSeconds:F2} seconds");
            Debug.Log($"File size: {fileInfo.Length / 1024 / 1024}MB at {path}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save pattern cache: {e.Message}");
        }
    }

    /// <summary>
    /// Fast O(1) pattern lookup from cache
    /// </summary>
    public int GetPattern(string guess, string answer)
    {
        guess = guess.ToUpper();
        answer = answer.ToUpper();

        if (!patternMatrix.ContainsKey(guess))
        {
            Debug.LogWarning($"Pattern cache miss for guess: {guess}");
            return PatternMatcher.GetPattern(guess, answer);
        }

        if (!answerToIndex.ContainsKey(answer))
        {
            Debug.LogWarning($"Pattern cache miss for answer: {answer}");
            return PatternMatcher.GetPattern(guess, answer);
        }

        int answerIndex = answerToIndex[answer];
        return patternMatrix[guess][answerIndex];
    }

    /// <summary>
    /// Fast pattern distribution using cached patterns
    /// </summary>
    public Dictionary<int, List<string>> GetPatternDistribution(string guess, List<string> possibleAnswers)
    {
        guess = guess.ToUpper();
        Dictionary<int, List<string>> distribution = new Dictionary<int, List<string>>();

        if (!patternMatrix.ContainsKey(guess))
        {
            Debug.LogWarning($"Pattern cache miss for guess: {guess}");
            return PatternMatcher.GetPatternDistribution(guess, possibleAnswers);
        }

        byte[] patterns = patternMatrix[guess];

        foreach (string answer in possibleAnswers)
        {
            string upperAnswer = answer.ToUpper();

            if (!answerToIndex.ContainsKey(upperAnswer))
            {
                Debug.LogWarning($"Answer not in cache: {answer}");
                continue;
            }

            int answerIndex = answerToIndex[upperAnswer];
            int patternId = patterns[answerIndex];

            if (!distribution.ContainsKey(patternId))
                distribution[patternId] = new List<string>();

            distribution[patternId].Add(answer);
        }

        return distribution;
    }

    public bool IsInitialized()
    {
        return patternMatrix.Count > 0;
    }
}
