using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FishInfoPanel : MonoBehaviour {
    [SerializeField] private GameObject Panel;
    [SerializeField] private TMP_Text NameText;
    [SerializeField] private TMP_Text DescriptionText;
    [SerializeField] private TMP_Text HabitatText;
    [SerializeField] private TMP_Text DietText;
    [SerializeField] private TMP_Text SizeText;
    [SerializeField] private Image FishIcon;

    void Awake() {
        Panel.SetActive(false);
    }

    public void Show(FishData data) {
        Panel.SetActive(true);
        NameText.text = data.FishName;
        DescriptionText.text = data.Description;
        HabitatText.text = $"Habitat: {data.Habitat}";
        DietText.text = $"Diet: {data.Diet}";
        SizeText.text = $"Size: {data.Size}";
        if (data.Icon != null) {
            FishIcon.sprite = data.Icon;
            FishIcon.gameObject.SetActive(true);
        } else {
            FishIcon.gameObject.SetActive(false);
        }
    }

    public void Hide() {
        Panel.SetActive(false);
    }
}
