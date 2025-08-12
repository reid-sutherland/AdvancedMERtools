using LabApi.Loader.Features.Paths;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;

namespace AdvancedMERTools;

public class Config
{
    [Description("Whether debug logs are written to the console.")]
    public bool Debug { get; set; } = false;

    [Description("The path to AMERT audio files. Defaults to '.../SCP Secret Laboratory/LabAPI/audio'.")]
    public string AudioFolderPath { get; set; } = Path.Combine(PathManager.LabApi.FullName, "audio");

    [Description("When enabled, InteractableObjects that use the keycodes in the list below will be created as InteractableToys." +
        "\n# This means that a player will simply need to press their base-game Interact button to interact with them, and SS keybinds will not be needed.")]
    public bool EnableIoToys { get; set; } = false;

    [Description("With IOToys enabled, IO schematics that use an InputKeyCode value from this list will be spawned as InteractableToys instead." +
        "\n# Note that the number is the ASCII value of the keyboard character, for example 101 => the 'E' key. If a schematic does not have an InputKeyCode, it will be treated as a 0.")]
    public List<int> IoToysKeycodes { get; set; } = new() { 0, 101 };

    [Description("Set this to false to disable IO toys on root IO components. Some schematics have issues where the root component is in a different location than the components making up the overall shape.")]
    public bool IoToysNoRoot { get; set; } = false;

    [Description("If enabled, IOs using InteractableToys will spawn a visible primitive to represent the toy.")]
    public bool IoToysDebug { get; set; } = false;

    //public bool ReplacementMode { get; set; } = true;
    //public List<string> DummyDoorInstallingMaps { get; set; } = new List<string>
    //{
    //    "ExampleMap"
    //};

    //[Description("Below index means the door numbering that do not require dummy door installing. Caution: It's double List. - (Above Map Index) - (Door Index)")]
    //public List<List<int>> BlackListOfDummyDoor { get; set; } = new List<List<int>>
    //{
    //    new List<int>
    //    {
    //        101
    //    },
    //    new List<int>
    //    {
    //        15
    //    }
    //};

    public Dictionary<string, List<GateSerializable>> Gates { get; set; } = new Dictionary<string, List<GateSerializable>>
    {
        { "ExampleMapName", new List<GateSerializable> { new GateSerializable(), new GateSerializable() } },
    };

    [Description("If turned on, it will autowork with every MER's door spawning event.")]
    public bool AutoRun { get; set; } = false;

    //[Description("Generated, Round, Decont, Warhead")]
    //public List<EventList> AutoRunOnEventList { get; set; } = new List<EventList>
    //{
    //    Config.EventList.Generated,
    //};

    //[Description("If you load massive schematic, extend delay.")]
    //public float AutoRunDelay { get; set; } = 2f;

    //[Description("If you build doors with external-plugin not MER, use this option.")]
    //public bool AutoRunWithEveryDoor { get; set; } = false;

    public bool CustomSpawnPointEnable { get; set; } = true;
    //public bool UseExperimentalFeature { get; set; } = false;

    [Serializable]
    public enum EventList
    {
        Generated,
        Round,
        Decont,
        Warhead,
    }
}
