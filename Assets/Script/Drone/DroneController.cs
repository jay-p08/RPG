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

    // for, if 등을 위한 변수 저장 공간
    private Dictionary<string, int> variables = new Dictionary<string, int>();


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
                        {
                            Vector2Int dir = (Vector2Int)cmd.data;
                            MoveCommand(dir);
                            yield return new WaitUntil(() => !isMoving);
                        }
                        break;

                    case CommandType.Wait:
                        {
                            int t = (int)cmd.data;
                            yield return new WaitForSeconds(t);
                        }
                        break;

                    case CommandType.If:
                        {
                            var ifData = (IfData)cmd.data;
                            if (EvaluateCondition(ifData.condition))
                                yield return StartCoroutine(RunBlock(ifData.block));
                        }
                        break;

                    case CommandType.While:
                        {
                            var w = (WhileData)cmd.data;
                            while (EvaluateCondition(w.condition))
                                yield return StartCoroutine(RunBlock(w.block));
                        }
                        break;

                    case CommandType.For:
                        {
                            var f = (ForData)cmd.data;

                            ExecuteInit(f.init);

                            while (EvaluateCondition(f.condition))
                            {
                                yield return StartCoroutine(RunBlock(f.block));
                                ExecuteIncrement(f.increment);
                            }
                        }
                        break;

                    case CommandType.VariableAssign:
                        {
                            var v = (VariableAssignData)cmd.data;
                            variables[v.VarName] = GetValue(v.Value);
                        }
                        break;
                    case CommandType.FunctionCall:
                        {
                            var call = (FunctionCallData)cmd.data;
                            yield return StartCoroutine(RunFunction(call));
                        }
                        break;
                }
            }
            yield return null;
        }
    }

    // ▼ 블록 실행 함수
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
                    {
                        if (CodeStop) yield break;
                        yield return StartCoroutine(RunBlock(w.block));
                    }
                    break;

                case CommandType.For:
                    var f = (ForData)cmd.data;
                    ExecuteInit(f.init);
                    while (EvaluateCondition(f.condition))
                    {
                        if (CodeStop) yield break;
                        yield return StartCoroutine(RunBlock(f.block));
                        ExecuteIncrement(f.increment);
                    }
                    break;
                case CommandType.VariableAssign:
                    var v = (VariableAssignData)cmd.data;
                    variables[v.VarName] = GetValue(v.Value);
                    break;
            }
        }
    }
    private IEnumerator RunFunction(FunctionCallData call)
    {
        FunctionData f = ScriptEnvironment.functions[call.name];

        // 파라미터 매핑
        string[] argValues = string.IsNullOrEmpty(call.args) ?
                             new string[0] :
                             call.args.Split(',');

        for (int i = 0; i < f.parameters.Count; i++)
        {
            string paramDef = f.parameters[i];   // "int a"
            string[] parts = paramDef.Split(' ');
            string type = parts[0];
            string name = parts[1];

            ScriptEnvironment.numbers[name] = float.Parse(argValues[i]);
        }

        // 함수 본문 실행
        foreach (var cmd in f.body)
        {
            AddCommand(cmd);
        }

        yield return null;
    }

    public void StartScript()
    {
        CodeStop = false;

        isMoving = false;

        // 현재 위치 고정
        transform.position = new Vector2(currentGridPos.x, currentGridPos.y);
        targetWorldPos = transform.position;

        // 큐 초기화
        commandQueue.Clear();
    }
    public void StopScript()
    {
        CodeStop = true;

        // 코루틴 완전 종료 후 재시작
        StopAllCoroutines();
        StartCoroutine(ProcessCommands());

        // 이동 멈춤
        isMoving = false;

        // 위치 고정
        transform.position = new Vector2(currentGridPos.x, currentGridPos.y);
        targetWorldPos = transform.position;

        // 명령 초기화
        commandQueue.Clear();

        // 변수 초기화
        ScriptEnvironment.ClearVariables();
    }

    // bool E
    // ▼ 조건 평가
    private bool EvaluateCondition(string cond)
    {
        cond = cond.Replace(" ", "");

        // 1) true / false
        if (cond == "true") return true;
        if (cond == "false") return false;

        // 2) 단일 변수일 경우 처리
        if (!cond.Contains(">") && !cond.Contains("<") &&
            !cond.Contains("==") && !cond.Contains("!="))
        {
            float v = GetValue(cond);

            // float이 0이 아니면 true, 0이면 false 로 처리
            // 또는 bool 타입이 따로 있다면 bool로 캐스팅
            return Mathf.Abs(v) > 0.0001f;
        }

        // 3) 비교 연산자 탐색
        string op = "";

        if (cond.Contains(">=")) op = ">=";
        else if (cond.Contains("<=")) op = "<=";
        else if (cond.Contains("==")) op = "==";
        else if (cond.Contains("!=")) op = "!=";
        else if (cond.Contains(">")) op = ">";
        else if (cond.Contains("<")) op = "<";
        else
            throw new Exception("조건식 연산자 오류: " + cond);

        // 4) 비교식 파싱
        string[] parts = cond.Split(new string[] { op }, StringSplitOptions.None);

        string left = parts[0];
        string right = parts[1];

        float leftVal = GetValue(left);
        float rightVal = GetValue(right);

        switch (op)
        {
            case ">": return leftVal > rightVal;
            case "<": return leftVal < rightVal;
            case ">=": return leftVal >= rightVal;
            case "<=": return leftVal <= rightVal;
            case "==": return Mathf.Approximately(leftVal, rightVal);
            case "!=": return !Mathf.Approximately(leftVal, rightVal);
        }

        return false;
    }

    private int GetValue(string token)
    {
        if (token == "true") return 1;
        if (token == "false") return 0;

        if (int.TryParse(token, out int f))
            return f;

        if (variables.ContainsKey(token))
            return variables[token];

        // 변수가 없으면 기본 생성
        variables[token] = 0;
        return 0;
    }

    // ▼ for문의 초기화
    private void ExecuteInit(string init)
    {
        // i = 0
        string[] parts = init.Split('=');
        string var = parts[0].Trim();
        string value = parts[1].Trim();

        variables[var] = GetValue(value);
    }

    // ▼ for문의 증가식
    private void ExecuteIncrement(string inc)
    {
        inc = inc.Trim();

        if (inc.EndsWith("++"))
        {
            string var = inc.Replace("++", "");
            variables[var]++;
            return;
        }
        if (inc.EndsWith("--"))
        {
            string var = inc.Replace("--", "");
            variables[var]--;
            return;
        }

        // i += 2
        if (inc.Contains("+="))
        {
            string[] p = inc.Split("+=");
            variables[p[0].Trim()] += GetValue(p[1].Trim());
            return;
        }

        if (inc.Contains("-="))
        {
            string[] p = inc.Split("-=");
            variables[p[0].Trim()] -= GetValue(p[1].Trim());
            return;
        }
    }

    void Update()
    {
        if (isMoving)
            MoveTowardsTarget();
    }

    void MoveTowardsTarget()
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
