using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private string answer;

    private WordleBot wordleBot;

    void Start()
    {
        answer = WordList.Instance.GetRandomAnswer();

        wordleBot = new WordleBot(WordList.Instance.GetAllValidWords());

    }

    void Update()
    {
        
    }

}
