using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class ScriptRunner : MonoBehaviour
{
    public DroneController drone;
    public TMP_InputField codeInput;

    public void OnRun()
    {
        List<Command> cmds = ScriptParser.Parse(codeInput.text);

        foreach (var cmd in cmds)
            drone.AddCommand(cmd);
    }
}
