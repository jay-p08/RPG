using TMPro;
using UnityEngine;
using System.Collections;

public class AutoIndent : MonoBehaviour
{
    public TMP_InputField input;

    void Update()
    {
        if (!input.isFocused) return;

        if (Input.GetKeyDown(KeyCode.Return))
        {
            StartCoroutine(IndentNextFrame());
        }
    }

    IEnumerator IndentNextFrame()
    {
        // TMP가 줄바꿈을 먼저 처리하도록 한 프레임 기다림
        yield return null;

        string text = input.text;
        int pos = input.caretPosition;

        // 현재 줄의 시작 찾기
        int lineStart = text.LastIndexOf('\n', Mathf.Clamp(pos - 1, 0, text.Length - 1));
        int prevLineStart = text.LastIndexOf('\n', lineStart - 1);

        if (prevLineStart < 0) prevLineStart = -1;

        string prevLine = text.Substring(prevLineStart + 1, lineStart - prevLineStart - 1);

        // 들여쓰기 추출
        string indent = "";
        foreach (char c in prevLine)
        {
            if (c == ' ' || c == '\t')
                indent += c;
            else
                break;
        }

        // 들여쓰기 삽입
        input.text = text.Insert(pos, indent);
        input.caretPosition = pos + indent.Length;
    }
}
