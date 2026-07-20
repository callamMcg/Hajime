using UnityEngine;
using TMPro;

public class TechniquesScreen : MenuScreen
{
    //----------Variables----------\\
    //List of techniques
    [SerializeField] private TechniqueInfo[] techniques;
    int currentIndex = 0;

    //Text References
    [SerializeField] private TMP_Text nameLabel;
    [SerializeField] private TMP_Text japLabel;
    [SerializeField] private TMP_Text descriptionLabel;
    [SerializeField] private TMP_Text stepsArea;

    //----------Override Functions----------\\
    protected override void OnEnter()
    {
        currentIndex = 0;
        if (techniques.Length > 0) Show(currentIndex);
    }
    float t = 0;
    private void Update()
    {
        if(t > 0)
        {
            t -= Time.deltaTime;
            return;
        }
        int newIndex = currentIndex;
        float movement = InputReader.Instance.Navigate.x;
        if (movement > 0)
            newIndex++;
        else if(movement  < 0)
            newIndex--;

        if (newIndex > techniques.Length - 1)
            newIndex = 0;
        else if(newIndex < 0)
            newIndex = techniques.Length -1;

        if(newIndex != currentIndex)
        {
            currentIndex = newIndex;
            Show(currentIndex);
            t = 1;
        }
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