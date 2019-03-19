using System.Collections;
using System.Collections.Generic;
using UnityEditor.TreeViewExamples;
using UnityEngine;
using UnityEngine.UI;

public class SpriteReferenceTreeElement : TreeElement 
{
    public GameObject Go;
    public string GameObjectName;
    public string SpriteName;
    public Texture2D SpriteTexture;
    public string Path;
    public Sprite Sprite;
    public Image ImageCom;
    public bool breakSpriteReference = true;
    
    
    public SpriteReferenceTreeElement(GameObject go,Image image,Sprite sprite,string path, string name,int depth,int id):base(name,depth,id)
    {
        if(go != null)
        {
            this.Go = go;
            GameObjectName = go.name;
            if(path.StartsWith(go.name + "/"))
            {
                path = path.Replace(go.name + "/","");
            }
            else if(path.StartsWith(go.name))
            {
                path = path.Replace(go.name,"");
            }
        }

        ImageCom = image;
        Sprite = sprite;
        if (sprite != null)
        {
           
            SpriteTexture = sprite.texture;
            SpriteName = sprite.name;
        }
        Path = path;
    }
}
