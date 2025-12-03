using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class DroneController : MonoBehaviour
{
    public float moveSpeed = 3f;
    public static bool CodeStop = false;

    private Queue<Command> commandQueue = new Queue<Command>();

    private Vector2Int currentGridPos;
    private Vector2 targetWorldPos;

    private bool isMoving = false;


    void Start()
    {
        currentGridPos = Vector2Int.RoundToInt(transform.position);
        targetWorldPos = transform.position;

        StartCoroutine(ProcessCommands());
    }


    public void AddCommand(Command cmd)
    {
        commandQueue.Enqueue(cmd);
    }


    private IEnumerator ProcessCommands()
    {
        while (true)
        {
            if (CodeStop)
            {
                isMoving = false;
                yield return null;
                continue;
            }

            if (commandQueue.Count > 0 && !isMoving)
            {
                Command cmd = commandQueue.Dequeue();

                switch (cmd.type)
                {
                    case CommandType.Move:
                        MoveCommand((Vector2Int)cmd.data);
                        yield return new WaitUntil(() => !isMoving);
                        break;

                    case CommandType.Wait:
                        yield return new WaitForSeconds((int)cmd.data);
                        break;

                    case CommandType.If:
                        var ifData = (IfData)cmd.data;
                        if (EvaluateCondition(ifData.condition))
                            yield return StartCoroutine(RunBlock(ifData.block));
                        break;

                    case CommandType.While:
                        var w = (WhileData)cmd.data;
                        while (EvaluateCondition(w.condition))
                            yield return StartCoroutine(RunBlock(w.block));
                        break;

                    case CommandType.For:
                        var f = (ForData)cmd.data;
                        ExecuteInit(f.init);
                        while (EvaluateCondition(f.condition))
                        {
                            yield return StartCoroutine(RunBlock(f.block));
                            ExecuteIncrement(f.increment);
                        }
                        break;

                    case CommandType.VariableAssign:
                        var v = (VariableAssignData)cmd.data;
                        ScriptEnvironment.numbers[v.VarName] = GetValue(v.Value);
                        break;

                    case CommandType.FunctionCall:
                        yield return StartCoroutine(RunFunction((FunctionCallData)cmd.data));
                        break;
                }
            }

            yield return null;
        }
    }



    // -----------------------------
    // 블록 실행
    // -----------------------------
    private IEnumerator RunBlock(List<Command> block)
    {
        foreach (var cmd in block)
        {
            if (CodeStop) yield break;

            switch (cmd.type)
            {
                case CommandType.Move:
                    MoveCommand((Vector2Int)cmd.data);
                    yield return new WaitUntil(() => !isMoving);
                    break;

                case CommandType.Wait:
                    yield return new WaitForSeconds((int)cmd.data);
                    break;

                case CommandType.If:
                    var ifD = (IfData)cmd.data;
                    if (EvaluateCondition(ifD.condition))
                        yield return StartCoroutine(RunBlock(ifD.block));
                    break;

                case CommandType.While:
                    var w = (WhileData)cmd.data;
                    while (EvaluateCondition(w.condition))
                        yield return StartCoroutine(RunBlock(w.block));
                    break;

                case CommandType.For:
                    var f = (ForData)cmd.data;
                    ExecuteInit(f.init);
                    while (EvaluateCondition(f.condition))
                    {
                        yield return StartCoroutine(RunBlock(f.block));
                        ExecuteIncrement(f.increment);
                    }
                    break;

                case CommandType.VariableAssign:
                    var va = (VariableAssignData)cmd.data;
                    ScriptEnvironment.numbers[va.VarName] = GetValue(va.Value);
                    break;

                case CommandType.FunctionCall:
                    yield return StartCoroutine(RunFunction((FunctionCallData)cmd.data));
                    break;
            }
        }
    }



    // -----------------------------
    // 함수 실행
    // -----------------------------
    private IEnumerator RunFunction(FunctionCallData call)
    {
        // 함수 정보 가져오기
        var f = ScriptEnvironment.functions[call.name];

        // 파라미터 적용
        string[] argValues = string.IsNullOrEmpty(call.args)
            ? new string[0]
            : call.args.Split(',');

        for (int i = 0; i < f.parameters.Count; i++)
        {
            string[] parts = f.parameters[i].Split(' ');
            string paramName = parts[1];

            ScriptEnvironment.numbers[paramName] = GetValue(argValues[i].Trim());
        }

        // 함수 본문 실행
        yield return StartCoroutine(RunBlock(f.body));
    }

    // -----------------------------
    // 스크립트 제어
    // -----------------------------
    public void StartScript()
    {
        CodeStop = false;
        isMoving = false;

        // ★★★ 이게 문제 해결 핵심 ★★★
        currentGridPos = Vector2Int.RoundToInt(transform.position);

        targetWorldPos = transform.position;

        commandQueue.Clear();

        // ★ 실행할 때마다 항상 코루틴 재시작 ★
        StopAllCoroutines();
        StartCoroutine(ProcessCommands());
    }

    public void StopScript()
    {
        CodeStop = true;

        StopAllCoroutines();
        StartCoroutine(ProcessCommands());

        isMoving = false;

        // ★★★ 이것도 넣어야 한다 ★★★
        currentGridPos = Vector2Int.RoundToInt(transform.position);

        transform.position = targetWorldPos = transform.position;

        commandQueue.Clear();
        ScriptEnvironment.ClearVariables();
    }



    // -----------------------------
    // 조건/값 계산
    // -----------------------------
    private bool EvaluateCondition(string cond)
    {
        cond = cond.Replace(" ", "");

        if (cond == "true") return true;
        if (cond == "false") return false;

        if (!cond.Contains(">") && !cond.Contains("<") &&
            !cond.Contains("==") && !cond.Contains("!="))
        {
            return Mathf.Abs(GetValue(cond)) > 0.0001f;
        }

        string op = "";

        if (cond.Contains(">=")) op = ">=";
        else if (cond.Contains("<=")) op = "<=";
        else if (cond.Contains("==")) op = "==";
        else if (cond.Contains("!=")) op = "!=";
        else if (cond.Contains(">")) op = ">";
        else if (cond.Contains("<")) op = "<";

        string[] p = cond.Split(new string[] { op }, StringSplitOptions.None);
        float left = GetValue(p[0]);
        float right = GetValue(p[1]);

        return op switch
        {
            ">" => left > right,
            "<" => left < right,
            ">=" => left >= right,
            "<=" => left <= right,
            "==" => Mathf.Approximately(left, right),
            "!=" => !Mathf.Approximately(left, right),
            _ => false
        };
    }

    private float GetValue(string token)
    {
        token = token.Trim();

        if (token == "true") return 1;
        if (token == "false") return 0;

        if (float.TryParse(token, out float f))
            return f;

        if (ScriptEnvironment.numbers.ContainsKey(token))
            return ScriptEnvironment.numbers[token];

        ScriptEnvironment.numbers[token] = 0;
        return 0;
    }



    // -----------------------------
    // for 구문
    // -----------------------------
    private void ExecuteInit(string init)
    {
        string[] parts = init.Split('=');
        string var = parts[0].Trim();
        string value = parts[1].Trim();

        ScriptEnvironment.numbers[var] = GetValue(value);
    }

    private void ExecuteIncrement(string inc)
    {
        inc = inc.Trim();

        if (inc.EndsWith("++"))
        {
            string var = inc.Replace("++", "");
            ScriptEnvironment.numbers[var]++;
            return;
        }

        if (inc.EndsWith("--"))
        {
            string var = inc.Replace("--", "");
            ScriptEnvironment.numbers[var]--;
            return;
        }

        if (inc.Contains("+="))
        {
            string[] p = inc.Split("+=");
            ScriptEnvironment.numbers[p[0].Trim()] += GetValue(p[1].Trim());
            return;
        }

        if (inc.Contains("-="))
        {
            string[] p = inc.Split("-=");
            ScriptEnvironment.numbers[p[0].Trim()] -= GetValue(p[1].Trim());
            return;
        }
    }



    // -----------------------------
    // 이동 처리
    // -----------------------------
    void Update()
    {
        if (isMoving)
            MoveTowardsTarget();
    }

    private void MoveTowardsTarget()
    {
        transform.position = Vector2.MoveTowards(transform.position, targetWorldPos, moveSpeed * Time.deltaTime);
        if (Vector2.Distance(transform.position, targetWorldPos) < 0.01f)
        {
            transform.position = targetWorldPos;
            isMoving = false;
        }
    }

    public void MoveCommand(Vector2Int dir)
    {
        Vector2Int nextPos = currentGridPos + dir;
        targetWorldPos = new Vector2(nextPos.x, nextPos.y);
        isMoving = true;
        currentGridPos = nextPos;
    }
}