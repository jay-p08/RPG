using System.Collections.Generic;
public static class ScriptEnvironment
{
    public static Dictionary<string, float> numbers = new Dictionary<string, float>();
    public static Dictionary<string, bool> bools = new Dictionary<string, bool>();
    public static Dictionary<string, FunctionData> functions = new();

    public static void ClearVariables()
    {
        numbers.Clear();
        bools.Clear();
    }
    public static void ClearFunctions()
    {
        functions.Clear();
    }
}

public class FunctionData
{
    public string returnType;
    public List<string> parameters;   // "int a", "int b"...
    public List<Command> body;

    public FunctionData(string returnType, List<string> parameters, List<Command> body)
    {
        this.returnType = returnType;
        this.parameters = parameters;
        this.body = body;
    }
}