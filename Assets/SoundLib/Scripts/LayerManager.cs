using System.Collections.Generic;
using UnityEngine;

public class LayerManager : MonoBehaviour
{
    public List<GameObject> Layers;
    private int curIndex = 0;

    public static int CurIndex;

    private void Start()
    {
        foreach (var l in Layers)
            l.SetActive(false);
        Layers[0].SetActive(true);
    }

    public static void SetCurIndex(int index)
    {
        CurIndex = index;
        Debug.Log($"Current Index : {CurIndex} ");
    }

    public void OnChangeLayer(bool isNext)
    {
        Layers[curIndex].SetActive(false);

        curIndex += isNext ? 1 : -1;

        if (curIndex >= Layers.Count)
            curIndex = 0;
        if (curIndex < 0)
            curIndex = Layers.Count - 1;

        Layers[curIndex].SetActive(true);

        SetCurIndex(curIndex);
    }
}
