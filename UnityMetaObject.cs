using VYaml.Annotations;

namespace UGConverter;

[YamlObject]
public partial class UnityMetaObject
{
    [YamlMember("guid")]
    public string guid { get; set; }
}