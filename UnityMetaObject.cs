using VYaml.Annotations;

namespace UGConverter;

[YamlObject]
public partial class UnityMeta
{
    [YamlMember("guid")]
    public string guid { get; set; }
}