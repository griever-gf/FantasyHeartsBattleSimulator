using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class PersonSpawnerController : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IEndDragHandler, IDragHandler
{
    public Image characterPortrait;
    public Text characterText;

    string characterName;

    [SerializeField] private Canvas canvas;
    private RectTransform rectTransform;

    GameObject dragIcon;
    BattlefieldView bfView;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    public void SetBFView(BattlefieldView bfv)
    {
        bfView = bfv;
    }

    public void LoadPersonView(SimuData.CharData char_data)
    {
        StartCoroutine(BattlefieldView.LoadSpriteFromFileAndUpdateUIImage(char_data.sprite_front, characterPortrait));
        characterPortrait.preserveAspect = true;
        characterPortrait.transform.localScale = new Vector3(-1,1,1); //mirror character portrait
        LoadPersonText(char_data);
        characterName = char_data.name;
    }

    public void LoadPersonText(SimuData.CharData char_data)
    {
        characterText.text = char_data.name + "\nКласс: " + char_data.class_of_char + "\nЗдоровье: " + char_data.health;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        //Debug.Log("OnBeginDrag");
        dragIcon = new GameObject("dragIcon");
        dragIcon.transform.position = GetMousePosForDragIcon(canvas.transform);

        RectTransform trans = dragIcon.AddComponent<RectTransform>();
        trans.localScale = Vector3.one;
        trans.transform.SetParent(canvas.transform, false); // setting parent
        trans.sizeDelta = new Vector2(75, 75); // custom size

        Image image = dragIcon.AddComponent<Image>();
        image.sprite = characterPortrait.sprite;
        image.preserveAspect = true;
        image.color = new Color(image.color.r, image.color.g, image.color.b, 0.7f);

        dragIcon.transform.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData)
    {
        //Debug.Log("OnDrag");
        dragIcon.transform.position = GetMousePosForDragIcon(canvas.transform);
    }

    Vector3 GetMousePosForDragIcon(Transform parent_trans)
    {
        //Debug.Log(Input.mousePosition);
        Vector3 mousepos = Camera.allCameras[1].ScreenToWorldPoint(Input.mousePosition); //allCameras[1] is UI camera
        mousepos.z = parent_trans.position.z;
        return mousepos;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        float icon_height = dragIcon.GetComponent<Image>().sprite.bounds.size.y;
        //Debug.Log("icon_height "+ icon_height);
        float lossyScale = dragIcon.GetComponent<RectTransform>().lossyScale.y;
        //Debug.Log(dragIcon.GetComponent<RectTransform>().lossyScale);
        Destroy(dragIcon);
        Vector3 mousepos = Camera.main.ScreenToWorldPoint(Input.mousePosition); //allCameras[1] is UI camera
        //Debug.Log("mousepos before" + mousepos);
        mousepos.z = 10;
        mousepos.y -= icon_height * lossyScale * 0.5f; //нацеливаемся ближе к ногам спрайта
        //Debug.Log("mousepos after" + mousepos);
        RaycastHit2D hit = Physics2D.Raycast(mousepos, Vector2.zero);
        if (hit)
        {
            Debug.Log("Hit! " + hit.collider.name);
            HexCellController hcc = hit.transform.gameObject.GetComponentInParent<HexCellController>();
            if (hcc != null)
            {
                bfView.PlacePersonIfCellIsFree(hcc.GetDimensions(), characterName);
            }
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        //Debug.Log("OnPointerDown");
        canvas = transform.parent.GetComponentInParent<Canvas>().rootCanvas;
    }
}
