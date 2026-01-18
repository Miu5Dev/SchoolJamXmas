using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class TypewriterEffect : MonoBehaviour
{
    [SerializeField] private float typewriterSpeed = 50f;
    
    public bool IsRunning { get; private set; }
    
    private readonly Dictionary<HashSet<char>, float> punctuinations = new Dictionary<HashSet<char>, float>()
    {
        {new HashSet<char>(){'.','!','?'}, 1f},
        {new HashSet<char>(){',',';',':'}, 0.3f},
    };

    private Coroutine typingCorountine;
    
    public void Run(string textToType, TMP_Text textLabel)
    {
        typingCorountine = StartCoroutine(TypeText(textToType, textLabel));
    }

    public void Stop()
    {
        StopCoroutine(typingCorountine);
        IsRunning = false;
    }

    private IEnumerator TypeText(string textToType, TMP_Text textLabel)
    {
        IsRunning = true;
        textLabel.text = string.Empty;
        float t = 0;
        int charIndex = 0;

        while (charIndex < textToType.Length)
        {
            int lastCharIndex = charIndex;
            
            t += Time.deltaTime * typewriterSpeed;
            charIndex = Mathf.FloorToInt(t);
            charIndex = Mathf.Clamp(charIndex, 0, textToType.Length);

            for (int i = lastCharIndex; i < charIndex; i++)
            {
                bool isLast = i >= textToType.Length - 1;
                
                textLabel.text = textToType.Substring(0, i + 1);

                if (IsPunctuation(textToType[i], out float waitTime) && !isLast && !IsPunctuation(textToType[i + 1], out _))
                {
                    yield return new WaitForSeconds(waitTime);
                }
            }

            yield return null;
        }

        IsRunning = false;
    }

    private bool IsPunctuation(char character, out float waitTime)
    {
        foreach(KeyValuePair<HashSet<char>, float> punctuinationCategory in punctuinations)
        {
            if (punctuinationCategory.Key.Contains(character))
            {
                waitTime = punctuinationCategory.Value;
                return true;
            }
        }

        waitTime = default;
        return false;
    }

}
