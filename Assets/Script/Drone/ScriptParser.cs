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

            // ★ 함수 정의
            if (IsFunctionHeader(line))
            {
                ParseFunction(lines, ref index);
                continue;
            }

            // if / while / for
            if (line.StartsWith("if"))
            {
                list.Add(ParseIf(lines, ref index));
                continue;
            }
            if (line.StartsWith("while"))
            {
                list.Add(ParseWhile(lines, ref index));
                continue;
            }
            if (line.StartsWith("for"))
            {
                list.Add(ParseFor(lines, ref index));
                continue;
            }

            // 변수 (선언 + 할당 포함)
            if (line.Contains("=") && !line.Contains("("))
            {
                list.Add(ParseVariableAssign(line));
                index++;
                continue;
            }

            // 명령 또는 함수 호출
            Command cmd = ParseSingleCommand(line);
            if (cmd != null)
                list.Add(cmd);

            index++;
        }

        return list;
    }

    private static bool IsFunctionHeader(string line)
    {
        return (line.StartsWith("int ") ||
                line.StartsWith("float ") ||
                line.StartsWith("bool ") ||
                line.StartsWith("void "))
                && line.Contains("(")
                && line.Contains(")")
                && !line.EndsWith("{");
    }

    private static Command ParseVariableAssign(string line)
    {
        string left = line.Split('=')[0].Trim();
        string right = line.Split('=')[1].Trim();

        string type = null;
        string name = left;

        if (left.StartsWith("int "))
        {
            type = "int";
            name = left.Substring(3).Trim();
        }
        else if (left.StartsWith("float "))
        {
            type = "float";
            name = left.Substring(5).Trim();
        }
        else if (left.StartsWith("bool "))
        {
            type = "bool";
            name = left.Substring(4).Trim();
        }

        return new Command(CommandType.VariableAssign, new VariableAssignData(name, right, type));
    }

    private static Command ParseSingleCommand(string line)
    {
        int open = line.IndexOf("(");
        int close = line.IndexOf(")");
        if (open == -1 || close == -1)
            return null;

        string func = line.Substring(0, open).Trim().ToLower();   // <-- toLower
        string args = line.Substring(open + 1, close - open - 1).Trim();

        // 인자도 소문자 처리하되, 숫자/좌표는 그대로 파싱되도록 처리
        string argsLowerForNames = args.ToLower().Trim();

        switch (func)
        {
            case "move":
                return new Command(CommandType.Move, ParseMoveArg(argsLowerForNames));

            case "wait":
                // wait은 정수/실수 허용. 여기선 int 사용하던 기존 로직 유지
                if (int.TryParse(args, out int iv)) return new Command(CommandType.Wait, iv);
                if (float.TryParse(args, out float fv)) return new Command(CommandType.Wait, (int)fv);
                return null;
        }

        // 함수 호출 (사용자 정의 함수) — 함수 이름 소문자화 일관화 필요
        // ScriptEnvironment.functions 키를 저장할 때 소문자 키로 저장했다면 여기서도 ToLower()
        if (ScriptEnvironment.functions.ContainsKey(func.ToLower()))
        {
            return new Command(CommandType.FunctionCall, new FunctionCallData(func, args));
        }

        return null;
    }

    private static Vector2Int ParseMoveArg(string arg)
    {
        if (string.IsNullOrWhiteSpace(arg)) return Vector2Int.zero;

        string a = arg.Trim().ToLower();

        switch (a)
        {
            case "up": return Vector2Int.up;
            case "down": return Vector2Int.down;
            case "left": return Vector2Int.left;
            case "right": return Vector2Int.right;
        }

        // 숫자 좌표: "1, 2" 같은 형태 허용 (공백 허용)
        string[] parts = arg.Split(',');
        if (parts.Length == 2)
        {
            if (int.TryParse(parts[0].Trim(), out int x) && int.TryParse(parts[1].Trim(), out int y))
                return new Vector2Int(x, y);
        }

        return Vector2Int.zero;
    }

    // IF
    private static Command ParseIf(string[] lines, ref int index)
    {
        string line = lines[index].Trim();
        string cond = line.Substring(line.IndexOf("(") + 1);
        cond = cond.Substring(0, cond.LastIndexOf(")"));
        index++;

        if (lines[index].Trim() != "{")
            throw new Exception("If must have {");

        index++;
        List<Command> block = ParseBlock(lines, ref index);

        return new Command(CommandType.If, new IfData(cond, block));
    }

    private static Command ParseWhile(string[] lines, ref int index)
    {
        string line = lines[index].Trim();
        string cond = line.Substring(line.IndexOf("(") + 1);
        cond = cond.Substring(0, cond.LastIndexOf(")"));
        index++;

        if (lines[index].Trim() != "{")
            throw new Exception("While must have {");

        index++;
        List<Command> block = ParseBlock(lines, ref index);

        return new Command(CommandType.While, new WhileData(cond, block));
    }

    private static Command ParseFor(string[] lines, ref int index)
    {
        string line = lines[index].Trim();
        string inside = line.Substring(line.IndexOf("(") + 1);
        inside = inside.Substring(0, inside.LastIndexOf(")"));

        string[] parts = inside.Split(';');
        string init = parts[0].Trim();
        string cond = parts[1].Trim();
        string inc = parts[2].Trim();

        index++;
        if (lines[index].Trim() != "{")
            throw new Exception("For must have {");

        index++;
        List<Command> block = ParseBlock(lines, ref index);

        return new Command(CommandType.For, new ForData(init, cond, inc, block));
    }

    private static void ParseFunction(string[] lines, ref int index)
    {
        string header = lines[index].Trim();
        int typeEnd = header.IndexOf(' ');
        string returnType = header.Substring(0, typeEnd);

        int nameEnd = header.IndexOf("(");
        string funcName = header.Substring(typeEnd, nameEnd - typeEnd).Trim();

        string paramStr = header.Substring(header.IndexOf("(") + 1);
        paramStr = paramStr.Substring(0, paramStr.LastIndexOf(")"));

        List<string> parameters = new();
        if (!string.IsNullOrEmpty(paramStr))
        {
            foreach (var p in paramStr.Split(','))
                parameters.Add(p.Trim());
        }

        index++;
        if (lines[index].Trim() != "{")
            throw new Exception("Function must have {");

        index++;
        List<Command> body = ParseBlock(lines, ref index);

        ScriptEnvironment.functions[funcName.ToLower()] = new FunctionData(returnType, parameters, body);
    }
}