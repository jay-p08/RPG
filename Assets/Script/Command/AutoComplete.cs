using TMPro;
using UnityEngine;

public class AutoComplete : MonoBehaviour
{
    public TMP_InputField input;

    void Update()
    {
        if (!input.isFocused) return;

        string s = Input.inputString;

        if (string.IsNullOrEmpty(s))
            return;

        char c = s[0];

        // 문자 기반 괄호 감지
        if (c == '(')
            InsertPair("(", ")");
        else if (c == '[')
            InsertPair("[", "]");
        else if (c == '{')
            InsertPair("{", "}");
    }

    void InsertPair(string open, string close)
    {
        int pos = input.caretPosition;

        // TMP가 넣은 문자를 제거하고 우리가 삽입
        input.text = input.text.Remove(pos - 1, 1);

        input.text = input.text.Insert(pos - 1, open + close);
        input.caretPosition = pos; // 커서 괄호 사이에 위치
    }
}
