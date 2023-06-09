using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System.Threading;
using System;
using UnityEngine.UI;

public class BallController : MonoBehaviour
{
    private Rigidbody2D ballRB;
    private Collider2D ballCollider;
    [SerializeField]
    private BallData_SO data;
    [HideInInspector]
    public int count;
    [SerializeField]
    private Text CountText;
    private bool canShoot = true;
    private WaitForSeconds spawnFlashDeltaTime = new WaitForSeconds(0.05f);
    public SpriteRenderer arrowSpriteRenderer;
    public SpriteRenderer dirCircleSpriteRenderer;
    private ShootType shootType = ShootType.Normal;
    private void Awake()
    {
        ballRB = GetComponent<Rigidbody2D>();
        ballCollider = GetComponent<Collider2D>();
       
    }

    private void OnEnable()
    {
        EventManager.Register<bool>(EventName.CanShoot, OnCanShoot);
        EventManager.Register<int>(EventName.ShootCountInit, OnShootCountInit);
        EventManager.Register<ShootType>(EventName.ChangeShootType, OnChangeShootType);
        Initialize();
    }

    private void OnShootCountInit(int _shootCount)
    {
        count = _shootCount;
        UpdateCountText();

    }

    private void Start()
    {
        StartCoroutine(SpawnFlash());
    }

    private void OnDisable()
    {

        EventManager.Remove<bool>(EventName.CanShoot, OnCanShoot);
        EventManager.Remove<int>(EventName.ShootCountInit, OnShootCountInit);
        EventManager.Remove<ShootType>(EventName.ChangeShootType, OnChangeShootType);
    }

    private void OnChangeShootType(ShootType _type)
    {
        shootType = _type;
       
    }

    private void OnCanShoot(bool _canShoot)
    {
        canShoot = _canShoot;
        if (!_canShoot) CountText.color = Color.clear;
    }
    private void Update()
    {
        if (canShoot)
        {
            switch (shootType)
            {
                case ShootType.Normal:
                    CountText.color = Color.black;
                    break;
                case ShootType.Penetration:
                    CountText.color = Color.red;
                    break;
                default:
                    break;
            }
        }
        if (InputManager.ShootPerformed && canShoot)
        {
            AudioManager.Instance.Play(AudioName.BulletTime);
            AudioManager.Instance.Pause(AudioName.BGM);
        }

        if (InputManager.ShootPress && canShoot) TimeManager.LaunchBulletTime(0.1f);

        if (InputManager.ShootRelease && canShoot)
        {
            AudioManager.Instance.Stop(AudioName.BulletTime);
            AudioManager.Instance.Play(AudioName.BGM);
            TimeManager.StopBulletTime();
         
            switch (shootType)
            {
                case ShootType.Normal:
                    NormalShoot();
                    break;
                case ShootType.Penetration:
                    PenetrationShoot();
                    break;
                default:
                    break;
            }
          

        }

        if (CheckBallDead && !isCoroutineRunning) StartCoroutine(DestoryBall());
    }
    bool isCoroutineRunning = false;
    IEnumerator DestoryBall()
    {
        yield return null;
         //EventManager.Send<Vector2>(EventName.PrepareDistoryBall, (Vector2)transform.position);
        isCoroutineRunning = true;
        //Tweener t1 = transform.DOScale(new Vector2(0f, 0f), 0.15f);
       
        //TimeManager.LaunchBulletTime(0.1f);

        //yield return t1.WaitForCompletion();
        //TimeManager.StopBulletTime();
        var newPieces = PoolManager.Instance.GetFromPool(PoolName.PiecesPool);
        newPieces.transform.position = transform.position;
        Destroy(this.gameObject);
        EventManager.Send<Vector2>(EventName.BallDead, (Vector2)transform.position);
        AudioManager.Instance.Play(AudioName.Hit_2);
        isCoroutineRunning = false;
    }
    IEnumerator SpawnFlash()
    {
        while (!CheckBallDead)
        {
            var newFlash = PoolManager.Instance.GetFromPool(PoolName.FlashPool);
            newFlash.transform.position = transform.position;
            newFlash.GetComponent<FlashController>().followSpriteRenderer = this.GetComponent<SpriteRenderer>();
            yield return spawnFlashDeltaTime;
        }


    }

    public bool CheckBallDead
    {
        get
        {

            if (count <0 || transform.position.y < -5.6f)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        var newPieces = PoolManager.Instance.GetFromPool(PoolName.PiecesPool);
        newPieces.transform.position = transform.position;
        newPieces.GetComponent<ParticleSystem>().startColor = GetComponent<SpriteRenderer>().color;
        EventManager.Send<Collision2D,GameObject>(EventName.BallHit, other,gameObject);
        AudioManager.Instance.Play(AudioName.Hit_2);
        //碰到平台反弹
        if (other.gameObject.CompareTag(Tags.Platform))
        {
            AudioManager.Instance.Play(AudioName.Hit_1);
            float x = HitFactor(transform.position, other.transform.position, other.collider.bounds.size.x);
            ballRB.velocity = new Vector2(x, 1).normalized * data.maxSpeed;
        }

        JellyEffect();

    }
   private Tweener scaleTweener;
    private void JellyEffect()
    {
        
        if(scaleTweener!=null)scaleTweener.Kill();
        transform.localScale = Vector3.one;
       scaleTweener = transform.DOPunchScale(new Vector2(0.7f, 0.7f), 0.2f);
    }
  
    //private void OnCollisionExit2D(Collision2D collision)
    //{
    //    transform.DOScale(new Vector3(1, 1, 1), 0.2f);
    //}
    private float HitFactor(Vector2 ballPos, Vector2 platformPos, float platformWidth)
    {
        return (ballPos.x - platformPos.x) / platformWidth;
    }

    /// <summary>
    /// 初始化数据
    /// </summary>
    private void Initialize()
    {
        ballRB.gravityScale = data.gravity;
        ballCollider.sharedMaterial.bounciness = data.bounciness;
        
    }
   public void UpdateCountText()
    {
        CountText.text = count.ToString();
    }
    private void NormalShoot()
    {
        AudioManager.Instance.Play(AudioName.BulletHit_1);
        Vector2 dir = ((Vector2)(transform.position - Camera.main.ScreenToWorldPoint(InputManager.MousePos))).normalized;
        var newProjectile = PoolManager.Instance.GetFromPool(PoolName.ProjectilePool);
        newProjectile.GetComponent<ProjectileController>().SetAngle(-dir);
        newProjectile.transform.position = transform.position;
        newProjectile.GetComponent<SpriteRenderer>().DOBlendableColor(GetComponent<SpriteRenderer>().color, 0.1f);
        ballRB.velocity = Vector2.zero;

        ballRB.AddForce(dir * data.backlashForce, ForceMode2D.Force);
        JellyEffect();
        count--;
        UpdateCountText();

    }

    private void PenetrationShoot()
    {
        Vector2 dir = ((Vector2)(transform.position - Camera.main.ScreenToWorldPoint(InputManager.MousePos))).normalized;
        StartCoroutine(PenetrationShootCoroutine(dir));
    }
    IEnumerator PenetrationShootCoroutine(Vector2 dir)
    {
        
        while (!Physics2D.OverlapCircle(transform.position, transform.localScale.x+0.1f).gameObject.CompareTag(Tags.WallBrick))
        {
            var hitBrick = Physics2D.OverlapCircle(transform.position, 1f).gameObject.GetComponent<BrickController>();
            if (hitBrick != null&&!hitBrick.gameObject.CompareTag(Tags.WallBrick)) StartCoroutine(hitBrick.Destory());
            ballRB.velocity = dir * data.maxSpeed*2.5f;
            yield return  new WaitForEndOfFrame();
        }
        shootType = ShootType.Normal;
      
    }
}
