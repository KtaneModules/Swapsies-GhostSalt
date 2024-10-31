using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;

public class SwapsiesScript : MonoBehaviour
{
    static int _moduleIdCounter = 1;
    int _moduleID = 0;

    public KMNeedyModule Module;
    public KMBombInfo Bomb;
    public KMAudio Audio;
    public KMSelectable[] Tiles;
    public MeshRenderer[] TileRends;
    public Material[] TileMats;
    public Sprite[] TileSprites;
    public SpriteRenderer[] TileSpriteRends;
    public SpriteRenderer[] BaseRends;
    public SpriteRenderer[] BaseSpriteRends;

    private int GapPos = 8;
    private int DiamondPos;
    private int HashPos = 1;
    private int DiamondGoalPos = 1;
    private int HashGoalPos;

    private bool IsAnimating, IsActive;

    private bool IsMoveValid(int pos, int rows = 3, int cols = 3)
    {
        var posX = pos % 3;
        var posY = pos / 3;

        var gapX = GapPos % 3;
        var gapY = GapPos / 3;

        return (Mathf.Abs(posX - gapX) == 1 && posY == gapY) || (Mathf.Abs(posY - gapY) == 1 && posX == gapX);
    }

    private Vector3 GetTileLocation(int pos, int rows = 3, int cols = 3, float tileGap = 0.3f)
    {
        return new Vector3(tileGap * ((pos % cols) - 1), 0, tileGap * (rows - (pos / rows) - 2));
    }

    void Awake()
    {
        _moduleID = _moduleIdCounter++;

        for (int i = 0; i < Tiles.Length; i++)
        {
            int x = i;
            Tiles[x].OnInteract += delegate { if (!IsAnimating) TilePress(x); return false; };

            TileRends[x].material = TileMats[0];
            TileSpriteRends[x].sprite = null;
        }

        for (int i = 0; i < BaseRends.Length; i++)
        {
            BaseRends[i].color = Color.black;
            BaseSpriteRends[i].sprite = null;
        }

        Module.OnNeedyActivation += delegate { StartCoroutine(Activate()); };
        Module.OnTimerExpired += delegate { Strike(); };
    }

    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    void ArrangeTiles(int gap, float tileGap = 0.3f)
    {
        for (int i = 0; i < Tiles.Length + 1; i++)
        {
            if (i == gap)
                continue;
            if (i < gap)
                Tiles[i].transform.localPosition = GetTileLocation(i);
            else
                Tiles[i - 1].transform.localPosition = GetTileLocation(i);
        }
        GapPos = gap;

        for (int i = 0; i < Tiles.Length; i++)
        {
            TileRends[i].material = TileMats[0];
            TileSpriteRends[i].sprite = null;
        }

        if (IsActive)
        {
            TileRends[DiamondPos > gap ? DiamondPos - 1 : DiamondPos].material = TileMats[1];
            TileSpriteRends[DiamondPos > gap ? DiamondPos - 1 : DiamondPos].sprite = TileSprites[0];

            TileRends[HashPos > gap ? HashPos - 1 : HashPos].material = TileMats[2];
            TileSpriteRends[HashPos > gap ? HashPos - 1 : HashPos].sprite = TileSprites[1];
        }
    }

    private IEnumerator Activate()
    {
        Audio.PlaySoundAtTransform("activate", transform);
        IsActive = true;

        yield return new WaitWhile(() => IsAnimating);

        var choices = Enumerable.Range(0, 9).Where(x => x != GapPos).ToArray().Shuffle().Take(2).ToArray();
        DiamondPos = HashGoalPos = choices[0];
        HashPos = DiamondGoalPos = choices[1];

        for (int i = 0; i < BaseRends.Length; i++)
        {
            BaseRends[i].color = Color.black;
            BaseSpriteRends[i].sprite = null;
        }

        BaseRends[DiamondGoalPos].color = TileMats[1].color;
        BaseSpriteRends[DiamondGoalPos].sprite = TileSprites[0];

        BaseRends[HashGoalPos].color = TileMats[2].color;
        BaseSpriteRends[HashGoalPos].sprite = TileSprites[1];

        ArrangeTiles(GapPos);
    }

    void CheckSolve()
    {
        if (DiamondPos == DiamondGoalPos && HashPos == HashGoalPos)
        {
            Module.HandlePass();
            IsActive = false;
            Audio.PlaySoundAtTransform("correct", transform);
            ArrangeTiles(GapPos);
            for (int i = 0; i < BaseRends.Length; i++)
            {
                BaseRends[i].color = Color.black;
                BaseSpriteRends[i].sprite = null;
            }
        }
    }

    void Strike()
    {
        Module.HandleStrike();
        IsActive = false;
        Audio.PlaySoundAtTransform("strike", transform);
        ArrangeTiles(GapPos);
        for (int i = 0; i < BaseRends.Length; i++)
        {
            BaseRends[i].color = Color.black;
            BaseSpriteRends[i].sprite = null;
        }
    }

    void TilePress(int pos)
    {
        var slotPos = pos;
        if (slotPos >= GapPos)
            slotPos++;

        Tiles[pos].AddInteractionPunch();

        if (IsMoveValid(slotPos))
        {
            Audio.PlaySoundAtTransform("tile", Tiles[pos].transform);

            if (slotPos == DiamondPos)
                DiamondPos = GapPos;
            else if (slotPos == HashPos)
                HashPos = GapPos;

            StartCoroutine(MoveTile(pos, slotPos, GetTileLocation(slotPos), GetTileLocation(GapPos)));
        }
        else
            Audio.PlaySoundAtTransform("buzzer", Tiles[pos].transform);
    }

    private IEnumerator MoveTile(int pos, int slotPos, Vector3 start, Vector3 end, float duration = 0.1f)
    {
        IsAnimating = true;
        Tiles[pos].transform.localPosition = start;

        float timer = 0;
        while (timer < duration)
        {
            Tiles[pos].transform.localPosition = Vector3.Lerp(start, end, timer / duration);
            yield return null;
            timer += Time.deltaTime;
        }

        Tiles[pos].transform.localPosition = end;

        ArrangeTiles(slotPos);
        if (IsActive)
            CheckSolve();
        IsAnimating = false;
    }

#pragma warning disable 414
    private string TwitchHelpMessage = "Use '!{0} 123' to press the three buttons on the top row (buttons are 1-9 in reading order).";
#pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string command)
    {
        var validCommands = "123456789";
        if (!IsActive)
        {
            yield return "sendtochaterror I'm not active right now!";
            yield break;
        }

        for (int i = 0; i < command.Length; i++)
        {
            if (!validCommands.Contains(command[i]))
            {
                yield return "sendtochaterror Invalid command.";
                yield break;
            }
        }

        yield return null;
        for (int i = 0; i < command.Length; i++)
        {
            yield return new WaitUntil(() => !IsAnimating);
            if (!IsActive)
                yield break;
            var ix = int.Parse(command[i].ToString()) - 1;
            if (ix > GapPos)
                ix--;
            Tiles[ix].OnInteract();
        }
    }
}
