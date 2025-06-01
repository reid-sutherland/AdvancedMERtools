using LabApi.Loader.Features.Paths;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;

namespace AdvancedMERTools;

public class Config
{
    public bool IsEnabled { get; set; } = true;
    public bool Debug { get; set; } = false;

    [Description("The path to LabAPI audio files")]
    public string AudioFolderPath { get; set; } = Path.Combine(PathManager.LabApi.FullName, "audio");

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
