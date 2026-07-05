using UnityEngine;
using TMPro;

public class TechniquesScreen : MenuScreen
{
    [System.Serializable]
    public struct Technique
    {
        public string name;
        public string jap;
        [TextArea] public string description;
        [TextArea] public string[] steps;
    }

    [SerializeField] private Technique[] techniques;
    [SerializeField] private TMP_Text nameLabel;
    [SerializeField] private TMP_Text japLabel;
    [SerializeField] private TMP_Text descriptionLabel;
    [SerializeField] private TMP_Text stepsArea;

    protected override void OnEnter()
    {
        if (techniques.Length > 0) Show(0);
    }

    public void Show(int index)
    {
        if (index < 0 || index >= techniques.Length) return;
        nameLabel.text = techniques[index].name;
        descriptionLabel.text = techniques[index].description;
        japLabel.text = techniques[index].jap;

        string steps = "";
        foreach (string step in techniques[index].steps)
        {
            steps += step;
            steps += "\n";
        }
        stepsArea.text = steps;
    }
}