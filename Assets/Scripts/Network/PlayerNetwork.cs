using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class PlayerNetwork : MonoBehaviourPun, IPunObservable
{
    [Header("Player Info")]
    public string playerName;
    public string playerRole;
    public int userId;

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotateSpeed = 100f;

    [Header("Network Settings")]
    [SerializeField] private float lerpSpeed = 15f;

    private Vector3 networkedPosition;
    private Quaternion networkedRotation;
    private Renderer playerRenderer;
    private Vector3 lastPosition;
    private float lastSyncTime;

    private void Awake()
    {
        playerRenderer = GetComponent<Renderer>();
    }

    private void Start()
    {
        if (photonView.IsMine)
        {
            Debug.Log($"Local player spawned: {playerName} ({playerRole})");
            SetupCamera();
            lastPosition = transform.position;
            lastSyncTime = Time.time;
        }
        else
        {
            // Get info from owner's custom properties
            if (photonView.Owner != null && photonView.Owner.CustomProperties != null)
            {
                if (photonView.Owner.CustomProperties.ContainsKey("playerName"))
                    playerName = photonView.Owner.CustomProperties["playerName"].ToString();
                if (photonView.Owner.CustomProperties.ContainsKey("role"))
                    playerRole = photonView.Owner.CustomProperties["role"].ToString();
                if (photonView.Owner.CustomProperties.ContainsKey("userId"))
                    userId = (int)photonView.Owner.CustomProperties["userId"];
            }

            networkedPosition = transform.position;
            networkedRotation = transform.rotation;
            Debug.Log($"Remote player spawned: {playerName} ({playerRole})");
        }

        ApplyRoleColor();
    }

    private void SetupCamera()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            GameObject camObj = new GameObject("MainCamera");
            cam = camObj.AddComponent<Camera>();
            camObj.tag = "MainCamera";
        }

        cam.transform.SetParent(transform);
        cam.transform.localPosition = new Vector3(0, 5, -10);
        cam.transform.localRotation = Quaternion.Euler(15, 0, 0);
    }

    public void Initialize(string name, string role, int id)
    {
        playerName = name;
        playerRole = role;
        userId = id;
        ApplyRoleColor();
    }

    private void ApplyRoleColor()
    {
        if (playerRenderer == null || string.IsNullOrEmpty(playerRole)) return;

        switch (playerRole.ToLower())
        {
            case "host":
                playerRenderer.material.color = Color.yellow;
                break;
            case "player":
                playerRenderer.material.color = Color.blue;
                break;
            case "audience":
                playerRenderer.material.color = new Color(0.6f, 0.2f, 0.8f);
                break;
            default:
                playerRenderer.material.color = Color.white;
                break;
        }
    }

    private void Update()
    {
        if (photonView.IsMine)
            HandleLocalMovement();
        else
            HandleRemoteMovement();
    }

    private void HandleLocalMovement()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 move = new Vector3(horizontal, 0, vertical) * moveSpeed * Time.deltaTime;
        transform.Translate(move, Space.World);

        if (move != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(move);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotateSpeed * Time.deltaTime);
        }
    }

    private void HandleRemoteMovement()
    {
        transform.position = Vector3.Lerp(transform.position, networkedPosition, Time.deltaTime * lerpSpeed);
        transform.rotation = Quaternion.Lerp(transform.rotation, networkedRotation, Time.deltaTime * lerpSpeed);
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
            stream.SendNext(playerName);
            stream.SendNext(playerRole);
            stream.SendNext(userId);
        }
        else
        {
            networkedPosition = (Vector3)stream.ReceiveNext();
            networkedRotation = (Quaternion)stream.ReceiveNext();
            playerName = (string)stream.ReceiveNext();
            playerRole = (string)stream.ReceiveNext();
            userId = (int)stream.ReceiveNext();
            ApplyRoleColor();
        }
    }
}