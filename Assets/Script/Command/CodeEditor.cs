using System;
using TMPro;
using UnityEngine;
using System.Collections;
using Unity.VisualScripting;

public class CodeEditor : MonoBehaviour
{
    public TMP_InputField input;

    void Start()
    {
        input.onValidateInput += ValidateChar;
    }

    void Update()
    {
        if (!input.isFocused) return;

        bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        // -------------------------
        // 자동 괄호 처리 구분
        // -------------------------

        // [ 또는 {  
        if (Input.GetKeyDown(KeyCode.LeftBracket))
        {
            if (shift)
                HandleCurlyBracket();   // Shift + [  → { }
            else
                InsertPair("[", "]");   // [ → []  
        }

        // (  
        if (Input.GetKeyDown(KeyCode.Alpha9) && shift)
        {
            InsertPair("(", ")");       // Shift + 9 → (
        }

        // ) (닫는 것은 자동생성 X / 필요시 추가)
        if (Input.GetKeyDown(KeyCode.Alpha0) && shift)
        {
            // 그냥 ) 입력되도록 (자동 닫기 필요하면 알려줘)
        }

        // 엔터 → 자동 들여쓰기
        if (Input.GetKeyDown(KeyCode.Return))
        {
            StartCoroutine(AutoIndentNextFrame());
        }
    }

    char ValidateChar(string text, int charIndex, char addedChar)
    {
        // 자동완성으로 처리할 문자들 → 입력을 막고(\0), 자동완성만 실행되게 함
        if (addedChar == '{' || addedChar == '[' || addedChar == '(')
            return '\0'; // 입력 취소
    
        return addedChar; // 그 외는 원래 입력 유지
    }

    // ----------------------------------------------------------------
    // 1) 자동 괄호 기본 처리
    // ----------------------------------------------------------------
    void InsertPair(string open, string close)
    {
        int pos = input.caretPosition;

        // 선택영역 있을 때 감싸기
        if (input.selectionStringFocusPosition != input.selectionStringAnchorPosition)
        {
            int start = Mathf.Min(input.selectionStringAnchorPosition, input.selectionStringFocusPosition);
            int end = Mathf.Max(input.selectionStringAnchorPosition, input.selectionStringFocusPosition);

            string inner = input.text.Substring(start, end - start);
            input.text = input.text.Remove(start, end - start);
            input.text = input.text.Insert(start, open + inner + close);

            input.caretPosition = start + open.Length;
            return;
        }

        // 선택 없음
        input.text = input.text.Insert(pos, open + close);
        input.caretPosition = pos + 1;
    }

    // ----------------------------------------------------------------
    // 2) { 처리 (자동으로 {} + 줄 닫기까지)
    // ----------------------------------------------------------------
    void HandleCurlyBracket()
    {
        int pos = input.caretPosition;
        InsertPair("{", "}");
    }

    // ----------------------------------------------------------------
    // 3) Enter 후 자동 들여쓰기 + } 자동 내림
    // ----------------------------------------------------------------
    IEnumerator AutoIndentNextFrame()
    {
        yield return null; // TMP가 줄바꿈 먼저 처리하도록
    
        string text = input.text;
        int pos = input.caretPosition;
    
        try
        {
            // 현재 줄 찾기
            int lineStart = text.LastIndexOf('\n', Mathf.Clamp(pos - 1, 0, text.Length - 1));
        
            // 이전 줄 찾기
            int prevLineStart = text.LastIndexOf('\n', lineStart - 1);
            if (prevLineStart < 0) prevLineStart = -1;
            
            string prevLine = text.Substring(prevLineStart + 1, lineStart - (prevLineStart + 1));
            // 들여쓰기 분석
            string indent = "";
            foreach (char c in prevLine)
            {
                if (c == ' ' || c == '\t')
                    indent += c;
                else
                    break;
            }
        
            bool prevEndsWithBrace = prevLine.TrimEnd().EndsWith("{");
            if (prevEndsWithBrace)
            {
                // 새 줄 들여쓰기
                string innerIndent = indent + "    ";
        
                // 커서 위치에 삽입: (들여쓰기 + 줄바꿈 + 닫는 중괄호)
                string insertText = innerIndent + "\n" + indent;
        
                input.text = text.Insert(pos, insertText);
        
                // 커서를 들여쓰기된 위치로 이동
                input.caretPosition = pos + innerIndent.Length;
            }
            else
            {
                // 일반 들여쓰기 적용
                input.text = text.Insert(pos, indent);
                input.caretPosition = pos + indent.Length;
            }
        }
        catch (System.ArgumentOutOfRangeException)
        {
            Debug.Log( "윗줄의 내용이 비어있습니다" );
        }
    }
}
