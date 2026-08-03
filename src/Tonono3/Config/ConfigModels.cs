using System;
using System.Collections.Generic;
using System.Linq;
using VYaml.Annotations;

namespace Tonono3;

public record class AppConfig(
    Dictionary<string, string> RomajiTable,
    Dictionary<string, string> MoraModifier,
    Dictionary<string, string> MoraAutoComplete,
    Dictionary<char, string> ZenkakuTable,
    string[] DictionaryPaths,
    string UserDictionaryPath,
    string[] ViCompatibleApps)
{
    public bool HasError => Enumerable.Any([RomajiTable.Count, MoraModifier.Count, ZenkakuTable.Count, DictionaryPaths.Length], i => i < 1);
    public bool HasChange(AppConfig other) => !(
        UserDictionaryPath == other.UserDictionaryPath &&
        DictionaryEqual(RomajiTable, other.RomajiTable) &&
        DictionaryEqual(ZenkakuTable, other.ZenkakuTable) &&
        DictionaryEqual(MoraModifier, other.MoraModifier) &&
        DictionaryEqual(MoraAutoComplete, other.MoraAutoComplete) &&
        DictionaryPaths.SequenceEqual(other.DictionaryPaths) &&
        ViCompatibleApps.SequenceEqual(other.ViCompatibleApps));

    private static bool DictionaryEqual<TKey, TValue>(Dictionary<TKey, TValue> left, Dictionary<TKey, TValue> right)
        where TKey : notnull =>
        left.Count == right.Count && left.All(pair => right.TryGetValue(pair.Key, out var value) && EqualityComparer<TValue>.Default.Equals(pair.Value, value));

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
