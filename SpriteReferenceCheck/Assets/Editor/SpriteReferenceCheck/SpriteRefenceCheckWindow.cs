using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditor.TreeViewExamples;
using UnityEngine;
using UnityEngine.UI;

public class SpriteRefenceCheckWindow : EditorWindow 
{
    private static SpriteRefenceCheckWindow window;
    
    private bool initialized = false;
    TreeViewState m_TreeViewState;
    MultiColumnHeaderState m_MultiColumnHeaderState;
    SpriteReferenceTreeView m_TreeView;
    public SpriteReferenceTreeView treeView
    {
        get { return m_TreeView; }
    }
    Rect replaceUISpriteTreeViewRect
    {
        get { return new Rect(20, 30, window.position.width - 40, window.position.height - 50); }
    }
    
    private static List<string> prefabPathList = new List<string> ();
    
    [MenuItem("Tools/UISprite References Check")]
    public static SpriteRefenceCheckWindow ShowWindow()
    {
        prefabPathList.Clear ();
        GetFiles (new DirectoryInfo (Application.dataPath + "/Resources"), "*.prefab", ref prefabPathList);
        if (window == null)
            window = EditorWindow.GetWindow(typeof(SpriteRefenceCheckWindow)) as SpriteRefenceCheckWindow;
        window.titleContent = new GUIContent("UISpriteReferences");
        window.Show();
        return window;
    }
    
    public static void GetFiles (DirectoryInfo directory, string pattern, ref List<string> fileList) 
    {
        if (directory != null && directory.Exists && !string.IsNullOrEmpty (pattern)) {
            try {
                foreach (FileInfo info in directory.GetFiles (pattern)) {
                    string path = info.FullName.ToString ();
                    fileList.Add (path.Substring (path.IndexOf ("Assets")));
                }
            } catch (System.Exception) 
            {
                throw;
            }
            foreach (DirectoryInfo info in directory.GetDirectories ()) 
            {
                GetFiles (info, pattern, ref fileList);
            }
        }
    }
    
    void OnGUI()
    {
        InitIfNeeded();
        DrawToolbar();
        DrawCheckList();
    }
    
    private void InitIfNeeded()
    {
        if(!initialized)
        {
            if (m_TreeViewState == null)
                m_TreeViewState = new TreeViewState();

            bool firstInit = m_MultiColumnHeaderState == null;
            var headerState = SpriteReferenceTreeView.CreateDefaultMultiColumnHeaderState(replaceUISpriteTreeViewRect.width);

            if (MultiColumnHeaderState.CanOverwriteSerializedFields(m_MultiColumnHeaderState, headerState))
                MultiColumnHeaderState.OverwriteSerializedFields(m_MultiColumnHeaderState, headerState);
            m_MultiColumnHeaderState = headerState;
            var multiColumnHeader = new MultiColumnHeader(headerState);
            if (firstInit)
                multiColumnHeader.ResizeToFit();
            TreeModel<SpriteReferenceTreeElement> treeModel = new TreeModel<SpriteReferenceTreeElement>(GetData());

            m_TreeView = new SpriteReferenceTreeView(m_TreeViewState, multiColumnHeader, treeModel);
        }
        initialized = true;
    }

    private string[] GetAtlasNames()
    {
        string[] folders = Directory.GetDirectories(Application.dataPath + "/Resources/Atlas/");
        for (int i = 0; i < folders.Length; i++)
        {
            int index = folders[i].LastIndexOf("/");

            folders[i] = folders[i].Substring(index + 1);
        }
        return folders;
    }

    private string searchAtlaName = "";
    private string searchSpriteName = "";
    
    private void DrawToolbar()
    {
        GUILayout.BeginHorizontal();
        
        if (GUILayout.Button(string.IsNullOrEmpty(searchAtlaName) ? "Select Atlas" : searchAtlaName, "DropDown", GUILayout.Width(120f)))
        {
            GenericMenu menu = new GenericMenu();
            string[] atlasName = GetAtlasNames();
            for (int i = 0; i < atlasName.Length; i++)
            {
                menu.AddItem(new GUIContent(atlasName[i]), false, OnAtlasSelectButtonClick, atlasName[i]);
            }

            menu.ShowAsContext();
        }

        searchSpriteName = EditorGUILayout.TextField("精灵：", searchSpriteName, GUILayout.Width(250f));

        if(GUILayout.Button("搜索"))
        {
            if(!string.IsNullOrEmpty(searchAtlaName) && !string.IsNullOrEmpty(searchSpriteName))
            {
                CheckUISprite();
            }
            else
            {
                EditorUtility.DisplayDialog("提示", "请先选择图集和精灵!", "确定");
            }
        }

        if (GUILayout.Button("导出搜索数据"))
        {
            if (m_TreeView.treeModel == null || m_TreeView.treeModel.numberOfDataElements <= 1)
            {
                EditorUtility.DisplayDialog("提示", "数据为空!", "确定");
            }
            else
            {
                WirteReferenceSpriteDataToFile();
            }
        }

        if (GUILayout.Button("自动断开所有引用"))
        {
            if (m_TreeView.treeModel == null || m_TreeView.treeModel.numberOfDataElements <= 1)
            {
                EditorUtility.DisplayDialog("提示", "数据为空!", "确定");
            }
            else
            {
                BreakSpriteReferenceSavePrefab();
            }
        }
        GUILayout.EndHorizontal();
    }

    private void OnAtlasSelectButtonClick(object userdata)
    {
        searchAtlaName = userdata.ToString();
    }

    /// <summary>
    /// 断开图片引用并保存预制体
    /// </summary>
    private void BreakSpriteReferenceSavePrefab()
    {
        if (m_TreeView == null || m_TreeView.treeModel == null) return;
        IList<SpriteReferenceTreeElement> datas = m_TreeView.treeModel.GetData();
        if (datas != null && datas.Count > 0)
        {
            for (int i = 0; i < datas.Count; i++)
            {
                SpriteReferenceTreeElement element = datas[i];
                if (element.breakSpriteReference == false) continue; 
                GameObject prefab = element.Go;
                if (prefab != null)
                {
                    GameObject prefabGameobject = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                    Transform imageChild = prefabGameobject.transform.Find(element.Path);
                    bool hasBreakReference = false;
                    if (imageChild != null)
                    {
                        Image childImage = imageChild.GetComponent<Image>();
                        if (childImage != null)
                        {
                            childImage.sprite = null;
                            hasBreakReference = true;
                        }
                    }
                    //5、保存预制体
                    if (hasBreakReference)
                    {
                        PrefabUtility.ReplacePrefab(prefabGameobject, prefab, ReplacePrefabOptions.Default);
                    }
                    DestroyImmediate(prefabGameobject);
                    AssetDatabase.SaveAssets();
                }
            }
            
            AssetDatabase.Refresh();
        }
    }
    
    private void WirteReferenceSpriteDataToFile()
    {
        if (m_TreeView == null || m_TreeView.treeModel == null) return;
        IList<SpriteReferenceTreeElement> datas = m_TreeView.treeModel.GetData();
        if (datas != null && datas.Count > 0)
        {
            string path = EditorPrefs.GetString("create_sprite_reference_model_folder", "");
            string fileName = searchSpriteName + "_reference_model.csv";
            path = EditorUtility.SaveFilePanel("Create Sprite Reference File ", path, fileName, "csv");
            
            string dataModelStr = "";

            //Header
            dataModelStr += "GameObjectName,ChildPath,SpriteName";
            
            for (int i = 0; i < datas.Count; i++)
            {
                SpriteReferenceTreeElement element = datas[i];
                dataModelStr += element.GameObjectName + ",";
                dataModelStr += element.Path + ",";
                dataModelStr += element.SpriteName + "\n";
            }

            if (string.IsNullOrEmpty(dataModelStr) || string.IsNullOrEmpty(dataModelStr))
            {
                return;
            }
            
            File.WriteAllText(path, dataModelStr, new UTF8Encoding(false));
            AssetDatabase.Refresh();
            EditorPrefs.SetString("create_sprite_reference_model_folder", path);
        }
    }

    private void DrawCheckList()
    {
        m_TreeView.OnGUI(replaceUISpriteTreeViewRect);
    }
    
    IList<SpriteReferenceTreeElement> GetData()
    {
        List<SpriteReferenceTreeElement> replaceSpriteTreeElementList = new List<SpriteReferenceTreeElement>();
        SpriteReferenceTreeElement root = new SpriteReferenceTreeElement(null,null,null,"","Root", -1, 0);
        replaceSpriteTreeElementList.Add(root);
        return replaceSpriteTreeElementList;
    }
    
    public void CheckUISprite()
    {
        List<SpriteReferenceTreeElement> replaceSpriteTreeElementList = new List<SpriteReferenceTreeElement>();
        SpriteReferenceTreeElement root = new SpriteReferenceTreeElement(null,null,null,"","Root", -1, 0);
        replaceSpriteTreeElementList.Add(root);
        
        //获取CheckSprite
        string path = string.Format("Atlas/{0}/{1}",searchAtlaName,searchAtlaName.Replace("Atlas",""));
        Sprite checkSprite = GetSprite(path, searchSpriteName);
        if (checkSprite == null) return;
        for (int i = 0; i < prefabPathList.Count; i++)
        {
            ShowProgress(i,prefabPathList.Count);
            if(string.IsNullOrEmpty(prefabPathList[i])) continue;
            GameObject gameObj = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPathList[i]);
            CheckUISpriteInPrefab(gameObj,checkSprite,ref replaceSpriteTreeElementList);
        }
        m_TreeView.treeModel.SetData(replaceSpriteTreeElementList);
        m_TreeView.Reload();
        EditorUtility.ClearProgressBar();
    }

    private Sprite GetSprite(string atlaPath,string spriteName)
    {
        Object[] sprites = UnityEngine.Resources.LoadAll(atlaPath);
        for (int i = 0; i < sprites.Length; i++)
        {
            if (sprites[i] is Sprite)
            {
                if (sprites[i].name == spriteName)
                {
                    return (Sprite) sprites[i];
                }
            }
        }

        return null;
    }
    
    public static void CheckUISpriteInPrefab(GameObject prefab,Sprite sprite,ref List<SpriteReferenceTreeElement> result)
    {
        if(prefab == null || sprite == null) return;
        Image[] images = prefab.GetComponentsInChildren<Image>(true);
        List<string> childPath = new List<string>();
        if(images != null && images.Length > 0)
        {
            for (int i = 0; i < images.Length; i++)
            {
                if(images[i].sprite == sprite)
                {
                    SpriteReferenceTreeElement element = new SpriteReferenceTreeElement(prefab,images[i],sprite,GetPath(prefab,images[i].gameObject),images[i].gameObject.name, 0, result.Count + 1);
                    result.Add(element);
                }
            }
        }
    }
    
    private static string GetPath(GameObject root,GameObject child)
    {
        if(root == null || child == null) return "";
        List<string> parentList = new List<string>();
        parentList.Add(child.name);
        while(child.transform.parent != null && child.transform.parent != root)
        {
            child = child.transform.parent.gameObject;
            parentList.Add(child.name);
        }
        string path = "";
        for (int i = parentList.Count - 1; i >= 0; i--)
        {
            path += parentList[i];
            if(i != 0)
            {
                path += "/";
            }
        }
        return path;
    }
    
    static public void ShowProgress(int curIndex,int num)
    {
        EditorUtility.DisplayProgressBar("批量检测中...", string.Format("Please wait...  {0}/{1}", curIndex,num), curIndex * 1.0f / num);
    }
}