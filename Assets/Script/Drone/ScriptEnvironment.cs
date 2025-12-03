using System.Collections.Generic;

public static class ScriptEnvironment
{
    // 숫자, bool 변수를 분리 저장
    public static Dictionary<string, float> numbers = new();
    public static Dictionary<string, bool> bools = new();

    // 사용자 함수 저장
    public static Dictionary<string, FunctionData> functions = new();

    // 변수 초기화
    public static void ClearVariables()
    {
        numbers.Clear();
        bools.Clear();
    }

    // 함수 초기화 (원하면 유지 가능)
    public static void ClearFunctions()
    {
        functions.Clear();
    }

    // 지역 스코프 지원 (함수 호출 시 push/pop)
    private static Stack<Dictionary<string, float>> numberStack = new();
    private static Stack<Dictionary<string, bool>> boolStack = new();

    public static void PushScope()
    {
        numberStack.Push(new Dictionary<string, float>(numbers));
        boolStack.Push(new Dictionary<string, bool>(bools));
    }

    public static void PopScope()
    {
        if (numberStack.Count > 0)
            numbers = numberStack.Pop();

        if (boolStack.Count > 0)
            bools = boolStack.Pop();
    }
}

public class FunctionData
{
    public string returnType;
    public List<string> parameters;
    public List<Command> body;

    public FunctionData(string returnType, List<string> parameters, List<Command> body)
    {
        this.returnType = returnType;
        this.parameters = parameters;
        this.body = body;
    }
}