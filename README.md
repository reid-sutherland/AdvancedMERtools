# How to Install AdvancedMERtools (AMERT)

![image](https://github.com/Maciupek/AdvancedMERtools/blob/main/AMERT.png?raw=true)

## Installation Instructions

This version of the project now depends on the following:
- SCPSL Version 14.1
- [ProjectMER](https://github.com/Michal78900/ProjectMER)
- [LabAPI](https://github.com/northwood-studios/LabAPI)
- [AudioPlayerApi](https://github.com/Killers0992/AudioPlayerApi)

### Main Plugin
To add AMERT to your server:
1. Download the AMERT .dll and the `dependencies.zip` from the [Releases](https://github.com/reid-sutherland/AdvancedMERtools/releases) page
2. Place `AdvancedMERTools.dll` and `ProjectMER.dll` in `%APPDATA%\Roaming\SCP Secret Laboratory\LabAPI\plugins\global`
3. Place `AudioPlayerApi.dll` and `Newtonsoft.Json.dll` in `%APPDATA%\Roaming\SCP Secret Laboratory\LabAPI\dependencies\global`
4. If you are using any AMERT audio tools in the next section, place `AMERTAudioModule.dll` in `%APPDATA%\Roaming\SCP Secret Laboratory\LabAPI\dependencies\global`

### Extra AMERT Tools
1. Download the necessary files from the specified folder above.
2. Drag and drop the files into the 'Project' tab in the Unity Editor. Preferably to `DONT TOUCH/Scripts`
3. In the release tab, you will find a plugin that facilitates the execution of functions and modeling for LCZ, HCZ, and EZ doors.

## AdvancedMERtools Description
AdvancedMERtools offers advanced tools for the SCP: SL plugin called MapEditorReborn (MER). Please download the tools from the folder mentioned above and integrate them into your project. We plan to add more features in the future.

### Features
- **AutoScaler**: Scales grouped objects up or down automatically.
- **Rotator**: Rotates objects akin to the behavior of a magnetic field or sphere.
- **Mirror**: Creates a mirrored copy of objects.
- **Arrange**: Duplicates and arranges objects.
- **Health Object**: Adds a hitbox to your objects. **Requires additional plugin.**
- **Interactable Pickup**: Functions similarly to the Health Object. **Requires additional plugin.**
- **Custom Collider**: Adds custom collision properties to objects.
- **Interactable Teleporters**: 
- **Text**: Adds text to set empty object.
- **Custom RGBA**: Extends the minimum and maximum RGBA values beyond 0-255 to enhance object glow.
- **Converter**: Converts Unity's primitives into MER's components if placed accidentally. Activating it will also automatically color your primitives based on the set material.

## Tutorials
- Arrange Tutorial: [Watch Tutorial](https://youtu.be/adXuM0UINhE)
- Text Tutorial: [Watch Tutorial](https://youtu.be/UmkEbiVhDTE)

![image](https://github.com/MujisongPlay/AdvancedMERtools/assets/96275409/3249ec64-4bfc-4071-98fb-51d1052cc8e6)

## DummyDoorSpawner
The DummyDoorSpawner creates visible dummy doors to counteract the visibility issues caused by the culling system.

- Showcase Video: [Watch Showcase](https://youtu.be/TLkXputvKFc)
- Straightforward Installation Tutorial: [Watch Tutorial](https://youtu.be/-_IvE2kCHvU)

## Development / Contribute

To build the project, you just need to define the following environment variables:
- "LABAPI_PLUGINS" => "%APPDATA%\Roaming\SCP Secret Laboratory\LabAPI\plugins\global"
- "LABAPI_DEPENDENCIES" => "%APPDATA%\Roaming\SCP Secret Laboratory\LabAPI\dependencies\global"
- "SL_REFERENCES" => "<SCPSL server path>\SCPSL_Data\Managed"

Where <SCPSL server path> is the path to your SCPSL server folder. This is the same as "EXILED_REFERENCES" if you have developed EXILED plugins.