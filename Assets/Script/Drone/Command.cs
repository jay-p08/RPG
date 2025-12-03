using System.Collections.Generic;
using UnityEngine;

public enum CommandType
{
    Move,
    Wait,
    Cast,
    Reel,
    Store,

    If,
    While,
    For,
    VariableAssign,

    FunctionCall,
}

public class Command
{
    public CommandType type;
    public object data;

    public Command(CommandType t, object d = null)
    {
        type = t;
        data = d;
    }
}

// 데이터 클래스들
public class IfData
{
    public string condition;
    public List<Command> block;
    public IfData(string c, List<Command> b) { condition = c; block = b; }
}

public class WhileData
{
    public string condition;
    public List<Command> block;
    public WhileData(string c, List<Command> b) { condition = c; block = b; }
}

public class ForData
{
    public string init;
    public string condition;
    public string increment;
    public List<Command> block;

    public ForData(string init, string cond, string inc, List<Command> b)
    {
        this.init = init;
        this.condition = cond;
        this.increment = inc;
        this.block = b;
    }
}

public class VariableAssignData
{
    public string VarName;
    public string Value;
    public string Type;

    public VariableAssignData(string name, string value, string type = null)
    {
        VarName = name;
        Value = value;
        Type = type;
    }
}

public class FunctionCallData
{
    public string name;
    public string args;

    public FunctionCallData(string name, string args)
    {
        this.name = name;
        this.args = args;
    }
}