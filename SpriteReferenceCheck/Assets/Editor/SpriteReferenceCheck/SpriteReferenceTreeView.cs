using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditor.SceneManagement;
using UnityEditor.TreeViewExamples;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Assertions;

public class SpriteReferenceTreeView : TreeViewWithTreeModel<SpriteReferenceTreeElement>  {

	enum MyColumns
    {
		GameObjectName,
//        SpriteTexture,
        ChildPath,
        BreakSpriteReference,
		Preview,
    }

	public static void TreeToList(TreeViewItem root, IList<TreeViewItem> result)
    {
        if (root == null)
            return;
        if (result == null)
            return;

        result.Clear();

        if (root.children == null)
            return;

        Stack<TreeViewItem> stack = new Stack<TreeViewItem>();
        for (int i = root.children.Count - 1; i >= 0; i--)
            stack.Push(root.children[i]);

        while (stack.Count > 0)
        {
            TreeViewItem current = stack.Pop();
            result.Add(current);

            if (current.hasChildren && current.children[0] != null)
            {
                for (int i = current.children.Count - 1; i >= 0; i--)
                {
                    stack.Push(current.children[i]);
                }
            }
        }
    }

	public SpriteReferenceTreeView(TreeViewState state, MultiColumnHeader multicolumnHeader, TreeModel<SpriteReferenceTreeElement> model) : base(state, multicolumnHeader, model)
    {
		rowHeight = 20;
		columnIndexForTreeFoldouts = 2;
		showAlternatingRowBackgrounds = true;
		showBorder = true;
		customFoldoutYOffset = (rowHeight - EditorGUIUtility.singleLineHeight) * 0.5f;
		extraSpaceBeforeIconAndLabel = 18f;
		Reload();
    }

	protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
    {
        var rows = base.BuildRows(root);
        SortIfNeeded(root, rows);
        return rows;
    }

    void SortIfNeeded(TreeViewItem root, IList<TreeViewItem> rows)
    {
        if (rows.Count <= 1)
            return;

        if (multiColumnHeader.sortedColumnIndex == -1)
        {
            return; // No column to sort for (just use the order the data are in)
        }

        // Sort the roots of the existing tree items
        //SortByMultipleColumns();
        TreeToList(root, rows);
        Repaint();
    }

    void SortByMultipleColumns()
    {
        
    }

    protected override void RowGUI(RowGUIArgs args)
    {
        var item = (TreeViewItem<SpriteReferenceTreeElement>)args.item;

        for (int i = 0; i < args.GetNumVisibleColumns(); ++i)
        {
            CellGUI(args.GetCellRect(i), item, (MyColumns)args.GetColumn(i), ref args);
        }
    }

    void CellGUI(Rect cellRect, TreeViewItem<SpriteReferenceTreeElement> item, MyColumns column, ref RowGUIArgs args)
    {
        // Center cell rect vertically (makes it easier to place controls, icons etc in the cells)
        CenterRectUsingSingleLineHeight(ref cellRect);
        GUIStyle style = new GUIStyle();
        style.alignment = TextAnchor.MiddleCenter;
        style.normal.textColor = Color.black;
        switch (column)
        {
            case MyColumns.GameObjectName:
				EditorGUI.LabelField(cellRect,item.data.GameObjectName,style);
                break;
//            case MyColumns.SpriteTexture:
//                NGUIEditorTools.DrawSprite(item.data.SpriteTexture, cellRect, item.data.SpriteData, Color.white,false);
//                EditorGUI.DrawTextureTransparent(textureRect, AssetPreview.GetAssetPreview(sprites[i]));
//                break;
            case MyColumns.ChildPath:
                EditorGUI.LabelField(cellRect,item.data.Path,style);
                break;
            case MyColumns.BreakSpriteReference:
                item.data.breakSpriteReference =
                    EditorGUI.ToggleLeft(cellRect, "是否断开引用", item.data.breakSpriteReference);
                break;
            case MyColumns.Preview:
                Rect rect = new Rect(cellRect.x + cellRect.width / 2 - 50,cellRect.y,100,cellRect.height);
                if(GUI.Button(rect,"查看"))
                {
                    GameObject ui = PrefabUtility.InstantiatePrefab(item.data.Go) as GameObject;
                    ui.transform.SetParent(GameObject.Find("Canvas").transform);
                    ui.transform.localScale = Vector3.one;
                    ui.transform.rotation = Quaternion.identity;
                    Selection.activeGameObject = ui.transform.Find(item.data.Path).gameObject;
                    EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                }
                break;
        }
    }

    public static MultiColumnHeaderState CreateDefaultMultiColumnHeaderState(float treeViewWidth)
    {
        int headNum = 4;
        float _width = treeViewWidth / headNum;
        var columns = new[]
        {
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("GameObject Name","This is GameObject Name"),
                    headerTextAlignment = TextAlignment.Center,
                    sortedAscending = false,
                    sortingArrowAlignment = TextAlignment.Center,
                    width = _width,
                    minWidth = 60,
                    autoResize = false,
                    allowToggleVisibility = false
                },
//                new MultiColumnHeaderState.Column
//                {
//                    headerContent = new GUIContent("Sprite Texture",""),
//                    headerTextAlignment = TextAlignment.Center,
//                    sortedAscending = false,
//                    sortingArrowAlignment = TextAlignment.Center,
//                    width = _width,
//                    minWidth = 60,
//                    autoResize = false,
//                    allowToggleVisibility = false
//                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Child Path",""),
                    headerTextAlignment = TextAlignment.Center,
                    sortedAscending = false,
                    sortingArrowAlignment = TextAlignment.Center,
                    width = _width,
                    minWidth = 60,
                    autoResize = false,
                    allowToggleVisibility = true
                },
            new MultiColumnHeaderState.Column
            {
                headerContent = new GUIContent("是否断开引用",""),
                headerTextAlignment = TextAlignment.Center,
                sortedAscending = false,
                sortingArrowAlignment = TextAlignment.Center,
                width = _width,
                minWidth = 60,
                autoResize = false,
                allowToggleVisibility = true
            },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("查看",""),
                    headerTextAlignment = TextAlignment.Center,
                    sortedAscending = false,
                    sortingArrowAlignment = TextAlignment.Center,
                    width = _width,
                    minWidth = 60,
                    autoResize = false,
                    allowToggleVisibility = false
                }
            };

        Assert.AreEqual(columns.Length, Enum.GetValues(typeof(MyColumns)).Length, "Number of columns should match number of enum values: You probably forgot to update one of them.");

        var state = new MultiColumnHeaderState(columns);
        return state;
    }
}
