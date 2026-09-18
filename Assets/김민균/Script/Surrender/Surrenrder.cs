using UnityEngine;

public class Surrender : MonoBehaviour
{
    [SerializeField] private GameObject surrenderUI;
    public bool surrendered = false;

    private void Awake()
    {
        surrenderUI.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            surrenderUI.SetActive(false);
        }
    }
    public void ConfirmSurrender()
    {
        surrendered = true;
        surrenderUI.SetActive(false);
    }
}
