using UnityEngine.UI;
using UnityEngine;

public class TextAssign : MonoBehaviour
{
    public void Assign(float number)=> GetComponent<Text>().text = number.ToString("0");
}
