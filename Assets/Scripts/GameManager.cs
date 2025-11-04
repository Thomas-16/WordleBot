using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private string answer;

    void Start()
    {
        answer = WordList.Instance.GetRandomAnswer();

    }

    void Update()
    {
        
    }

}
