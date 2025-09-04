using System.Collections.Generic;
using UnityEngine;

public class GuideManager : MonoBehaviour
{
    static GuideManager instance;
    public static GuideManager Instance
    {
        get { return instance; }
    }

    [SerializeField] List<GameObject> Guides;
    private int curGuideCount = 0;

    private void Awake()
    {
        instance = this;
        curGuideCount = 0;
    }

    public void ProgressGuide(int index)
    {
        if(index >= Guides.Count)
        {
            gameObject.SetActive(false);
        }
        if (curGuideCount + 1 == index)
        {
            Guides[curGuideCount].SetActive(false);
            curGuideCount++;
            Guides[curGuideCount].SetActive(true);
        }
    }

}
