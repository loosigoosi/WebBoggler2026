using System.Runtime.Serialization;

/// <summary>
/// Metadati compressi per trasporto WordList via SignalR
/// </summary>
[DataContract]
public class WordListMetadata
{
    [DataMember]
    public string[] DicesArray { get; set; }

    [DataMember]
    public bool[] DuplicatedPropertyArray { get; set; }
}
