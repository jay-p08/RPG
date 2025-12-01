using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class CommandInputUI : MonoBehaviour
{
    public DroneController drone;
    public TMP_InputField codeInput;
    public Button runButton;
    public Button stopButton;

    void Start()
    {
        runButton.onClick.AddListener(RunScript);
        stopButton.onClick.AddListener(StopScript);
    }

    public void RunScript()
    {
        drone.StartScript();                                                                                                                                                  
        string script = codeInput.text;
        List<Command> cmds = ScriptParser.Parse(script);

        foreach (var cmd in cmds)
            drone.AddCommand(cmd);
    }

    public void StopScript()
    {
        drone.StopScript();
    }
}
