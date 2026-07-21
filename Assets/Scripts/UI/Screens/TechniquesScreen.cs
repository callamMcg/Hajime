using UnityEngine;
using TMPro;

/// <summary>
/// The page that explains the techniques.
/// It holds a list of them filled in from the inspector and shows one at a time:
/// its name, its name in Japanese, what it does, and the steps to perform it.
/// Pushing left or right moves through the list, wrapping round at either end so
/// it can be cycled through in either direction forever. A short wait after each
/// change stops one push from racing through the whole list at once.
/// The steps are stored as separate lines and stitched together into one block,
/// so each technique can have as many or as few as it needs.
/// </summary>
public class TechniquesScreen : MenuScreen
{
    //----------Variables----------\\

    // Every technique this page can show, filled in from the inspector
    [SerializeField] private TechniqueInfo[] techniques;

    // Which one is currently on show
    int currentIndex = 0;

    // Seconds left before another push will be listened to
    float t = 0;

    // Where the technique's name goes
    [SerializeField] private TMP_Text nameLabel;

    // Where its Japanese name goes
    [SerializeField] private TMP_Text japLabel;

    // Where the description of it goes
    [SerializeField] private TMP_Text descriptionLabel;

    // Where the list of steps goes
    [SerializeField] private TMP_Text stepsArea;

    //----------Override Functions----------\\

    /* ON ENTER
     * 1 - Go back to the start of the list, so the page always opens on the
     *     first technique rather than wherever it was left
     * 2 - Show it, as long as there is anything in the list
     */
    protected override void OnEnter()
    {
        currentIndex = 0;
        if (techniques.Length > 0) Show(currentIndex);
    }

    //----------Event Loop----------\\

    /* UPDATE
     * 1 - If we are still waiting after the last change, count that down and
     *     ignore everything else. This is what stops holding a direction from
     *     tearing through the whole list
     * 2 - Read which way the player is pushing, and step one along that way
     * 3 - Wrap round at either end, so going past the last technique comes back
     *     to the first and vice versa
     * 4 - If that actually landed on a different technique, show it and start
     *     the wait again
     */
    private void Update()
    {
        // 1
        if (t > 0)
        {
            t -= Time.deltaTime;
            return;
        }

        // 2
        int newIndex = currentIndex;
        float movement = InputReader.Instance.Navigate.x;
        if (movement > 0)
            newIndex++;
        else if (movement < 0)
            newIndex--;

        // 3
        if (newIndex > techniques.Length - 1)
            newIndex = 0;
        else if (newIndex < 0)
            newIndex = techniques.Length - 1;

        // 4
        if (newIndex != currentIndex)
        {
            currentIndex = newIndex;
            Show(currentIndex);
            t = 1;
        }
    }

    //----------Public Functions----------\\

    /* SHOW
     * 1 - Do nothing if asked for a technique that is not in the list
     * 2 - Fill in its name, its Japanese name and its description
     * 3 - Stitch its steps together into one block, one per line, and show that
     */
    public void Show(int index)
    {
        // 1
        if (index < 0 || index >= techniques.Length) return;

        // 2
        nameLabel.text = techniques[index].name;
        descriptionLabel.text = techniques[index].description;
        japLabel.text = techniques[index].jap;

        // 3
        string steps = "";
        foreach (string step in techniques[index].steps)
        {
            steps += step;
            steps += "\n";
        }
        stepsArea.text = steps;
    }
}
