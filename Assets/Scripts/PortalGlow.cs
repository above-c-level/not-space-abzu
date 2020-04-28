using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PortalGlow : MonoBehaviour
{
    private Astronaut player;
    // Start is called before the first frame update
    void Start()
    {
        player = FindObjectOfType<Astronaut>();
    }

    // Update is called once per frame
    void Update()
    {
        if (player.collectedStarPieces == 5)
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                transform.GetChild(i).gameObject.SetActive(true);
            }
        }
    }
    
}
