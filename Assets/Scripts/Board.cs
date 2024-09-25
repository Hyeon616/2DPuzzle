using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

public class Board : MonoBehaviour
{

    public int width;
    public int height;

    public GameObject bgTilePrefab;
    public Gem[] gems;

    public Gem[,] allGems;

    public float gemSpeed;

    public MatchFinder matchFinder;
    private bool isProcessing = false;

    private void Awake()
    {
        matchFinder = FindObjectOfType<MatchFinder>();
    }

    private async void Start()
    {
        allGems = new Gem[width, height];
        Setup();
        await GameLoop();
    }

    private void Setup()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2 pos = new Vector2(x, y);
                GameObject bgTile = Instantiate(bgTilePrefab, pos, Quaternion.identity, transform);
                bgTile.name = $"BG Tile {x}, {y}";

                SpawnGem(new Vector2Int(x, y), gems[Random.Range(0, gems.Length)]);
            }
        }
    }

    private void SpawnGem(Vector2Int pos, Gem gemToSpawn)
    {
        Gem gem = Instantiate(gemToSpawn, new Vector3(pos.x, pos.y + 1, 0), Quaternion.identity, transform);
        gem.name = $"Gem {pos.x}, {pos.y}";
        allGems[pos.x, pos.y] = gem;
        gem.SetupGem(pos, this);
    }

    private async Task GameLoop()
    {
        while (true)
        {
            if (!isProcessing)
            {
                isProcessing = true;
                await CheckMatches();
                isProcessing = false;
            }
            await Task.Yield();
        }
    }

    private async Task CheckMatches()
    {
        matchFinder.FindAllMatches();
        if (matchFinder.currentMatches.Count > 0)
        {
            await DestroyMatches();
            await DecreaseRows();
            await RefillBoard();
        }
    }

    public async Task DestroyMatches()
    {
        // 매치된 gem들을 표시
        foreach (Gem gem in matchFinder.currentMatches)
        {
            if (gem != null)
            {
                // gem.GetComponent<SpriteRenderer>().color = Color.white;
            }
        }

        // 0.3초 대기
        await Task.Delay(500);

        // 매치된 gem들을 제거
        foreach (Gem gem in matchFinder.currentMatches)
        {
            if (gem != null)
            {
                allGems[gem.posIndex.x, gem.posIndex.y] = null;
                Destroy(gem.gameObject);
            }
        }
        matchFinder.currentMatches.Clear();
        await Task.Yield();
    }
    private async Task DecreaseRows()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (allGems[x, y] == null)
                {
                    for (int yAbove = y + 1; yAbove < height; yAbove++)
                    {
                        if (allGems[x, yAbove] != null)
                        {
                            allGems[x, yAbove].posIndex.y = y;
                            allGems[x, y] = allGems[x, yAbove];
                            allGems[x, yAbove] = null;
                            break;
                        }
                    }
                }
            }
        }
        await MoveGems();
    }

    private async Task MoveGems()
    {
        bool isMoving;
        do
        {
            isMoving = false;
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (allGems[x, y] != null)
                    {
                        Vector3 targetPosition = new Vector3(x, y, 0);
                        if (allGems[x, y].transform.position != targetPosition)
                        {
                            allGems[x, y].transform.position = Vector3.Lerp(
                                allGems[x, y].transform.position,
                                targetPosition,
                                gemSpeed * Time.deltaTime
                            );
                            if ((allGems[x, y].transform.position - targetPosition).sqrMagnitude > 0.01f)
                            {
                                isMoving = true;
                            }
                        }
                    }
                }
            }
            await Task.Yield();
        } while (isMoving);
    }

    private async Task RefillBoard()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (allGems[x, y] == null)
                {
                    SpawnGem(new Vector2Int(x, y), gems[Random.Range(0, gems.Length)]);
                }
            }
        }
        await MoveGems();
    }
}
