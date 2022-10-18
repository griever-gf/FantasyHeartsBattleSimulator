using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HexCellController : MonoBehaviour
{
    Vector2Int hexDimensions;
    SpriteRenderer cellSpriteRenderer;
    public SpriteRenderer frameSpriteRenderer;
    public GameObject characterSprite;
    public GameObject prefabAttackIcon;
    public GameObject prefabDamageIcon;
    Vector3 charLowerBorderPosition;

    void Awake()
    {
        cellSpriteRenderer = GetComponent<SpriteRenderer>();
        charLowerBorderPosition = characterSprite.transform.localPosition;
    }

    public void SetDimensions(Vector2Int vec)
    {
        hexDimensions = vec;
    }

    public Vector2Int GetDimensions()
    {
        return hexDimensions;
    }

    public void SetCharacterSprite(Sprite spr)
    {
        SpriteRenderer spr_ren = characterSprite.GetComponent<SpriteRenderer>();
        spr_ren.sprite = spr;
        spr_ren.sortingLayerName = (hexDimensions.x+1).ToString();
        Destroy(characterSprite.GetComponent<PolygonCollider2D>());
        characterSprite.AddComponent<PolygonCollider2D>();

        spr_ren.transform.localPosition = charLowerBorderPosition + new Vector3(-0.02f, spr_ren.bounds.size.y / 2f, 0);
    }

    public void RemoveCharacterSprite()
    {
        characterSprite.GetComponent<SpriteRenderer>().sprite = null;
        Destroy(characterSprite.GetComponent<PolygonCollider2D>());
    }

    public void OnMouseDown()
    {
        transform.parent.GetComponent<BattlefieldView>().ClickOnCell(hexDimensions);
    }

    public void SpawnAttackIcon()
    {
        Instantiate(prefabAttackIcon, this.transform.position + new Vector3(0f, 1.1f,0), Quaternion.identity);
    }

    public void SpawnDamageIcon(int dmg, bool is_critical)
    {
        GameObject obj = Instantiate(prefabDamageIcon, this.transform.position + new Vector3(0f, 1.1f, 0f), Quaternion.identity) as GameObject;
        //obj.GetComponent<TextMesh>().text = dmg.ToString();

        if (!is_critical)
            obj.GetComponentInChildren<Text>().text = dmg.ToString();
        else
            obj.GetComponentInChildren<Text>().text = "Предельный! " + dmg.ToString();
    }

    #region Cell & Frame Colors
    void SetHexCellColor(Color clr)
    {
        cellSpriteRenderer.color = clr;
    }

    void SetHexFrameColor(Color clr)
    {
        frameSpriteRenderer.color = clr;
    }

    public void SetCellFrameActivePerson()
    {
        SetHexFrameColor(Color.red);
    }

    public void SetCellFrameActiveCell()
    {
        SetHexFrameColor(Color.yellow);
    }

    public void SetCellFrameClear()
    {
        SetHexFrameColor(Color.clear);
    }

    public void SetCellColorMoveable()
    {
        Color clr = Color.blue;
        clr.a = 80f / 255f;
        SetHexCellColor(clr);
    }

    public void SetCellColorAttackable()
    {
        Color clr = Color.red;
        clr.a = 80f / 255f;
        SetHexCellColor(clr);
    }

    public void SetCellColorSkillable()
    {
        Color clr = new Color32(0xFF, 0x66, 0, 80);
        SetHexCellColor(clr);
    }

    public void SetCellColorClear()
    {
        SetHexCellColor(new Color32(0xB0, 0xFC, 0x72, 180));
    }
    #endregion

    void Update()
    {
        
    }
}
