using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MyGame.Resources;

public class RoomController : MonoBehaviour
{
    // The room keeps track of its own doors
    [Header("Dependencies")]
    [Tooltip("Drag the root GameObject for the doors (from left to right) into the three slots of this array")]
    public DoorController[] doors;
    [SerializeField] private GameObject shopCanvas;
    [SerializeField] private ShopUIController shopUI;
    [SerializeField] private QuizUI quizUI;

    [Header("Environment Objects")]
    public Transform plantLeft;
    public Transform plantRight;
    public Transform leftChair;
    public Transform rightChair;
    public Renderer lampshadeRenderer;

    private Vector3 plantLeftDefault;
    private Vector3 plantRightDefault;
    private Vector3 leftChairDefault;
    private Vector3 rightChairDefault;
    public Texture2D lampshadeNormalTexture;
    public Texture2D lampshadeChangedTexture;

    private void Awake()
    {
        if (plantLeft != null) plantLeftDefault = plantLeft.localPosition;
        if (plantRight != null) plantRightDefault = plantRight.localPosition;
        if (leftChair != null) leftChairDefault = leftChair.localPosition;
        if (rightChair != null) rightChairDefault = rightChair.localPosition;
    }

    private void Start()
    {
        if (shopUI != null)
        {
            shopUI.Initialize(GameManager.Instance.OnShopItemPurchased);
        }
    }

    // The GameManager can call this to easily lock/unlock a room
    public void SetLockDoors(bool isLocked)
    {
        foreach (DoorController door in doors)
        {
            door.isLocked = isLocked;
        }
    }

    public void SetLockDoor(DoorPos pos, bool isLocked)
    {
        doors[(int)pos].isLocked = false;
    }

    public void OpenDoor(DoorPos pos)
    {
        doors[(int)pos].OpenDoor();
    }

    public void OpenAllDoors()
    {
        foreach(DoorController door in doors)
        {
            door.OpenDoor();
        }
    }

    public void CloseDoor(DoorPos pos)
    {
        doors[(int)pos].CloseDoor();
    }

    public void CloseAllDoors()
    {
        foreach (DoorController door in doors)
        {
            door.CloseDoor();
        }
    }

    public void EnqueueQuestion(QuizQuestion q) => quizUI?.EnqueueQuestion(q);
    public void ResetQuiz() => quizUI?.ResetForNewRun();
    public bool IsQuizPending() => quizUI != null && quizUI.HasPendingSession();

    public void SetShopVisible(bool visible)
    {
        if (!shopCanvas.activeInHierarchy && visible)
        {
            AudioManager.Play(AudioManager.SoundCategory.Shop);
            shopUI.ResetShop();
        }
        shopCanvas.SetActive(visible);
    }

    public void ChangeRoomColor(RoomColors colors)
    {
        for(int i = 0; i < 3; i++)
        {
            doors[i].ChangeDoorColor(colors.doors[i], colors.doorHandles[i]);
        }
    }

    public void ApplyEnvironmentModifiers(RoomConfig.EnvironmentState env)
    {
        ResetEnvironment();

        if (env.plantsSwapped && plantLeft != null && plantRight != null)
        {
            plantLeft.localPosition = plantRightDefault;
            plantRight.localPosition = plantLeftDefault;
        }

        if (env.leftChairOut && leftChair != null)
        {
            leftChair.localPosition = leftChairDefault + new Vector3(0f, 0f, 0.25f);
        }

        if (env.rightChairOut && rightChair != null)
        {
            rightChair.localPosition = rightChairDefault + new Vector3(0f, 0f, -0.25f);
        }

        if (env.ceilingLightChanged && lampshadeRenderer != null)
        {
            lampshadeRenderer.material.SetTexture("_BaseMap", lampshadeChangedTexture);
        }
    }

    private void ResetEnvironment()
    {
        if (plantLeft != null) plantLeft.localPosition = plantLeftDefault;
        if (plantRight != null) plantRight.localPosition = plantRightDefault;
        if (leftChair != null) leftChair.localPosition = leftChairDefault;
        if (rightChair != null) rightChair.localPosition = rightChairDefault;
        if (lampshadeRenderer != null) lampshadeRenderer.material.SetTexture("_BaseMap", lampshadeNormalTexture);
    }
}
