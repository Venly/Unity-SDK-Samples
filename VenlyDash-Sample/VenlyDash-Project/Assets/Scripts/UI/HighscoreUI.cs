using UnityEngine;
using UnityEngine.UI;

public class HighscoreUI : MonoBehaviour
{
	public Text number;
	public Text playerName;
	public InputField inputName;
	public Text score;
    public Color highlightColor;

    private Color _origColor = Color.white;
    private bool _isHighlighted;

    public void SetHighlight(bool active)
    {
        if (active == _isHighlighted) return;

        if (active)
        {
            _origColor = number.color;
        }

        number.color = active ? highlightColor : _origColor;
        playerName.color = active ? highlightColor : _origColor;
        score.color = active ? highlightColor : _origColor;
        _isHighlighted = active;
    }
}
