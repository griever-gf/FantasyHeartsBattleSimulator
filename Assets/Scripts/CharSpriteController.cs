using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharSpriteController : MonoBehaviour
{
    void OnMouseDown()
    {
        transform.parent.GetComponent<HexCellController>().OnMouseDown();
    }
}
