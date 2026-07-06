using UnityEngine;
using TMPro;

public class TechniquesScreen : MenuScreen
{
    //----------Variables----------\\
    //List of techniques
    [SerializeField] private Technique[] techniques;

    //Text References
    [SerializeField] private TMP_Text nameLabel;
    [SerializeField] private TMP_Text japLabel;
    [SerializeField] private TMP_Text descriptionLabel;
    [SerializeField] private TMP_Text stepsArea;

    //----------Override Functions----------\\
    protected override void OnEnter()
    {
        if (techniques.Length > 0) Show(0);
    }

    //----------Public Functions----------\\
    /*Show
     * 1 - If index out of range do nothing
     * 2 - Update the texts
     * 3 - Set up the steps and update them
     */
    public void Show(int index)
    {
        //1
        if (index < 0 || index >= techniques.Length) return;
        //2
        nameLabel.text = techniques[index].name;
        descriptionLabel.text = techniques[index].description;
        japLabel.text = techniques[index].jap;
        //3
        string steps = "";
        foreach (string step in techniques[index].steps)
        {
            steps += step;
            steps += "\n";
        }
        stepsArea.text = steps;
    }
}