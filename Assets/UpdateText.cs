using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UpdateText : MonoBehaviour
{
    public TMP_Text textObj;

    public void ChangeText()
    {
        textObj.text = ((int)GetComponent<Slider>().value).ToString()+"%";
    }
}
