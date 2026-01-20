using UnityEngine;


public enum MultiChanelSpeakerLayout
{
    _5_1_2CH,

}
public class MultiChanelSpeakerSetter : MonoBehaviour
{
    MultiChanelSpeakerLayout layout;

    string LayoutPath
    {
        get
        {
            string path;
            switch (layout)
            {
                case MultiChanelSpeakerLayout._5_1_2CH:
                    path = "5.1.2";
                    break;
                default:
                    path = null;
                    break;
            }

            return path;
        }
    }
    
    AudioSource[] sources;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetSpeakers(string audioName)
    {
        AudioClip[] audios = Resources.LoadAll<AudioClip>($"Incheon/{LayoutPath}/{audioName}");

        Transform t = transform.GetChild((int)layout);

        sources = t.GetComponentsInChildren<AudioSource>();

        for(int i = 0; i < audios.Length; i++)
        {
            sources[i].clip = audios[i];
            sources[i].Play();
        }
    }

    public void ResetSpeakers()
    {
        Transform t = transform.GetChild((int)layout);

        foreach (var s in sources)
        {
            s.clip = null;
            s.Stop();
        }

        
    }

}
