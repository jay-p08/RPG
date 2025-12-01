using System;
using System.Collections.Generic;
using UnityEngine;

public static class ScriptParser
{
    public static List<Command> Parse(string script)
    {
        string[] lines = script.Split('\n');
        int index = 0;

        return ParseBlock(lines, ref index);
    }

    private static List<Command> ParseBlock(string[] lines, ref int index)
    {
        List<Command> list = new List<Command>();

        while (index < lines.Length)
        {
            string line = lines[index].Trim();
            line = line.TrimEnd(';');

            // 함수 정의: int Sum(int a, int b)
            if ((line.StartsWith("int ") || line.StartsWith("float ") || line.StartsWith("bool ") || line.StartsWith("void "))
                && line.Contains("(") && line.Contains(")") && line.EndsWith("{") == false)
            {
                list.Add(ParseFunction(lines, ref index));
                continue;
            }

            if (string.IsNullOrEmpty(line))
            {
                index++;
                continue;
            }

            if (line == "}")
            {
                index++;
                break;
            }

            // IF
            if (line.StartsWith("if"))
            {
                list.Add(ParseIf(lines, ref index));
                continue;
            }

            // WHILE
            if (line.StartsWith("while"))
            {
                list.Add(ParseWhile(lines, ref index));
                continue;
            }

            // FOR
            if (line.StartsWith("for"))
            {
                list.Add(ParseFor(lines, ref index));
                continue;
            }
            // ------------------------------------------
            // 통합된 변수 선언 + 할당
            // ------------------------------------------
            if (line.Contains("=") && !line.Contains("("))
            {
                string left = line.Split('=')[0].Trim();
                string right = line.Split('=')[1].Trim();

                string varType = null;
                string varName = left;

                // 타입이 포함된 경우
                if (left.StartsWith("int ") || left.StartsWith("float ") || left.StartsWith("bool "))
                {
                    if (left.StartsWith("int "))
                    {
                        varType = "int";
                        varName = left.Substring(3).Trim();
                    }
                    else if (left.StartsWith("float "))
                    {
                        varType = "float";
                        varName = left.Substring(5).Trim();
                    }
                    else if (left.StartsWith("bool "))
                    {
                        varType = "bool";
                        varName = left.Substring(4).Trim();
                    }
                }

                list.Add(new Command(
                    CommandType.VariableAssign,
                    new VariableAssignData(varName, right, varType)
                ));

                index++;
                continue;
            }

            // --------------------------
            //   일반 명령 (Move / Wait 등)
            // --------------------------
            Command cmd = ParseSingleCommand(line);
            if (cmd != null)
                list.Add(cmd);

            index++;
        }

        return list;
    }

    // --------------------------
    //   단일 명령 처리
    // --------------------------
    private static Command ParseSingleCommand(string line)
    {
        int open = line.IndexOf("(");
        int close = line.IndexOf(")");
        if (open == -1 || close == -1)
            return null;

        string func = line.Substring(0, open).Trim().ToLower();
        string args = line.Substring(open + 1, close - open - 1).Trim();

        switch (func)
        {
            case "move":
                return new Command(CommandType.Move, ParseMoveArg(args));

            case "wait":
                return new Command(CommandType.Wait, int.Parse(args));
        }
        // 함수 호출
        if (ScriptEnvironment.functions.ContainsKey(func))
        {
            return new Command(CommandType.FunctionCall, new FunctionCallData(func, args));
        }

        return null;
    }

    private static Vector2Int ParseMoveArg(string arg)
    {
        switch (arg)
        {
            case "up": return Vector2Int.up;
            case "down": return Vector2Int.down;
            case "left": return Vector2Int.left;
            case "right": return Vector2Int.right;
        }

        // 숫자 좌표
        string[] parts = arg.Split(',');
        if (parts.Length == 2)
            return new Vector2Int(int.Parse(parts[0]), int.Parse(parts[1]));

        return Vector2Int.zero;
    }

    // --------------------------
    //   IF 문
    // --------------------------
    private static Command ParseIf(string[] lines, ref int index)
    {
        // if 조건
        string line = lines[index].Trim();
        int open = line.IndexOf("(");
        int close = line.LastIndexOf(")");

        string condition = line.Substring(open + 1, close - open - 1).Trim();

        // 다음줄이 { 이어야 함
        index++;
        if (lines[index].Trim() != "{")
            throw new Exception("If 문은 { 가 필요함");

        index++; // { 넘기기
        List<Command> block = ParseBlock(lines, ref index);

        return new Command(CommandType.If, new IfData(condition, block));
    }

    // --------------------------
    //   WHILE 문
    // --------------------------
    private static Command ParseWhile(string[] lines, ref int index)
    {
        string line = lines[index].Trim();
        int open = line.IndexOf("(");
        int close = line.LastIndexOf(")");

        string condition = line.Substring(open + 1, close - open - 1).Trim();

        index++;
        if (lines[index].Trim() != "{")
            throw new Exception("While 문은 { 가 필요함");

        index++;
        List<Command> block = ParseBlock(lines, ref index);

        return new Command(CommandType.While, new WhileData(condition, block));
    }

    // --------------------------
    //   FOR 문
    // --------------------------
    private static Command ParseFor(string[] lines, ref int index)
    {
        string line = lines[index].Trim();
        int open = line.IndexOf("(");
        int close = line.LastIndexOf(")");

        string inside = line.Substring(open + 1, close - open - 1);

        // i = 0; i < 3; i++
        string[] parts = inside.Split(';');
        string init = parts[0].Trim();
        string condition = parts[1].Trim();
        string increment = parts[2].Trim();

        index++;
        if (lines[index].Trim() != "{")
            throw new Exception("For 문은 { 가 필요함");

        index++;
        List<Command> block = ParseBlock(lines, ref index);

        return new Command(CommandType.For, new ForData(init, condition, increment, block));
    }

    // --------------------------
    //   사용자 정의 함수
    // --------------------------
    private static Command ParseFunction(string[] lines, ref int index)
    {
        string header = lines[index].Trim();

        int typeEnd = header.IndexOf(' ');
        string returnType = header.Substring(0, typeEnd).Trim();

        int nameEnd = header.IndexOf('(');
        string funcName = header.Substring(typeEnd, nameEnd - typeEnd).Trim();

        int open = header.IndexOf('(');
        int close = header.IndexOf(')');
        string paramStr = header.Substring(open + 1, close - open - 1).Trim();

        List<string> parameters = new();

        if (!string.IsNullOrEmpty(paramStr))
        {
            foreach (string p in paramStr.Split(','))
                parameters.Add(p.Trim());  // "int a"
        }

        // 다음줄 { 체크
        index++;
        if (lines[index].Trim() != "{")
            throw new Exception("함수 정의에는 { 가 필요합니다.");

        index++;
        List<Command> body = ParseBlock(lines, ref index);

        // ScriptEnvironment 에 저장
        ScriptEnvironment.functions[funcName] = new FunctionData(returnType, parameters, body);

        // 함수 정의는 실행 명령이 아니다 → null 반환
        return null;
    }
}

