using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NameBanner : MonoBehaviour
{
    public TextMeshProUGUI snakeNameText;
    public GameObject owner;

    // Start is called before the first frame update
    void Start()
    {
       snakeNameText.text = owner.name;

    }

    // Update is called once per frame
    void Update()
    {
        transform.eulerAngles = new Vector3(0.0f, 0.0f, 0.0f);
        
    }
}
