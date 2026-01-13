using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

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

    // Reusable arrays for entropy calculation (avoid GC allocation)
    private float[] patternProbabilitiesBuffer = new float[243];
    private int[] patternCountsBuffer = new int[243];

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

        Debug.Log($"Precomputing {totalGuesses} x {answersCount} = {totalGuesses * answersCount:N0} patterns...");

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
    /// Calculate entropy directly from cached patterns - avoids building distribution dictionary
    /// Should be much faster than GetPatternDistribution + manual calculation
    /// Now supports probability-weighted entropy calculation
    /// </summary>
    public float CalculateEntropy(string guess, List<string> possibleAnswers, Dictionary<string, float> wordProbabilities = null)
    {
        if (!patternMatrix.ContainsKey(guess))
        {
            Debug.LogWarning($"Pattern cache miss for guess: {guess}");
            return -1f;
        }

        byte[] patterns = patternMatrix[guess];

        // Use probability sums instead of counts if probabilities are provided
        if (wordProbabilities != null)
        {
            // Clear and reuse buffer array
            Array.Clear(patternProbabilitiesBuffer, 0, 243);

            foreach (string answer in possibleAnswers)
            {
                if (!answerToIndex.TryGetValue(answer, out int answerIndex))
                    continue;

                int patternId = patterns[answerIndex];

                // Add this word's probability to the pattern bucket
                if (wordProbabilities.TryGetValue(answer, out float prob))
                {
                    patternProbabilitiesBuffer[patternId] += prob;
                }
            }

            // Calculate entropy from probability sums
            float entropy = 0f;

            for (int i = 0; i < 243; i++)
            {
                if (patternProbabilitiesBuffer[i] > 0f)
                {
                    float p = patternProbabilitiesBuffer[i];
                    entropy += p * Mathf.Log(1f / p, 2f);
                }
            }

            return entropy;
        }
        else
        {
            // Fallback: uniform probability (original implementation)
            Array.Clear(patternCountsBuffer, 0, 243);
            int totalCount = 0;

            foreach (string answer in possibleAnswers)
            {
                if (!answerToIndex.TryGetValue(answer, out int answerIndex))
                    continue;

                int patternId = patterns[answerIndex];

                patternCountsBuffer[patternId]++;
                totalCount++;
            }

            // Calculate entropy from counts
            float entropy = 0f;
            float totalCountFloat = (float)totalCount;

            for (int i = 0; i < 243; i++)
            {
                if (patternCountsBuffer[i] > 0)
                {
                    float p = patternCountsBuffer[i] / totalCountFloat;
                    entropy += p * Mathf.Log(1f / p, 2f);
                }
            }

            return entropy;
        }
    }

    public bool IsInitialized()
    {
        return patternMatrix.Count > 0;
    }

#if UNITY_WEBGL
    // GitHub Releases URL via CORS proxy (GitHub doesn't set CORS headers on release assets)
    private const string WEBGL_CACHE_URL = "https://corsproxy.io/?url=https://github.com/Thomas-16/WordleBot/releases/download/cache/pattern_cache.bytes";

    /// <summary>
    /// Async loading for WebGL using UnityWebRequest
    /// </summary>
    public IEnumerator LoadFromFileAsync(System.Action<bool> onComplete)
    {
        using (UnityWebRequest request = UnityWebRequest.Get(WEBGL_CACHE_URL))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    LoadFromBytes(request.downloadHandler.data);
                    Debug.Log($"Pattern cache loaded (WebGL): {orderedGuesses.Count} guesses x {orderedAnswers.Count} answers");
                    onComplete(true);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to parse pattern cache: {e.Message}");
                    onComplete(false);
                }
            }
            else
            {
                Debug.LogWarning($"Pattern cache failed to load from: {WEBGL_CACHE_URL} - {request.error}");
                onComplete(false);
            }
        }
    }

    /// <summary>
    /// Parse binary data from byte array (for WebGL)
    /// </summary>
    private void LoadFromBytes(byte[] data)
    {
        using (MemoryStream stream = new MemoryStream(data))
        using (BinaryReader reader = new BinaryReader(stream))
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
    }
#endif
}
