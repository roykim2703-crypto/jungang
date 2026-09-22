using UnityEngine;
using UnityEngine.InputSystem;

public class 테스트용 : MonoBehaviour
{
    수_놓기 수놓는거;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        수놓는거 = FindAnyObjectByType<수_놓기>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Keyboard.current.wKey.wasPressedThisFrame)
        {
            수놓는거.너백();
            수놓는거.이제너의턴();
        }
        
        if (Keyboard.current.bKey.wasPressedThisFrame)
        {
            수놓는거.너흑();
            수놓는거.이제너의턴();
        }
    }
}
