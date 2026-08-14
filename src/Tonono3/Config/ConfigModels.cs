using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using VYaml.Annotations;

namespace Tonono3;

public record class AppConfig(
    ImmutableArray<KeyValuePair<string, string>> RomajiTable,
    ImmutableArray<KeyValuePair<string, string>> MoraModifier,
    ImmutableArray<KeyValuePair<string, string>> MoraAutoComplete,
    ImmutableArray<KeyValuePair<char, string>> ZenkakuTable,
    ImmutableArray<string> DictionaryPaths,
    string UserDictionaryPath,
    ImmutableArray<string> ViCompatibleApps)
{
    public bool HasError =>
        RomajiTable.IsEmpty ||
        MoraModifier.IsEmpty ||
        ZenkakuTable.IsEmpty ||
        DictionaryPaths.IsEmpty;

    public bool HasChange(AppConfig other) =>
        UserDictionaryPath != other.UserDictionaryPath ||
        !RomajiTable.SequenceEqual(other.RomajiTable) ||
        !ZenkakuTable.SequenceEqual(other.ZenkakuTable) ||
        !MoraModifier.SequenceEqual(other.MoraModifier) ||
        !MoraAutoComplete.SequenceEqual(other.MoraAutoComplete) ||
        !DictionaryPaths.SequenceEqual(other.DictionaryPaths) ||
        !ViCompatibleApps.SequenceEqual(other.ViCompatibleApps);
}

[YamlObject]
public partial record class RomajiTable(
    string Vowel,
    Dictionary<string, string[]> Rows,
    Dictionary<string, string> Irregular,
    Dictionary<string, List<string>> MoraModifier,
    Dictionary<string, string> MoraAutoComplete);

[YamlObject]
public partial record class Standard(char Start, char End, int Offset);

[YamlObject]
public partial record class ZenkakuTable(Standard Standard, Dictionary<string, string> Irregular);

[YamlObject]
public partial record class ConfigYaml(
    string[] DictionaryPaths,
    string UserDictionaryPath,
    RomajiTable RomajiTable,
    ZenkakuTable ZenkakuTable,
    string[] ViCompatibleApps);
