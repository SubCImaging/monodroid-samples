using System.Reflection;

[assembly: Obfuscation(Feature = @"assembly probing path $(SolutionDir)../../Libraries")]
[assembly: Obfuscation(Feature = "optimization", Exclude = true)]
[assembly: Obfuscation(Feature = "embed $(SolutionDir)../../Libraries/SubCTools.dll", Exclude = false)]
[assembly: Obfuscation(Feature = "embed $(SolutionDir)../../Libraries/SubCTools.Extras.dll", Exclude = false)]
[assembly: Obfuscation(Feature = "embed $(SolutionDir)../../Libraries/SubCTools.MLProtect.dll", Exclude = false)]
[assembly: Obfuscation(Feature = "embed $(SolutionDir)../../Libraries/SubCTools.Controls.Devices.dll", Exclude = false)]
[assembly: Obfuscation(Feature = "embed $(SolutionDir)../../Libraries/SubCTools.SubCUI.dll", Exclude = false)]
[assembly: Obfuscation(Feature = "embed $(SolutionDir)../../Libraries/SubCTools.SubCLive.dll", Exclude = false)]
[assembly: Obfuscation(Feature = "embed $(SolutionDir)../../Libraries/SubCTools.Decklink.dll", Exclude = false)]
[assembly: Obfuscation(Feature = "embed $(SolutionDir)../../Libraries/SubCTools.Underwater.dll", Exclude = false)]