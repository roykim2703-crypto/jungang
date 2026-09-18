using UnityEngine;

public class SurrenderButton : MonoBehaviour
{
    [SerializeField] private GameObject surrenderPanel;

    public void OnSurrenderButtonClicked()
    {
        surrenderPanel.SetActive(true);
    }

    public void OnConfirmSurrender()
    {
        if (!surrenderPanel.TryGetComponent(out Surrender surrender))
        {
            Debug.LogError("Surrender component is missing from the surrender panel.", surrenderPanel);
            return;
        }

        surrender.ConfirmSurrender();
    }

    public void NoneButton()
    {
        surrenderPanel.SetActive(false);
    }
}
