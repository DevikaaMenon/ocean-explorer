using UnityEngine;

[CreateAssetMenu(fileName = "FishData", menuName = "Fish/Fish Data")]
public class FishData : ScriptableObject {
    public string FishName;
    [TextArea(3, 6)]
    public string Description;
    public Sprite Icon;
    public string Habitat;
    public string Diet;
    public string Size;
}
