using UnityEngine;
using UnityEngine.UI;

public class SurrenderButton : MonoBehaviour
{
    [SerializeField] private GameObject surrenderPanel;

    private void Awake()
    {
        Button button=GetComponent<Button>();
        if(button==null)return;
        button.onClick.RemoveAllListeners();
        if(name=="None")button.onClick.AddListener(NoneButton);
        else if(GetComponentInParent<Surrender>(true)!=null)button.onClick.AddListener(OnConfirmSurrender);
        else button.onClick.AddListener(OnSurrenderButtonClicked);
    }

    public void OnSurrenderButtonClicked()
    {
        if(AugmentMatch.Instance==null||AugmentMatch.Instance.Board.winner!=0)return;
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
